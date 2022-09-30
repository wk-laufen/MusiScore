namespace MusiScore.Client.Admin

open Elmish
open Microsoft.AspNetCore.Components.Forms
open Microsoft.JSInterop
open MusiScore.Shared.DataTransfer.Admin

type FormInputValidation<'a> =
    | NotValidated
    | ValidationError of string
    | ValidationSuccess of 'a

type FormInput<'a> = {
    Text: string
    ValidationState: FormInputValidation<'a>
}
module FormInput =
    let empty =
        { Text = ""; ValidationState = NotValidated }
    let validated text value =
        { Text = text; ValidationState = ValidationSuccess value }

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
    let tryGetSelectedVoice model =
        model.Voices
        |> List.tryFind (fun v -> model.SelectedVoice = Some v.Id )

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
    | ListCompositions of Deferred<CompositionListDto, exn>
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
    | CreateComposition
    | EditComposition of ExistingCompositionDto
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
