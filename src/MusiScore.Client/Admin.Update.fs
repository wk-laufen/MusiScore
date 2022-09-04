module MusiScore.Client.Admin.Update

open Aether
open Aether.Operators
open Elmish
open Microsoft.JSInterop
open MusiScore.Shared.DataTransfer.Admin
open System.Net.Http
open System.Net.Http.Json

[<AutoOpen>]
module private Optics =
    module Model =
        let listCompositions_ : Prism<_, _> =
            let getter = function
                | ListCompositions v -> Some v
                | _ -> None
            let setter v x =
                match x with
                | ListCompositions _ -> ListCompositions v
                | x -> x
            (getter, setter)
        let editComposition_ : Prism<_, _> =
            let getter = function
                | Model.EditComposition v -> Some v
                | _ -> None
            let setter v x =
                match x with
                | Model.EditComposition _ -> Model.EditComposition v
                | x -> x
            (getter, setter)

    module EditCompositionModel =
        let state_ : Lens<_, _> =
            (fun v -> v.State),
            (fun v x -> { x with State = v })
        let title_ : Lens<_, _> =
            (fun (v: EditCompositionModel) -> v.Title),
            (fun v x -> { x with Title = v })
        let saveUrl_ : Lens<_, _> =
            (fun v -> v.SaveUrl),
            (fun v x -> { x with SaveUrl = v })
        let saveState_ : Lens<_, _> =
            (fun v -> v.SaveState),
            (fun v x -> { x with SaveState = v })
        let voices_ : Lens<_, _> =
            (fun v -> v.Voices),
            (fun v x -> { x with Voices = v })
        let voicePrintSettings_ : Lens<_, _> =
            (fun v -> snd v.VoicePrintSettings),
            (fun v x -> { x with VoicePrintSettings = (fst x.VoicePrintSettings, v) })
        let pdfModule_ : Lens<_, _> =
            (fun v -> v.PdfModule),
            (fun v x -> { x with PdfModule = v })

    module EditCompositionState =
        let loaded_ : Prism<_, _> =
            let getter = function
                | LoadedComposition v -> Some v
                | _ -> None
            let setter v x =
                match x with
                | LoadedComposition _ -> LoadedComposition v
                | x -> x
            (getter, setter)
        let modified_ : Prism<_, _> =
            let getter = function
                | ModifiedComposition v -> Some v
                | _ -> None
            let setter v x =
                match x with
                | ModifiedComposition _ -> ModifiedComposition v
                | x -> x
            (getter, setter)

    module FormInput =
        let text_ : Lens<_, _> =
            (fun v -> v.Text),
            (fun v x -> { x with Text = v })
        let validationState_ : Lens<_, _> =
            (fun v -> v.ValidationState),
            (fun v x -> { x with ValidationState = v })

    module EditVoicesModel =
        let selectedVoice_ : Lens<_, _> =
            (fun v -> v.SelectedVoice),
            (fun v x -> { x with SelectedVoice = v })
        let voices_ : Lens<_, _> =
            (fun (v: EditVoicesModel) -> v.Voices),
            (fun v x -> { x with Voices = v })

    module EditVoiceModel =
        let voiceWithId_ voiceId : Prism<_, _> =
            let getter = List.tryFind (fun v -> v.Id = voiceId)
            let setter value =
                List.map (fun v ->
                    if v.Id = voiceId then value
                    else v
                )
            (getter, setter)
        let state_ : Lens<_, _> =
            (fun (v: EditVoiceModel) -> v.State),
            (fun v x -> { x with State = v })
        let name_ : Lens<_, _> =
            (fun (v: EditVoiceModel) -> v.Name),
            (fun v x -> { x with Name = v })
        let emptyName_ : Prism<_, _> =
            let getter =
                fun (v: EditVoiceModel) -> if v.Name.Text = "" then Some v.Name else None
            let setter =
                fun v (x: EditVoiceModel) -> if x.Name.Text = "" then { x with Name = v } else x
            (getter, setter)
        let file_ : Lens<_, _> =
            (fun (v: EditVoiceModel) -> v.File),
            (fun v x -> { x with File = v })
        let printSetting_ : Lens<_, _> =
            (fun (v: EditVoiceModel) -> v.PrintSetting),
            (fun v x -> { x with PrintSetting = v })

    module Deferred =
        let loaded_ : Prism<_, _> =
            let getter = function
                | Loaded v -> Some v
                | Loading
                | LoadFailed _ -> None
            let setter v x =
                Deferred.map (fun _ -> v) x
            (getter, setter)

