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

    module ListCompositionsModel =
        let compositions_ : Lens<_, _> =
            (fun (v: ListCompositionsModel) -> v.Compositions),
            (fun v x -> { x with Compositions = v })
        let compositionDeleteState_ : Lens<_, _> =
            (fun (v: ListCompositionsModel) -> v.CompositionDeleteState),
            (fun v x -> { x with CompositionDeleteState = v })
        let compositionFilter_ : Lens<_, _> =
            (fun (v: ListCompositionsModel) -> v.CompositionFilter),
            (fun v x -> { x with CompositionFilter = v })

    module CompositionFilter =
        let text_ : Lens<_, _> =
            (fun v -> v.Text),
            (fun v x -> { x with Text = v })
        let activeOnly_ : Lens<_, _> =
            (fun v -> v.ActiveOnly),
            (fun v x -> { x with ActiveOnly = v })

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
            (fun (v: FormInput<_>) -> v.Text),
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

    module Deferred =
        let loaded_ : Prism<_, _> =
            let getter = function
                | Loaded v -> Some v
                | Loading
                | LoadFailed _ -> None
            let setter v x =
                Deferred.map (fun _ -> v) x
            (getter, setter)

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
        Model.listCompositions_ >?> Deferred.loaded_ >?> ListCompositionsModel.compositions_ >?> List.item_ item

