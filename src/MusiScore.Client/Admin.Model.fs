namespace MusiScore.Client.Admin

open Elmish
open Microsoft.AspNetCore.Components.Forms
open Microsoft.JSInterop
open MusiScore.Shared.DataTransfer.Admin

type CompositionFilter = {
    Text: string
    ActiveOnly: bool
}
module CompositionFilter =
    let ``new`` = {
        Text = ""
        ActiveOnly = false
    }

type ListCompositionsModel = {
    Compositions: ExistingCompositionDto list
    CompositionFilter: CompositionFilter
    GetPrintSettingsUrl: string
    CreateCompositionUrl: string
    ExportCompositionsUrl: string
    ExportCompositionsState: Deferred<unit, exn> option
    CompositionDeleteState: (ExistingCompositionDto * Deferred<unit, exn> option) option
}
module ListCompositionsModel =
    let init (compositions: CompositionListDto) = {
        Compositions = compositions.Compositions |> Array.toList
        CompositionFilter = CompositionFilter.``new``
        GetPrintSettingsUrl = compositions.PrintSettingsUrl
        CreateCompositionUrl = compositions.CreateCompositionUrl
        ExportCompositionsUrl = compositions.ExportCompositionsUrl
        ExportCompositionsState = None
        CompositionDeleteState = None
    }

type ExistingVoiceData = {
    UpdateUrl: string
    DeleteUrl: string
}

type EditVoiceState =
    | LoadedVoice of isMarkedForDeletion: bool * ExistingVoiceData
    | CreatedVoice
    | ModifiedVoice of isMarkedForDeletion: bool * ExistingVoiceData
module EditVoiceState =
    let modify = function
        | CreatedVoice -> CreatedVoice
        | LoadedVoice (_, data)
        | ModifiedVoice (_, data) -> ModifiedVoice (false, data)
    let toggleDelete = function
        | CreatedVoice -> CreatedVoice
        | LoadedVoice (isMarkedForDeletion, data) -> LoadedVoice (not isMarkedForDeletion, data)
        | ModifiedVoice (isMarkedForDeletion, data) -> ModifiedVoice (not isMarkedForDeletion, data)

type EditVoiceModel = {
    Id: System.Guid
    State: EditVoiceState
    Name: FormInput<string>
    File: Result<byte[], exn> option
    PrintSetting: string
}
module EditVoiceModel =
    let ``new`` printSetting = {
        Id = System.Guid.NewGuid()
        State = CreatedVoice
        Name = FormInput.empty
        File = None
        PrintSetting = printSetting
    }
    let fromExistingVoices : ExistingVoiceDto seq -> EditVoiceModel list =
        Seq.sortBy (fun v -> v.Name)
        >> Seq.map (fun v ->
            {
                Id = System.Guid.NewGuid()
                State = LoadedVoice (false, { UpdateUrl = v.UpdateUrl; DeleteUrl = v.DeleteUrl })
                Name = FormInput.validated v.Name v.Name
                File = Some (Ok v.File)
                PrintSetting = v.PrintSetting
            }
        )
        >> Seq.toList
    let validateNewVoiceForm v : CreateVoiceDto option =
        match v.Name, v.File, v.PrintSetting with
        | { ValidationState = ValidationSuccess name }, Some (Ok file), printSetting ->
            Some { Name = name; File = file; PrintSetting = printSetting }
        | _ -> None
    let validateUpdateVoiceForm v : UpdateVoiceDto option =
        let applyName =
            match v.Name with
            | { ValidationState = ValidationSuccess name } -> (fun (m: UpdateVoiceDto) -> Some { m with Name = Some name })
            | { ValidationState = ValidationError _ } -> (fun _ -> None)
            | { ValidationState = NotValidated } -> (fun _ -> None)
        let applyFile =
            match v.File with
            | Some (Ok file) -> (fun (m: UpdateVoiceDto) -> Some { m with File = Some file })
            | Some (Error _) -> (fun _ -> None)
            | None -> (fun _ -> None)
        let init = Some { Name = None; File = None; PrintSetting = Some v.PrintSetting }
        (init, [applyName; applyFile])
        ||> List.fold (fun s t -> s |> Option.bind t)