let update (httpClient: HttpClient) (js: IJSRuntime) message model =
    let editVoices_ =
        Model.editComposition_ >?> EditCompositionModel.voices_ >?> Option.value_ >?> Deferred.loaded_
    let loadedVoices_ =
        editVoices_ >?> EditVoicesModel.voices_
    let selectedVoice_ : Prism<_, _> =
        let getter =
            fun model ->
                match model |> Optic.get (editVoices_ >?> EditVoicesModel.selectedVoice_) |> Option.bind id with
                | Some selectedVoiceId ->
                    model |> Optic.get (loadedVoices_ >?> EditVoiceModel.voiceWithId_ selectedVoiceId)
                | None -> None
        let setter =
            fun v model ->
                match model |> Optic.get (editVoices_ >?> EditVoicesModel.selectedVoice_) |> Option.bind id with
                | Some selectedVoiceId ->
                    model |> Optic.set (loadedVoices_ >?> EditVoiceModel.voiceWithId_ selectedVoiceId) v
                | None -> model
        (getter, setter)

    let validateVoiceName text =
        if text <> "" then ValidationSuccess text
        else ValidationError "Name darf nicht leer sein"

    let saveVoicesCmd =
        let editCompositionData =
            model |> Optic.get (Model.editComposition_ >?> EditCompositionModel.state_ >?> EditCompositionState.loaded_)
            |> Option.orElse (model |> Optic.get (Model.editComposition_ >?> EditCompositionModel.state_ >?> EditCompositionState.modified_))
        let voices = model |> Optic.get loadedVoices_
        match editCompositionData, voices with
        | Some editCompositionData, Some voices ->
            voices
            |> List.map (fun voice ->
                match voice.State with
                | LoadedVoice _ -> Cmd.none
                | CreatedVoice ->
                    match EditVoiceModel.validateNewVoiceForm voice with
                    | Some dto ->
                        let send () = task {
                            let! response = httpClient.PostAsJsonAsync(editCompositionData.CreateVoiceUrl, dto)
                            return! response.Content.ReadFromJsonAsync<ExistingVoiceDto>()
                        }
                        Cmd.OfTask.either send () (fun v -> SaveVoiceResult (voice.Id, Ok v)) (fun v -> SaveVoiceResult (voice.Id, Error v))
                    | None -> Cmd.none
                | ModifiedVoice existingVoiceData ->
                    match EditVoiceModel.validateUpdateVoiceForm voice with
                    | Some dto ->
                        let send () = task {
                            let! response = httpClient.PatchAsync(existingVoiceData.UpdateUrl, JsonContent.Create(dto))
                            return! response.Content.ReadFromJsonAsync<ExistingVoiceDto>()
                        }
                        Cmd.OfTask.either send () (fun v -> SaveVoiceResult (voice.Id, Ok v)) (fun v -> SaveVoiceResult (voice.Id, Error v))
                    | None -> Cmd.none
                | DeletedVoice existingVoiceData ->
                    Cmd.OfTask.either (fun () -> httpClient.DeleteAsync(existingVoiceData.DeleteUrl)) () (fun v -> DeleteVoiceResult (voice.Id, Ok ())) (fun v -> DeleteVoiceResult (voice.Id, Error v))
            )
            |> Cmd.batch
        | _ -> Cmd.none

    match message, model with
    | LoadCompositions, _ ->
        ListCompositions Deferred.Loading,
        Cmd.OfTask.either (fun (url: string) -> httpClient.GetFromJsonAsync<CompositionListDto>(url)) "/api/admin/compositions" (Ok >> LoadCompositionsResult) (Error >> LoadCompositionsResult)
    | LoadCompositionsResult (Ok compositionList), model ->
        model |> Optic.set Model.listCompositions_ (Deferred.Loaded compositionList),
        Cmd.none
    | LoadCompositionsResult (Error e), model ->
        model |> Optic.set Model.listCompositions_ (Deferred.LoadFailed e),
        Cmd.none
    | CreateComposition, ListCompositions (Deferred.Loaded compositionList) ->
        Model.EditComposition {
            State = CreatedComposition
            Title = FormInput.empty
            SaveUrl = compositionList.CreateCompositionUrl
            SaveState = None
            Voices = None
            VoicePrintSettings = (compositionList.GetPrintSettingsUrl, None)
            PdfModule = None
        },
        Cmd.ofMsg LoadEditCompositionVoicePrintSettings
    | CreateComposition, model -> model, Cmd.none
    | EditComposition composition, ListCompositions (Deferred.Loaded compositionList) ->
        Model.EditComposition {
            State = LoadedComposition { GetVoicesUrl = composition.GetVoicesUrl; CreateVoiceUrl = composition.CreateVoiceUrl }
            Title = FormInput.validated composition.Title composition.Title
            SaveUrl = composition.UpdateUrl
            SaveState = None
            Voices = None
            VoicePrintSettings = (compositionList.GetPrintSettingsUrl, None)
            PdfModule = None
        },
        Cmd.batch [
            Cmd.ofMsg LoadEditCompositionVoices
            Cmd.ofMsg LoadEditCompositionVoicePrintSettings
            Cmd.OfTask.either (fun () -> js.InvokeAsync("import", "./pdf.js").AsTask()) () (Ok >> LoadPdfLibResult) (Error >> LoadPdfLibResult)
        ]
    | EditComposition _, _ -> model, Cmd.none
    | LoadEditCompositionVoices, (Model.EditComposition { State = LoadedComposition data })
    | LoadEditCompositionVoices, (Model.EditComposition { State = ModifiedComposition data }) ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voices_) (Some Deferred.Loading),
        Cmd.OfTask.either (fun (url: string) -> httpClient.GetFromJsonAsync<ExistingVoiceDto array>(url)) data.GetVoicesUrl (Ok >> LoadEditCompositionVoicesResult) (Error >> LoadEditCompositionVoicesResult)
    | LoadEditCompositionVoices, model -> model, Cmd.none
    | LoadEditCompositionVoicesResult (Ok voices), (Model.EditComposition subModel) ->
        let editVoices =
            voices
            |> Seq.map (fun (voice: ExistingVoiceDto) ->
                {
                    Id = System.Guid.NewGuid()
                    State = LoadedVoice { UpdateUrl = voice.UpdateUrl; DeleteUrl = voice.DeleteUrl }
                    Name = FormInput.validated voice.Name voice.Name
                    File = Some (Ok voice.File)
                    PrintSetting = voice.PrintSetting
                }
            )
            |> Seq.toList
        let editVoicesModel = {
            SelectedVoice = editVoices |> List.tryHead |> Option.map (fun v -> v.Id)
            Voices = editVoices
        }
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voices_) (Some (Deferred.Loaded editVoicesModel)),
        Cmd.ofMsg RenderVoicePreview
    | LoadEditCompositionVoicesResult (Error e), (Model.EditComposition _) ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voices_) (Some (Deferred.LoadFailed e)),
        Cmd.none
    | LoadEditCompositionVoicesResult _, model -> model, Cmd.none
    | LoadEditCompositionVoicePrintSettings, (Model.EditComposition { VoicePrintSettings = (getPrintSettingsUrl, _) }) ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voicePrintSettings_) (Some Deferred.Loading),
        Cmd.OfTask.either (fun (url: string) -> httpClient.GetFromJsonAsync(url)) getPrintSettingsUrl (Ok >> LoadEditCompositionVoicePrintSettingsResult) (Error >> LoadEditCompositionVoicePrintSettingsResult)
    | LoadEditCompositionVoicePrintSettings, _ -> model, Cmd.none
    | LoadEditCompositionVoicePrintSettingsResult (Ok printSettings), _ ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voicePrintSettings_) (Some (Deferred.Loaded (Array.toList printSettings))),
        Cmd.none
    | LoadEditCompositionVoicePrintSettingsResult (Error e), _ ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voicePrintSettings_) (Some (Deferred.LoadFailed e)),
        Cmd.none
    | LoadPdfLibResult result, Model.EditComposition _ ->
        printfn "LoadPdfLibResult %A" result // TODO
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.pdfModule_) (Some result),
        Cmd.ofMsg RenderVoicePreview
    | SelectEditCompositionVoice voiceId, Model.EditComposition _ ->
        model |> Optic.set (editVoices_ >?> EditVoicesModel.selectedVoice_) (Some voiceId),
        Cmd.ofMsg RenderVoicePreview
    | SelectEditCompositionVoice _, model -> model, Cmd.none
    | AddEditCompositionVoice, (Model.EditComposition { Voices = Some (Deferred.Loaded { Voices = voices }); VoicePrintSettings = (_, Some (Deferred.Loaded printSettings))  }) ->
            let newVoice = EditVoiceModel.``new`` printSettings.Head.Key
            model |> Optic.set (editVoices_ >?> EditVoicesModel.voices_) (voices @ [ newVoice ]),
            Cmd.ofMsg (SelectEditCompositionVoice newVoice.Id)
    | AddEditCompositionVoice, model -> model, Cmd.none
    | LoadPdfLibResult _, _ -> model, Cmd.none
    | RenderVoicePreview, Model.EditComposition { Voices = Some (Deferred.Loaded editVoices); PdfModule = Some (Ok pdfLib) } ->
        let data =
            EditVoicesModel.tryGetSelectedVoice editVoices
            |> Option.bind (fun v -> v.File)
            |> Option.bind Result.toOption
            |> Option.defaultValue [||]
            |> fun file -> {| file = file |}
        model,
        Cmd.OfTask.either (fun () -> pdfLib.InvokeAsync("renderVoice", [| data :> obj |]).AsTask()) () (Ok >> RenderVoicePreviewsResult) (Error >> RenderVoicePreviewsResult)
    | RenderVoicePreview, _ -> model, Cmd.none
    | RenderVoicePreviewsResult result, _ ->
        printfn "RenderVoicePreviewsResult %A" result
        model, Cmd.none // TODO
    | SetEditCompositionFormInput (SetTitle text), (Model.EditComposition subModel) ->
        let validationState =
            if text <> "" then ValidationSuccess text
            else ValidationError "Titel darf nicht leer sein"
        model
        |> Optic.map (Model.editComposition_ >?> EditCompositionModel.state_) EditCompositionState.modify
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.title_ >?> FormInput.text_) text
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.title_ >?> FormInput.validationState_) validationState,
        Cmd.none
    | SetEditCompositionFormInput (SetTitle _), model -> model, Cmd.none
    | SetEditCompositionFormInput (SetVoiceName text), model ->
        model
        |> Optic.map (selectedVoice_ >?> EditVoiceModel.state_) EditVoiceState.modify
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.name_ >?> FormInput.text_) text
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.name_ >?> FormInput.validationState_) (validateVoiceName text),
        Cmd.none
    | SetEditCompositionFormInput (SetVoiceFile file), (Model.EditComposition { Voices = Some (Deferred.Loaded { SelectedVoice = Some _ }); PdfModule = Some (Ok pdfLib) }) ->
        let validate () = async {
            let! content = file.OpenReadStream(maxAllowedSize = 20L * 1024L * 1024L) |> Stream.readAllBytes
            do! pdfLib.InvokeAsync("validatePdfFile", [| content :> obj |]).AsTask() |> Async.AwaitTask
            return content
        }
        model,
        Cmd.OfAsync.either validate () (fun data -> Ok (System.IO.Path.GetFileNameWithoutExtension file.Name, data) |> SetVoiceFileData |> SetEditCompositionFormInput) (Error >> SetVoiceFileData >> SetEditCompositionFormInput)
    | SetEditCompositionFormInput (SetVoiceFile _), _ -> model, Cmd.none
    | SetEditCompositionFormInput (SetVoiceFileData (Ok (voiceName, content))), model ->
        model
        |> Optic.map (selectedVoice_ >?> EditVoiceModel.state_) EditVoiceState.modify
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.file_) (Some (Ok content))
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.emptyName_) { Text = voiceName; ValidationState = validateVoiceName voiceName },
        Cmd.ofMsg RenderVoicePreview
    | SetEditCompositionFormInput (SetVoiceFileData (Error e)), model ->
        printfn "SetVoiceFileData Error: %A" e // TODO
        model
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.file_) (Some (Error e)),
        Cmd.ofMsg RenderVoicePreview
    | SetEditCompositionFormInput (SetPrintSetting key), model ->
        model
        |> Optic.map (selectedVoice_ >?> EditVoiceModel.state_) EditVoiceState.modify
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.printSetting_) key,
        Cmd.none
    | SaveComposition, (Model.EditComposition subModel) ->
        match subModel.State with
        | LoadedComposition data -> model, saveVoicesCmd
        | CreatedComposition ->
            match EditCompositionModel.validateNewCompositionForm subModel with
            | Some dto ->
                let send (url: string, body: NewCompositionDto) = task {
                    let! response = httpClient.PostAsJsonAsync(url, body)
                    return! response.Content.ReadFromJsonAsync<ExistingCompositionDto>()
                }
                model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.saveState_) (Some Deferred.Loading),
                Cmd.OfTask.either send (subModel.SaveUrl, dto) (Ok >> SaveCompositionResult) (Error >> SaveCompositionResult)
            | None -> model, Cmd.none
        | ModifiedComposition data ->
            match EditCompositionModel.validateUpdateCompositionForm subModel with
            | Some dto ->
                let send (url: string, body: CompositionUpdateDto) = task {
                    let! response = httpClient.PatchAsync(url, JsonContent.Create(body))
                    return! response.Content.ReadFromJsonAsync<ExistingCompositionDto>()
                }
                model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.saveState_) (Some Deferred.Loading),
                Cmd.OfTask.either send (subModel.SaveUrl, dto) (Ok >> SaveCompositionResult) (Error >> SaveCompositionResult)
            | None -> model, Cmd.none
    | SaveComposition, model -> model, Cmd.none
    | SaveCompositionResult (Ok composition), (Model.EditComposition subModel) ->
        model
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.state_) (LoadedComposition { GetVoicesUrl = composition.GetVoicesUrl; CreateVoiceUrl = composition.CreateVoiceUrl })
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.saveUrl_) composition.UpdateUrl
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.saveState_) (Some (Deferred.Loaded ())),
        saveVoicesCmd
    | SaveCompositionResult (Error e), (Model.EditComposition subModel) ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.saveState_) (Some (Deferred.LoadFailed e)),
        Cmd.none
    | SaveCompositionResult _, model -> model, Cmd.none
    | SaveVoiceResult (voiceId, Ok voiceData), model ->
        model
        |> Optic.set (loadedVoices_ >?> EditVoiceModel.voiceWithId_ voiceId >?> EditVoiceModel.state_) (LoadedVoice { UpdateUrl = voiceData.UpdateUrl; DeleteUrl = voiceData.DeleteUrl }),
        Cmd.none
    | SaveVoiceResult (voiceId, Error e), model ->
        model, Cmd.none // TODO
    | DeleteVoiceResult (voiceId, Ok ()), model ->
        model
        |> Optic.map loadedVoices_ (List.filter (fun v -> v.Id <> voiceId)),
        Cmd.none
    | DeleteVoiceResult (voiceId, Error e), model ->
        model, Cmd.none // TODO
