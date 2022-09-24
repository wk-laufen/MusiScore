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
    | LoadedVoice of ExistingVoiceData
    | CreatedVoice
    | ModifiedVoice of ExistingVoiceData
    | DeletedVoice of ExistingVoiceData
module EditVoiceState =
    let modify = function
        | CreatedVoice -> CreatedVoice
        | LoadedVoice data
        | ModifiedVoice data -> ModifiedVoice data
        | DeletedVoice data -> DeletedVoice data

type EditVoiceModel = {
    Id: System.Guid
    State: EditVoiceState
    Name: FormInput<string>
    File: Result<byte[], exn> option
    PrintSetting: string
    SaveState: Deferred<unit, exn> option
}
module EditVoiceModel =
    let ``new`` printSetting = {
        Id = System.Guid.NewGuid()
        State = CreatedVoice
        Name = FormInput.empty
        File = None
        PrintSetting = printSetting
        SaveState = None
    }
    let validateNewVoiceForm v : CreateVoiceDto option =
        match v.Name, v.File, v.PrintSetting with
        | { ValidationState = ValidationSuccess name }, Some (Ok file), printSetting ->
            Some { Name = name; File = file; PrintSetting = printSetting }
        | _ -> None
    let validateUpdateVoiceForm v : UpdateVoiceDto option =
        let applyName =
            match v.Name with
            | { ValidationState = ValidationSuccess name } -> Some (fun (m: UpdateVoiceDto) -> { m with Name = Some name })
            | _ -> None
        let applyFile =
            match v.File with
            | Some (Ok file) -> Some (fun (m: UpdateVoiceDto) -> { m with File = Some file })
            | _ -> None
        match [applyName; applyFile] |> List.choose id with
        | [] -> None
        | filter ->
            let init = { Name = None; File = None; PrintSetting = Some v.PrintSetting }
            (init, filter)
            ||> List.fold (fun s t -> t s)
            |> Some 
        

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

type EditCompositionModel = {
    State: EditCompositionState
    Title: FormInput<string>
    SaveUrl: string
    SaveState: Deferred<unit, exn> option
    Voices: Deferred<EditVoicesModel, exn> option
    VoicePrintSettings: string * Deferred<VoicePrintSettingDto list, exn> option
    PdfModule: Result<IJSObjectReference, exn> option
}
module EditCompositionModel =
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