type EditVoicesModel = {
    SelectedVoice: System.Guid option
    Voices: EditVoiceModel list
    RenderPreviewError: exn option
}
module EditVoicesModel =
    let ``new`` = {
        SelectedVoice = None
        Voices = []
        RenderPreviewError = None
    }
    let init voices = {
        SelectedVoice = voices |> List.tryHead |> Option.map (fun v -> v.Id)
        Voices = voices
        RenderPreviewError = None
    }
    let tryGetSelectedVoice model =
        model.Voices
        |> List.tryFind (fun v -> model.SelectedVoice = Some v.Id )
    let remove voiceId model =
        match model.Voices |> List.tryFindIndex (fun v -> Some v.Id = model.SelectedVoice) with
        | Some index ->
            let selectedVoice =
                let isDeletedVoiceSelected = model.SelectedVoice = Some voiceId
                if isDeletedVoiceSelected then
                    model.Voices |> List.tryItem (index + 1)
                    |> Option.orElse (model.Voices |> List.tryItem (index - 1))
                    |> Option.map (fun v -> v.Id)
                else
                    model.SelectedVoice
            { model with
                Voices = model.Voices |> List.filter (fun v -> v.Id <> voiceId)
                SelectedVoice = selectedVoice
            }
        | None -> model

type ExistingCompositionData = {
    GetVoicesUrl: string
    CreateVoiceUrl: string
}

type EditCompositionState =
    | LoadedComposition of ExistingCompositionData
    | CreatedComposition
    | ModifiedComposition of ExistingCompositionData
module EditCompositionState =
    let modify = function
        | CreatedComposition -> CreatedComposition
        | LoadedComposition data
        | ModifiedComposition data -> ModifiedComposition data

type EditCompositionSaveState = {
    CountSaving: int
    CountSaved: int
    CountSaveError: int
    CountNotSaving: int
}
module EditCompositionSaveState =
    let zero = {
        CountSaving = 0
        CountSaved = 0
        CountSaveError = 0
        CountNotSaving = 0
    }
    let addSaving v = { v with CountSaving = v.CountSaving + 1 }
    let savingToSaved v = { v with CountSaving = v.CountSaving - 1; CountSaved = v.CountSaved + 1 }
    let savingToSaveError v = { v with CountSaving = v.CountSaving - 1; CountSaveError = v.CountSaveError + 1 }

type EditCompositionModel = {
    State: EditCompositionState
    Title: FormInput<string>
    SaveUrl: string
    SaveState: EditCompositionSaveState option
    Voices: Deferred<EditVoicesModel, exn> option
    VoicePrintSettings: string * Deferred<VoicePrintSettingDto list, exn> option
    PdfModule: Result<IJSObjectReference, exn> option
}
module EditCompositionModel =
    let modify (v: EditCompositionModel) =
        { v with State = EditCompositionState.modify v.State; SaveState = None }

    let validateNewCompositionForm v : NewCompositionDto option =
        match v.Title with
        | { ValidationState = ValidationSuccess title } -> Some { Title = title }
        | _ -> None
    let validateUpdateCompositionForm v : CompositionUpdateDto option =
        match v.Title with
        | { ValidationState = ValidationSuccess title } -> Some { Title = Some title; IsActive = None }
        | _ -> None

type Model =
    | ListCompositions of Deferred<ListCompositionsModel, exn>
    | EditComposition of EditCompositionModel

type SetEditCompositionFormInput =
    | SetTitle of string
    | SetVoiceName of string
    | SetVoiceFile of IBrowserFile
    | SetVoiceFileData of Result<(string * byte[]), exn>
    | SetPrintSetting of string

type Message =
    | LoadCompositions
    | LoadCompositionsResult of Result<CompositionListDto, exn>
    | ToggleActivateComposition of ExistingCompositionDto
    | UpdateCompositionResult of currentComposition: ExistingCompositionDto * newComposition: ExistingCompositionDto * Result<unit, exn>
    | ChangeCompositionFilterText of string
    | ShowActiveCompositionsOnly of bool
    | ExportCompositions
    | ExportCompositionsResult of Result<unit, exn>
    | CreateComposition
    | EditComposition of ExistingCompositionDto
    | DeleteComposition of ExistingCompositionDto
    | DeleteCompositionResult of Result<unit, exn>
    | LoadEditCompositionVoices
    | LoadEditCompositionVoicesResult of Result<ExistingVoiceDto array, exn>
    | LoadEditCompositionVoicePrintSettings
    | LoadEditCompositionVoicePrintSettingsResult of Result<VoicePrintSettingDto array, exn>
    | SelectEditCompositionVoice of System.Guid
    | AddEditCompositionVoice
    | DeleteEditCompositionVoice of System.Guid
    | LoadPdfLib
    | LoadPdfLibResult of Result<IJSObjectReference, exn>
    | RenderVoicePreview
    | RenderVoicePreviewsResult of Result<unit, exn>
    | SetEditCompositionFormInput of SetEditCompositionFormInput
    | SaveComposition
    | SaveCompositionResult of Result<ExistingCompositionDto, exn>
    | SaveVoiceResult of System.Guid * Result<ExistingVoiceDto, exn>
    | DeleteVoiceResult of System.Guid * Result<unit, exn>

module Model =
    let init =
        ListCompositions Deferred.Loading,
        Cmd.ofMsg LoadCompositions