let update (httpClient: HttpClient) (js: IJSRuntime) message model =
    let httpGet (url: string) = async {
        return! httpClient.GetFromJsonAsync<_>(url) |> Async.AwaitTask
    }
    let httpPost (url: string, data) = async {
        let! response = httpClient.PostAsJsonAsync(url, data) |> Async.AwaitTask
        return! response.Content.ReadFromJsonAsync<_>() |> Async.AwaitTask
    }
    let httpPatch (url: string, data) = async {
        let! response = httpClient.PatchAsync(url, JsonContent.Create data) |> Async.AwaitTask
        return! response.Content.ReadFromJsonAsync<_>() |> Async.AwaitTask
    }
    let httpDelete (url: string) = async {
        let! response = httpClient.DeleteAsync(url) |> Async.AwaitTask
        response.EnsureSuccessStatusCode() |> ignore
    }

    let validateVoiceName text =
        if text <> "" then ValidationSuccess text
        else ValidationError "Name darf nicht leer sein"

    let saveVoices model =
        let editCompositionData =
            model |> Optic.get (Model.editComposition_ >?> EditCompositionModel.state_ >?> EditCompositionState.loaded_)
            |> Option.orElse (model |> Optic.get (Model.editComposition_ >?> EditCompositionModel.state_ >?> EditCompositionState.modified_))
        let voices = model |> Optic.get loadedVoices_
        match editCompositionData, voices with
        | Some editCompositionData, Some voices ->
            ((model, Cmd.none), voices)
            ||> List.fold (fun (model, cmds) voice ->
                match voice.State with
                | LoadedVoice (false, _) -> model, cmds
                | CreatedVoice ->
                    match EditVoiceModel.validateNewVoiceForm voice with
                    | Some dto ->
                        let cmd =
                            Cmd.OfAsync.either httpPost (editCompositionData.CreateVoiceUrl, dto)
                                (fun v -> SaveVoiceResult (voice.Id, Ok v))
                                (fun v -> SaveVoiceResult (voice.Id, Error v))
                        model
                        |> Optic.map (Model.editComposition_ >?> EditCompositionModel.saveState_ >?> Option.value_) EditCompositionSaveState.addSaving,
                        Cmd.batch [ cmds; cmd ]
                    | None -> model, cmds
                | ModifiedVoice (false, existingVoiceData) ->
                    match EditVoiceModel.validateUpdateVoiceForm voice with
                    | Some dto ->
                        let cmd =
                            Cmd.OfAsync.either httpPatch (existingVoiceData.UpdateUrl, dto)
                                (fun v -> SaveVoiceResult (voice.Id, Ok v))
                                (fun v -> SaveVoiceResult (voice.Id, Error v))
                        model
                        |> Optic.map (Model.editComposition_ >?> EditCompositionModel.saveState_ >?> Option.value_) EditCompositionSaveState.addSaving,
                        Cmd.batch [ cmds; cmd ]
                    | None -> model, cmds
                | LoadedVoice (true, existingVoiceData)
                | ModifiedVoice (true, existingVoiceData) ->
                    let cmd =
                        Cmd.OfAsync.either httpDelete existingVoiceData.DeleteUrl
                            (fun v -> DeleteVoiceResult (voice.Id, Ok ()))
                            (fun v -> DeleteVoiceResult (voice.Id, Error v))
                    model
                    |> Optic.map (Model.editComposition_ >?> EditCompositionModel.saveState_ >?> Option.value_) EditCompositionSaveState.addSaving,
                    Cmd.batch [ cmds; cmd ]
            )
        | _ -> model, Cmd.none

    match message, model with
    | LoadCompositions, _ ->
        ListCompositions Deferred.Loading,
        Cmd.OfAsync.either httpGet "/api/admin/compositions" (Ok >> LoadCompositionsResult) (Error >> LoadCompositionsResult)
    | LoadCompositionsResult (Ok compositionList), model ->
        model |> Optic.set Model.listCompositions_ (Deferred.Loaded (ListCompositionsModel.init compositionList)),
        Cmd.none
    | LoadCompositionsResult (Error e), model ->
        model |> Optic.set Model.listCompositions_ (Deferred.LoadFailed e),
        Cmd.none
    | ToggleActivateComposition composition, model ->
        let newComposition = composition |> Optic.map ExistingCompositionDto.isActive_ not
        model |> Optic.set (listCompositionsItem_ composition) newComposition,
        Cmd.OfAsync.either httpPatch (composition.UpdateUrl, { Title = None; IsActive = Some newComposition.IsActive })
            (fun v -> UpdateCompositionResult (newComposition, v, Ok ()))
            (fun e -> UpdateCompositionResult (newComposition, composition, Error e))
    | UpdateCompositionResult (currentComposition, newComposition, Ok ()), model ->
        model |> Optic.set (listCompositionsItem_ currentComposition) newComposition,
        Cmd.none
    | UpdateCompositionResult (currentComposition, newComposition, Error e), model ->
        // TODO show error?
        model |> Optic.set (listCompositionsItem_ currentComposition) newComposition,
        Cmd.none
    | ChangeCompositionFilterText text, model ->
        model
        |> Optic.set (Model.listCompositions_ >?> Deferred.loaded_ >?> ListCompositionsModel.compositionFilter_ >?> CompositionFilter.text_) text,
        Cmd.none
    | ShowActiveCompositionsOnly value, model ->
        model
        |> Optic.set (Model.listCompositions_ >?> Deferred.loaded_ >?> ListCompositionsModel.compositionFilter_ >?> CompositionFilter.activeOnly_) value,
        Cmd.none
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
    | DeleteComposition composition, ListCompositions (Deferred.Loaded { CompositionDeleteState = Some (compositionToDelete, None) })
    | DeleteComposition composition, ListCompositions (Deferred.Loaded { CompositionDeleteState = Some (compositionToDelete, Some (Deferred.LoadFailed _)) }) when composition = compositionToDelete ->
        model
        |> Optic.set (Model.listCompositions_ >?> Deferred.loaded_ >?> ListCompositionsModel.compositionDeleteState_ >?> Option.value_ >?> snd_) (Some Deferred.Loading),
        Cmd.OfAsync.either httpDelete composition.DeleteUrl (Ok >> DeleteCompositionResult) (Error >> DeleteCompositionResult)
    | DeleteComposition composition, model ->
        model
        |> Optic.set (Model.listCompositions_ >?> Deferred.loaded_ >?> ListCompositionsModel.compositionDeleteState_) (Some (composition, None)),
        Cmd.none
    | DeleteCompositionResult (Ok ()), ListCompositions (Deferred.Loaded { CompositionDeleteState = Some (compositionToDelete, Some Deferred.Loading) }) ->
        model
        |> Optic.map (Model.listCompositions_ >?> Deferred.loaded_ >?> ListCompositionsModel.compositions_) (List.filter ((<>) compositionToDelete))
        |> Optic.set (Model.listCompositions_ >?> Deferred.loaded_ >?> ListCompositionsModel.compositionDeleteState_) None,
        Cmd.none
    | DeleteCompositionResult (Error e), ListCompositions (Deferred.Loaded { CompositionDeleteState = Some (compositionToDelete, Some Deferred.Loading) }) ->
        model
        |> Optic.set (Model.listCompositions_ >?> Deferred.loaded_ >?> ListCompositionsModel.compositionDeleteState_ >?> Option.value_ >?> snd_ >?> Option.value_) (Deferred.LoadFailed e),
        Cmd.none
    | DeleteCompositionResult _, model -> model, Cmd.none
    | LoadEditCompositionVoices, Model.EditComposition { State = LoadedComposition data }
    | LoadEditCompositionVoices, Model.EditComposition { State = ModifiedComposition data } ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voices_) (Some Deferred.Loading),
        Cmd.OfAsync.either httpGet data.GetVoicesUrl (Ok >> LoadEditCompositionVoicesResult) (Error >> LoadEditCompositionVoicesResult)
    | LoadEditCompositionVoices, model -> model, Cmd.none
    | LoadEditCompositionVoicesResult (Ok voices), Model.EditComposition { Voices = Some Deferred.Loading } ->
        let editVoicesModel =
            EditVoiceModel.fromExistingVoices voices
            |> EditVoicesModel.init
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voices_) (Some (Deferred.Loaded editVoicesModel)),
        Cmd.ofMsg RenderVoicePreview
    | LoadEditCompositionVoicesResult (Error e), Model.EditComposition { Voices = Some Deferred.Loading } ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voices_) (Some (Deferred.LoadFailed e)),
        Cmd.none
    | LoadEditCompositionVoicesResult _, model -> model, Cmd.none
    | LoadEditCompositionVoicePrintSettings, Model.EditComposition { VoicePrintSettings = (getPrintSettingsUrl, _) } ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voicePrintSettings_) (Some Deferred.Loading),
        Cmd.OfAsync.either httpGet getPrintSettingsUrl (Ok >> LoadEditCompositionVoicePrintSettingsResult) (Error >> LoadEditCompositionVoicePrintSettingsResult)
    | LoadEditCompositionVoicePrintSettings, model -> model, Cmd.none
    | LoadEditCompositionVoicePrintSettingsResult (Ok printSettings), Model.EditComposition { VoicePrintSettings = (_, Some Deferred.Loading) } ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voicePrintSettings_) (Some (Deferred.Loaded (Array.toList printSettings))),
        Cmd.none
    | LoadEditCompositionVoicePrintSettingsResult (Error e), Model.EditComposition { VoicePrintSettings = (_, Some Deferred.Loading) } ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.voicePrintSettings_) (Some (Deferred.LoadFailed e)),
        Cmd.none
    | LoadEditCompositionVoicePrintSettingsResult _, model -> model, Cmd.none
    | LoadPdfLib, model  ->
        model,
        Cmd.OfTask.either (fun () -> js.InvokeAsync("import", "./pdf.js").AsTask()) () (Ok >> LoadPdfLibResult) (Error >> LoadPdfLibResult)
    | LoadPdfLibResult (Ok pdfLib), Model.EditComposition _ ->
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.pdfModule_) (Some (Ok pdfLib)),
        Cmd.ofMsg RenderVoicePreview
    | LoadPdfLibResult (Error e), Model.EditComposition _ ->
        printfn "LoadPdfLib Error: %A" e
        model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.pdfModule_) (Some (Error e)),
        Cmd.none
    | LoadPdfLibResult _, model -> model, Cmd.none
    | SelectEditCompositionVoice voiceId, model ->
        model |> Optic.set (editVoices_ >?> EditVoicesModel.selectedVoice_) (Some voiceId),
        Cmd.ofMsg RenderVoicePreview
    | AddEditCompositionVoice, Model.EditComposition { Voices = Some (Deferred.Loaded { Voices = voices }); VoicePrintSettings = (_, Some (Deferred.Loaded printSettings)) } ->
        let newVoice = EditVoiceModel.``new`` printSettings.Head.Key
        model |> Optic.set (editVoices_ >?> EditVoicesModel.voices_) (voices @ [ newVoice ]),
        Cmd.ofMsg (SelectEditCompositionVoice newVoice.Id)
    | AddEditCompositionVoice, model -> model, Cmd.none
    | DeleteEditCompositionVoice voiceId, model ->
        match model |> Optic.get (loadedVoiceWithId_ voiceId) with
        | Some { State = CreatedVoice } ->
            model,
            Cmd.ofMsg (DeleteVoiceResult (voiceId, Ok ()))
        | Some _ ->
            model
            |> Optic.map (loadedVoiceWithId_ voiceId >?> EditVoiceModel.state_) EditVoiceState.toggleDelete,
            Cmd.none
        | None -> model, Cmd.none
    | RenderVoicePreview, Model.EditComposition { PdfModule = Some (Ok pdfLib) } ->
        let data =
            model
            |> Optic.get (selectedVoice_ >?> EditVoiceModel.file_ >?> Option.value_ >?> Result.ok_)
            |> Option.defaultValue [||]
            |> fun file -> {| file = file |}
        model,
        Cmd.OfTask.either (fun () -> pdfLib.InvokeAsync("renderVoice", [| data :> obj |]).AsTask()) () (Ok >> RenderVoicePreviewsResult) (Error >> RenderVoicePreviewsResult)
    | RenderVoicePreview, model -> model, Cmd.none
    | RenderVoicePreviewsResult result, model ->
        printfn "RenderVoicePreviews %A" result
        model |> Optic.set (editVoices_ >?> EditVoicesModel.renderPreviewError_) (Result.errorToOption result),
        Cmd.none
    | SetEditCompositionFormInput (SetTitle text), model ->
        let validationState =
            if text <> "" then ValidationSuccess text
            else ValidationError "Titel darf nicht leer sein"
        model
        |> Optic.map (Model.editComposition_) EditCompositionModel.modify
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.title_ >?> FormInput.text_) text
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.title_ >?> FormInput.validationState_) validationState,
        Cmd.none
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
    | SetEditCompositionFormInput (SetVoiceFile _), model -> model, Cmd.none
    | SetEditCompositionFormInput (SetVoiceFileData (Ok (voiceName, content))), model ->
        model
        |> Optic.map (selectedVoice_ >?> EditVoiceModel.state_) EditVoiceState.modify
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.file_) (Some (Ok content))
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.emptyName_) { Text = voiceName; ValidationState = validateVoiceName voiceName },
        Cmd.ofMsg RenderVoicePreview
    | SetEditCompositionFormInput (SetVoiceFileData (Error e)), model ->
        printfn "SetVoiceFileData Error: %A" e
        model
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.file_) (Some (Error e)),
        Cmd.ofMsg RenderVoicePreview
    | SetEditCompositionFormInput (SetPrintSetting key), model ->
        model
        |> Optic.map (selectedVoice_ >?> EditVoiceModel.state_) EditVoiceState.modify
        |> Optic.set (selectedVoice_ >?> EditVoiceModel.printSetting_) key,
        Cmd.none
    | SaveComposition, Model.EditComposition { SaveState = Some saveState } when saveState.CountSaving > 0 -> model, Cmd.none
    | SaveComposition, Model.EditComposition { State = LoadedComposition _ }  ->
        model
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.saveState_) (Some EditCompositionSaveState.zero)
        |> saveVoices
    | SaveComposition, Model.EditComposition ({ State = CreatedComposition _ } as subModel) ->
        match EditCompositionModel.validateNewCompositionForm subModel with
        | Some dto ->
            model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.saveState_) (Some { EditCompositionSaveState.zero with CountSaving = 1 }),
            Cmd.OfAsync.either httpPost (subModel.SaveUrl, dto) (Ok >> SaveCompositionResult) (Error >> SaveCompositionResult)
        | None -> model, Cmd.none
    | SaveComposition, Model.EditComposition ({ State = ModifiedComposition _ } as subModel) ->
        match EditCompositionModel.validateUpdateCompositionForm subModel with
        | Some dto ->
            model |> Optic.set (Model.editComposition_ >?> EditCompositionModel.saveState_) (Some { EditCompositionSaveState.zero with CountSaving = 1 }),
            Cmd.OfAsync.either httpPatch (subModel.SaveUrl, dto) (Ok >> SaveCompositionResult) (Error >> SaveCompositionResult)
        | None -> model, Cmd.none
    | SaveComposition, model -> model, Cmd.none
    | SaveCompositionResult (Ok composition), model ->
        model
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.state_) (LoadedComposition { GetVoicesUrl = composition.GetVoicesUrl; CreateVoiceUrl = composition.CreateVoiceUrl })
        |> Optic.set (Model.editComposition_ >?> EditCompositionModel.saveUrl_) composition.UpdateUrl
        |> Optic.map (Model.editComposition_ >?> EditCompositionModel.saveState_ >?> Option.value_) EditCompositionSaveState.savingToSaved
        |> saveVoices
    | SaveCompositionResult (Error e), model ->
        model
        |> Optic.map (Model.editComposition_ >?> EditCompositionModel.saveState_ >?> Option.value_) EditCompositionSaveState.savingToSaveError,
        Cmd.none
    | SaveVoiceResult (voiceId, Ok voiceData), model ->
        model
        |> Optic.map (Model.editComposition_ >?> EditCompositionModel.saveState_ >?> Option.value_) EditCompositionSaveState.savingToSaved
        |> Optic.set (loadedVoiceWithId_ voiceId >?> EditVoiceModel.state_) (LoadedVoice (false, { UpdateUrl = voiceData.UpdateUrl; DeleteUrl = voiceData.DeleteUrl })),
        Cmd.none
    | SaveVoiceResult (voiceId, Error e), model ->
        model
        |> Optic.map (Model.editComposition_ >?> EditCompositionModel.saveState_ >?> Option.value_) EditCompositionSaveState.savingToSaveError,
        Cmd.none
    | DeleteVoiceResult (voiceId, Ok ()), model ->
        model
        |> Optic.map (Model.editComposition_ >?> EditCompositionModel.saveState_ >?> Option.value_) EditCompositionSaveState.savingToSaved
        |> Optic.map editVoices_ (EditVoicesModel.remove voiceId),
        Cmd.none
    | DeleteVoiceResult (voiceId, Error e), model ->
        model
        |> Optic.map (Model.editComposition_ >?> EditCompositionModel.saveState_ >?> Option.value_) EditCompositionSaveState.savingToSaveError,
        Cmd.none
