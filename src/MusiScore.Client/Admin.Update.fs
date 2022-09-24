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

    module CompositionListDto =
        let compositions_ : Lens<_, _> =
            (fun v -> v.Compositions),
            (fun v x -> { x with Compositions = v })

    module List =
        let item_ item : Prism<_, _> =
            let getter = List.tryFind ((=) item)
            let setter v = List.map (fun x -> if x = item then v else x)
            (getter, setter)

    module ExistingCompositionDto =
        let isActive_ : Lens<_, _> =
            (fun (v: ExistingCompositionDto) -> v.IsActive),
            (fun v x -> { x with IsActive = v })

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
        let renderPreviewError_ : Lens<_, _> =
            (fun (v: EditVoicesModel) -> v.RenderPreviewError),
            (fun v x -> { x with RenderPreviewError = v })

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
        let saveState_ : Lens<_, _> =
            (fun (v: EditVoiceModel) -> v.SaveState),
            (fun v x -> { x with SaveState = v })

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
    let loadedVoiceWithId_ voiceId =
        loadedVoices_ >?> EditVoiceModel.voiceWithId_ voiceId
    let selectedVoice_ : Prism<_, _> =
        let getter =
            fun model ->
                match model |> Optic.get (editVoices_ >?> EditVoicesModel.selectedVoice_) |> Option.bind id with
                | Some selectedVoiceId ->
                    model |> Optic.get (loadedVoiceWithId_ selectedVoiceId)
                | None -> None
        let setter =
            fun v model ->
                match model |> Optic.get (editVoices_ >?> EditVoicesModel.selectedVoice_) |> Option.bind id with
                | Some selectedVoiceId ->
                    model |> Optic.set (loadedVoiceWithId_ selectedVoiceId) v
                | None -> model
        (getter, setter)
    let listCompositionsItem_ item =
        Model.listCompositions_ >?> Deferred.loaded_ >?> CompositionListDto.compositions_ >?> Array.list_ >?> List.item_ item

    let validateVoiceName text =
        if text <> "" then ValidationSuccess text
        else ValidationError "Name darf nicht leer sein"

    let saveVoicesCmd model =
        let editCompositionData =
            model |> Optic.get (Model.editComposition_ >?> EditCompositionModel.state_ >?> EditCompositionState.loaded_)
            |> Option.orElse (model |> Optic.get (Model.editComposition_ >?> EditCompositionModel.state_ >?> EditCompositionState.modified_))
        let voices = model |> Optic.get loadedVoices_
        let cmd =
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
        model, cmd

    let setVoicesSaving model =
        model |> Optic.map loadedVoices_ (List.map (fun voice ->
            match voice.State with
            | LoadedVoice _ -> voice
            | CreatedVoice
            | ModifiedVoice _
            | DeletedVoice _ -> voice |> Optic.set EditVoiceModel.saveState_ (Some Deferred.Loading)
        ))

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
    | ToggleActivateComposition composition, ListCompositions (Deferred.Loaded _) ->
        let newComposition = composition |> Optic.map ExistingCompositionDto.isActive_ not
        let send () = task {
            let! response = httpClient.PatchAsync(composition.UpdateUrl, JsonContent.Create { Title = None; IsActive = Some newComposition.IsActive })
            return! response.Content.ReadFromJsonAsync<ExistingCompositionDto>()
        }
        model |> Optic.set (listCompositionsItem_ composition) newComposition,
        Cmd.OfTask.either send () (fun v -> UpdateCompositionResult (newComposition, v, Ok ())) (fun e -> UpdateCompositionResult (newComposition, composition, Error e))
    | ToggleActivateComposition _, model -> model, Cmd.none
    | UpdateCompositionResult (currentComposition, newComposition, Ok ()), ListCompositions (Deferred.Loaded _) ->
        model |> Optic.set (listCompositionsItem_ currentComposition) newComposition,
        Cmd.none
    | UpdateCompositionResult (currentComposition, newComposition, Error e), ListCompositions (Deferred.Loaded _) ->
        // TODO show error?
        model |> Optic.set (listCompositionsItem_ currentComposition) newComposition,
        Cmd.none
    | UpdateCompositionResult _, model -> model, Cmd.none
    | CreateComposition, ListCompositions (Deferred.Loaded compositionList) ->
        Model.EditComposition {
            State = CreatedComposition
            Title = FormInput.empty
            SaveUrl = compositionList.CreateCompositionUrl
            SaveState = None
            Voices = Some (Deferred.Loaded EditVoicesModel.``new``)
            VoicePrintSettings = (compositionList.GetPrintSettingsUrl, None)
            PdfModule = None
        },
        Cmd.batch [
            Cmd.ofMsg LoadEditCompositionVoicePrintSettings
            Cmd.ofMsg LoadPdfLib
        ]
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
            Cmd.ofMsg LoadPdfLib
        ]
    | EditComposition _, model -> model, Cmd.none
    | LoadEditCompositionVoices, Model.EditComposition { State = LoadedComposition data }
    | LoadEditCompositionVoices, Model.EditComposition { State = ModifiedComposition data } ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voices_) (Some Deferred.Loading),
        Cmd.OfTask.either (fun (url: string) -> httpClient.GetFromJsonAsync<ExistingVoiceDto array>(url)) data.GetVoicesUrl (Ok >> LoadEditCompositionVoicesResult) (Error >> LoadEditCompositionVoicesResult)
    | LoadEditCompositionVoices, model -> model, Cmd.none
    | LoadEditCompositionVoicesResult (Ok voices), Model.EditComposition { Voices = Some Deferred.Loading } ->
        let editVoices =
            voices
            |> Seq.sortBy (fun v -> v.Name)
            |> Seq.map (fun (voice: ExistingVoiceDto) ->
                {
                    Id = System.Guid.NewGuid()
                    State = LoadedVoice { UpdateUrl = voice.UpdateUrl; DeleteUrl = voice.DeleteUrl }
                    Name = FormInput.validated voice.Name voice.Name
                    File = Some (Ok voice.File)
                    PrintSetting = voice.PrintSetting
                    SaveState = None
                }
            )
            |> Seq.toList
        let editVoicesModel = {
            SelectedVoice = editVoices |> List.tryHead |> Option.map (fun v -> v.Id)
            Voices = editVoices
            RenderPreviewError = None
        }
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voices_) (Some (Deferred.Loaded editVoicesModel)),
        Cmd.ofMsg RenderVoicePreview
    | LoadEditCompositionVoicesResult (Error e), Model.EditComposition { Voices = Some Deferred.Loading } ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voices_) (Some (Deferred.LoadFailed e)),
        Cmd.none
    | LoadEditCompositionVoicesResult _, model -> model, Cmd.none
    | LoadEditCompositionVoicePrintSettings, Model.EditComposition { VoicePrintSettings = (getPrintSettingsUrl, _) } ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voicePrintSettings_) (Some Deferred.Loading),
        Cmd.OfTask.either (fun (url: string) -> httpClient.GetFromJsonAsync(url)) getPrintSettingsUrl (Ok >> LoadEditCompositionVoicePrintSettingsResult) (Error >> LoadEditCompositionVoicePrintSettingsResult)
    | LoadEditCompositionVoicePrintSettings, model -> model, Cmd.none
    | LoadEditCompositionVoicePrintSettingsResult (Ok printSettings), Model.EditComposition { VoicePrintSettings = (_, Some Deferred.Loading) } ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voicePrintSettings_) (Some (Deferred.Loaded (Array.toList printSettings))),
        Cmd.none
    | LoadEditCompositionVoicePrintSettingsResult (Error e), Model.EditComposition { VoicePrintSettings = (_, Some Deferred.Loading) } ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voicePrintSettings_) (Some (Deferred.LoadFailed e)),
        Cmd.none
    | LoadEditCompositionVoicePrintSettingsResult _, model -> model, Cmd.none
    | LoadPdfLib, Model.EditComposition _  ->
        model,
        Cmd.OfTask.either (fun () -> js.InvokeAsync("import", "./pdf.js").AsTask()) () (Ok >> LoadPdfLibResult) (Error >> LoadPdfLibResult)
    | LoadPdfLib, model -> model, Cmd.none
    | LoadPdfLibResult (Ok pdfLib), Model.EditComposition _ ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.pdfModule_) (Some (Ok pdfLib)),
        Cmd.ofMsg RenderVoicePreview
    | LoadPdfLibResult (Error e), Model.EditComposition _ ->
        printfn "LoadPdfLib Error: %A" e
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.pdfModule_) (Some (Error e)),
        Cmd.none
    | LoadPdfLibResult _, _ -> model, Cmd.none
    | SelectEditCompositionVoice voiceId, Model.EditComposition _ ->
        model |> Optic.set (editVoices_ >?> EditVoicesModel.selectedVoice_) (Some voiceId),
        Cmd.ofMsg RenderVoicePreview
    | SelectEditCompositionVoice _, model -> model, Cmd.none
    | AddEditCompositionVoice, Model.EditComposition { Voices = Some (Deferred.Loaded { Voices = voices }); VoicePrintSettings = (_, Some (Deferred.Loaded printSettings)) } ->
            let newVoice = EditVoiceModel.``new`` printSettings.Head.Key
            model |> Optic.set (editVoices_ >?> EditVoicesModel.voices_) (voices @ [ newVoice ]),
            Cmd.ofMsg (SelectEditCompositionVoice newVoice.Id)
    | AddEditCompositionVoice, model -> model, Cmd.none
    | RenderVoicePreview, Model.EditComposition { PdfModule = Some (Ok pdfLib) } ->
        let data =
            model
            |> Optic.get (selectedVoice_ >?> EditVoiceModel.file_ >?> Option.value_ >?> Result.ok_)
            |> Option.defaultValue [||]
            |> fun file -> {| file = file |}
        model,
        Cmd.OfTask.either (fun () -> pdfLib.InvokeAsync("renderVoice", [| data :> obj |]).AsTask()) () (Ok >> RenderVoicePreviewsResult) (Error >> RenderVoicePreviewsResult)
    | RenderVoicePreview, model -> model, Cmd.none
    | RenderVoicePreviewsResult result, _ ->
        printfn "RenderVoicePreviews %A" result
        model |> Optic.set (editVoices_ >?> EditVoicesModel.renderPreviewError_) (Result.errorToOption result),
        Cmd.none
    | SetEditCompositionFormInput (SetTitle text), Model.EditComposition _ ->
        let validationState =
            if text <> "" then ValidationSuccess text
            else ValidationError "Titel darf nicht leer sein"
        model
        |> Optic.map (Model.editComposition_ >?> EditCompositionModel.state_) EditCompositionState.modify
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.title_ >?> FormInput.text_) text
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.title_ >?> FormInput.validationState_) validationState,
        Cmd.none
    | SetEditCompositionFormInput (SetTitle _), model -> model, Cmd.none
    | SetEditCompositionFormInput (SetVoiceName text), Model.EditComposition { Voices = Some (Deferred.Loaded { SelectedVoice = Some _ }) } ->
        model
        |> Optic.map (selectedVoice_ >?> EditVoiceModel.state_) EditVoiceState.modify
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.name_ >?> FormInput.text_) text
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.name_ >?> FormInput.validationState_) (validateVoiceName text),
        Cmd.none
    | SetEditCompositionFormInput (SetVoiceName _), model -> model, Cmd.none
    | SetEditCompositionFormInput (SetVoiceFile file), (Model.EditComposition { Voices = Some (Deferred.Loaded { SelectedVoice = Some _ }); PdfModule = Some (Ok pdfLib) }) ->
        let validate () = async {
            let! content = file.OpenReadStream(maxAllowedSize = 20L * 1024L * 1024L) |> Stream.readAllBytes
            do! pdfLib.InvokeAsync("validatePdfFile", [| content :> obj |]).AsTask() |> Async.AwaitTask
            return content
        }
        model,
        Cmd.OfAsync.either validate () (fun data -> Ok (System.IO.Path.GetFileNameWithoutExtension file.Name, data) |> SetVoiceFileData |> SetEditCompositionFormInput) (Error >> SetVoiceFileData >> SetEditCompositionFormInput)
    | SetEditCompositionFormInput (SetVoiceFile _), model -> model, Cmd.none
    | SetEditCompositionFormInput (SetVoiceFileData (Ok (voiceName, content))), Model.EditComposition { Voices = Some (Deferred.Loaded { SelectedVoice = Some _ }) } ->
        model
        |> Optic.map (selectedVoice_ >?> EditVoiceModel.state_) EditVoiceState.modify
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.file_) (Some (Ok content))
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.emptyName_) { Text = voiceName; ValidationState = validateVoiceName voiceName },
        Cmd.ofMsg RenderVoicePreview
    | SetEditCompositionFormInput (SetVoiceFileData (Error e)), Model.EditComposition { Voices = Some (Deferred.Loaded { SelectedVoice = Some _ }) } ->
        printfn "SetVoiceFileData Error: %A" e
        model
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.file_) (Some (Error e)),
        Cmd.ofMsg RenderVoicePreview
    | SetEditCompositionFormInput (SetVoiceFileData _), model -> model, Cmd.none
    | SetEditCompositionFormInput (SetPrintSetting key), Model.EditComposition { Voices = Some (Deferred.Loaded { SelectedVoice = Some _ }) } ->
        model
        |> Optic.map (selectedVoice_ >?> EditVoiceModel.state_) EditVoiceState.modify
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.printSetting_) key,
        Cmd.none
    | SetEditCompositionFormInput (SetPrintSetting _), model -> model, Cmd.none
    | SaveComposition, Model.EditComposition ({ SaveState = Some Deferred.Loading }) -> model, Cmd.none
    | SaveComposition, Model.EditComposition subModel ->
        match subModel.State with
        | LoadedComposition _ -> model |> setVoicesSaving |> saveVoicesCmd
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
        | ModifiedComposition _ ->
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
    | SaveCompositionResult (Ok composition), Model.EditComposition { SaveState = Some Deferred.Loading } ->
        model
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.state_) (LoadedComposition { GetVoicesUrl = composition.GetVoicesUrl; CreateVoiceUrl = composition.CreateVoiceUrl })
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.saveUrl_) composition.UpdateUrl
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.saveState_) (Some (Deferred.Loaded ()))
        |> setVoicesSaving
        |> saveVoicesCmd
    | SaveCompositionResult (Error e), Model.EditComposition { SaveState = Some Deferred.Loading } ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.saveState_) (Some (Deferred.LoadFailed e)),
        Cmd.none
    | SaveCompositionResult _, model -> model, Cmd.none
    | SaveVoiceResult (voiceId, Ok voiceData), Model.EditComposition _ ->
        model
        |> Optic.set (loadedVoiceWithId_ voiceId >?> EditVoiceModel.state_) (LoadedVoice { UpdateUrl = voiceData.UpdateUrl; DeleteUrl = voiceData.DeleteUrl })
        |> Optic.set (loadedVoiceWithId_ voiceId >?> EditVoiceModel.saveState_) (Some (Deferred.Loaded ())),
        Cmd.none
    | SaveVoiceResult (voiceId, Error e), Model.EditComposition _ ->
        model
        |> Optic.set (loadedVoiceWithId_ voiceId >?> EditVoiceModel.saveState_) (Some (Deferred.LoadFailed e)),
        Cmd.none
    | SaveVoiceResult _, model -> model, Cmd.none
    | DeleteVoiceResult (voiceId, Ok ()), Model.EditComposition _ ->
        model
        |> Optic.map loadedVoices_ (List.filter (fun v -> v.Id <> voiceId)),
        Cmd.none
    | DeleteVoiceResult (voiceId, Error e), Model.EditComposition _ ->
        model
        |> Optic.set (loadedVoiceWithId_ voiceId >?> EditVoiceModel.saveState_) (Some (Deferred.LoadFailed e)),
        Cmd.none
    | DeleteVoiceResult _, model -> model, Cmd.none
