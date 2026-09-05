namespace MusiScore.Server

open FsToolkit.ErrorHandling
open System
open System.Text.RegularExpressions

type NewVoiceDefinitionGroup = {
    Name: string
    SortOrder: int option
}

type VoiceDefinitionGroupUpdate = {
    Name: string option
    SortOrder: int option
}

type SaveVoiceDefinitionGroupError = DuplicateVoiceDefinitionGroupName

type VoiceDefinition = {
    Id: string
    Name: string
    MemberCount: int
}

type VoiceDefinitionWithStats = {
    Id: string
    Name: string
    MemberCount: int
    Compositions: string list
}

type VoiceDefinitionGroup = {
    Id: string
    Name: string
}

type VoiceDefinitionGroupWithVoices = {
    Group: VoiceDefinitionGroup option
    VoiceDefinitions: VoiceDefinitionWithStats list
}

type NewVoiceDefinition = {
    Name: string
    SortOrder: int option
    GroupId: string option
    MemberCount: int
}

type Voice =
    {
        Id: string
        Names: string list
        PrintConfigId: string
    }

type TagValueType = TagValueTypeText | TagValueTypeMultiLineText

type CompositionTagType = {
    Key: string
    Name: string
    Settings: {|
        ValueType: TagValueType
        OverviewDisplayFormat: {| Order: int; Format: string |} option
    |}
}

type ExistingTag = {
    Key: string
    Title: string
    Settings: {|
        ValueType: TagValueType
        OverviewDisplayFormat: {| Order: int; Format: string |} option
    |}
    Value: string option
    OtherValues: string list
}

type ActiveComposition = {
    Id: string
    Title: string
    Tags: ExistingTag list
    Voices: Voice list
}

type Composition = {
    Id: string
    Title: string
    Tags: ExistingTag list
    IsActive: bool
    Voices: Voice list
}

type PrintSettings = {
    ReorderPagesAsBooklet: bool
    CupsCommandLineArgs: string
}

type PrintConfig = {
    Key: string
    Name: string
    SortOrder: int
    Settings: PrintSettings
}

type CompositionVoices = {
    Title: string
    /// voice names in voice definition order
    Voices: string list
}

type PrintConfigWithStats = {
    Key: string
    Name: string
    SortOrder: int
    Settings: PrintSettings
    /// compositions using this print config, most voices first
    Compositions: CompositionVoices list
}

type PrintConfigUpdate = {
    Name: string option
    ReorderPagesAsBooklet: bool option
    CupsCommandLineArgs: string option
    SortOrder: int option
}

type PrintConfigCreateError =
    | PrintConfigExists

type PrintConfigDeleteError =
    | InvalidReplacementConfigId

type PrintableVoice = {
    File: byte[]
    PrintSettings: PrintSettings
}

type NewTag = {
    Key: string
    Value: string
}
type NewComposition = {
    Title: string
    Tags: NewTag list
    IsActive: bool
}

type TagUpdate =
    | AddTag of NewTag
    | RemoveTag of key: string

type CompositionUpdate = {
    Title: string option
    TagUpdates: TagUpdate list
    IsActive: bool option
}

type VoiceDefinitionReference =
    | CreateNewDefinition of NewVoiceDefinition
    | UseExistingDefinition of string

type CreateVoice = {
    Definitions: VoiceDefinitionReference list
    File: byte[]
    PrintConfig: string
}

type CreateVoiceError = UnknownPrintConfig

type UpdateVoice = {
    Definitions: VoiceDefinitionReference list option
    File: byte[] option
    PrintConfig: string option
}

type VoiceDefinitionUpdate = {
    Name: string option
    SortOrder: int option
    /// `None` leaves the group untouched, `Some None` removes the voice definition from its group.
    GroupId: string option option
    MemberCount: int option
}

type SaveVoiceError =
    | DuplicateVoiceDefinitionName
    | UnknownVoiceDefinitionGroup

type VoiceDefinitionDeleteError = InvalidReplacementVoiceDefinitionId

type FullVoice =
    {
        Id: string
        Names: string list
        File: byte[]
        PrintConfig: string
    }

module Validation =
    // see https://hoogle.haskell.org/?hoogle=Maybe%20(Result%20a%20b)%20-%3E%20Result%20a%20(Maybe%20b)
    let accumulateOption = function
        | Some (Ok v) -> Ok (Some v)
        | Some (Error v) -> Error v
        | None -> Ok None

module Parse =
    let compositionTitle (title: string) = validation {
        if String.IsNullOrWhiteSpace title then return! Error "EmptyTitle"
        else return title
    }

    let tagKey v = validation {
        if String.IsNullOrWhiteSpace v then return! Error "EmptyTagKey"
        else return v
    }

    let newTag (v: MusiScore.Shared.DataTransfer.Admin.NewTag) = validation {
        let! key = tagKey v.Key
        return { Key = key; Value = v.Value }
    }

    let newCompositionDto (v: MusiScore.Shared.DataTransfer.Admin.NewCompositionDto) = validation {
        let! title = compositionTitle v.Title
        let! tags = v.Tags |> List.map newTag |> List.sequenceValidationA
        return { NewComposition.Title = title; Tags = tags; IsActive = v.IsActive |> Option.defaultValue false }
    }

    let compositionUpdateDto (v: MusiScore.Shared.DataTransfer.Admin.CompositionUpdateDto) = validation {
        let! title = v.Title |> Option.map compositionTitle |> Validation.accumulateOption
        let! tagsToAdd = v.TagsToAdd |> Option.defaultValue [] |> List.map newTag |> List.sequenceValidationA |> Validation.map (List.map AddTag)
        let! tagsToRemove = v.TagsToRemove |> Option.defaultValue [] |> List.map tagKey |> List.sequenceValidationA |> Validation.map (List.map RemoveTag)
        return { Title = title; TagUpdates = tagsToRemove @ tagsToAdd; IsActive = v.IsActive }
    }

    let voiceDefinitionReference (voiceDefinitions: VoiceDefinition list) (name: string) = validation {
        if String.IsNullOrWhiteSpace name then return! Error "EmptyName"
        else
            match voiceDefinitions |> List.tryFind (fun voiceDefinition -> voiceDefinition.Name = name) with
            | Some v -> return UseExistingDefinition v.Id
            | None -> return CreateNewDefinition { Name = name; SortOrder = None; GroupId = None; MemberCount = 1 }
    }

    let voiceFile (content: byte array) = validation {
        if isNull content || Array.isEmpty content then return! Error "EmptyFile"
        elif not <| PDF.isValid content then return! Error "InvalidFile"
        else return content
    }

    let printConfigKey (name: string) = validation {
        if not <| Regex.IsMatch(name, @"^[a-zA-Z0-9_-]+$") then
            return! Error "InvalidKey"
        else return name
    }

    let printConfigName (name: string) = validation {
        if String.IsNullOrWhiteSpace name then return! Error "EmptyName"
        else return name
    }

    let printConfig (v: MusiScore.Shared.DataTransfer.Admin.NewPrintConfigDto) = validation {
        let! key = printConfigKey v.Key
        and! name = printConfigName v.Name
        let cupsCommandLineArgs = v.CupsCommandLineArgs |> Option.ofObj |> Option.defaultValue ""
        return { Key = key; Name = name; SortOrder = v.SortOrder; Settings = { ReorderPagesAsBooklet = v.ReorderPagesAsBooklet; CupsCommandLineArgs = cupsCommandLineArgs } }
    }

    let printConfigUpdateDto (v: MusiScore.Shared.DataTransfer.Admin.PrintConfigUpdateDto) = validation {
        let! name = v.Name |> Option.map printConfigName |> Validation.accumulateOption
        return { Name = name; ReorderPagesAsBooklet = v.ReorderPagesAsBooklet; CupsCommandLineArgs = v.CupsCommandLineArgs; SortOrder = v.SortOrder }
    }

    let voiceDefinitionReferences (voiceDefinitions: VoiceDefinition list) (names: string list) = validation {
        if List.isEmpty names then return! Error "EmptyName"
        else
            let! references = names |> List.map (voiceDefinitionReference voiceDefinitions) |> List.sequenceValidationA
            return references |> List.distinct
    }

    let createVoiceDto (v: MusiScore.Shared.DataTransfer.Admin.CreateVoiceDto) (voiceDefinitions: VoiceDefinition list) = validation {
        let! definitions = voiceDefinitionReferences voiceDefinitions v.Names
        and! file = voiceFile v.File
        and! printConfig = printConfigKey v.PrintConfig
        return { CreateVoice.Definitions = definitions; File = file; PrintConfig = printConfig }
    } 

    let updateVoiceDto (v: MusiScore.Shared.DataTransfer.Admin.UpdateVoiceDto) (voiceDefinitions: VoiceDefinition list) = validation {
        let! definitions = v.Names |> Option.map (voiceDefinitionReferences voiceDefinitions) |> Validation.accumulateOption
        and! file = v.File |> Option.map voiceFile |> Validation.accumulateOption
        and! printConfig = v.PrintConfig |> Option.map printConfigKey |> Validation.accumulateOption
        return { Definitions = definitions; File = file; PrintConfig = printConfig }
    }

    let regex v = validation {
        try
            return Regex(v)
        with _ -> return! Error ()
    }

    let voiceName (name: string) = validation {
        if String.IsNullOrWhiteSpace name then return! Error "EmptyName"
        else return name
    }

    let voiceMemberCount (v: int) = validation {
        if v < 0 then return! Error "InvalidMemberCount"
        else return v
    }

    let createVoiceDefinition (v: MusiScore.Shared.DataTransfer.Admin.CreateVoiceDefinitionDto) : Validation<NewVoiceDefinition, _> = validation {
        let! name = voiceName v.Name
        let! memberCount = voiceMemberCount v.MemberCount
        return { Name = name; SortOrder = Some v.SortOrder; GroupId = v.GroupId; MemberCount = memberCount }
    }

    let updateVoiceDefinition (v: MusiScore.Shared.DataTransfer.Admin.UpdateVoiceDefinitionDto) = validation {
        let! name = v.Name |> Option.map voiceName |> Validation.accumulateOption
        let! memberCount = v.MemberCount |> Option.map voiceMemberCount |> Validation.accumulateOption
        return { Name = name; SortOrder = v.SortOrder; GroupId = v.Group |> Option.map _.Id; MemberCount = memberCount }
    }

    let voiceDefinitionGroupName (name: string) = validation {
        if String.IsNullOrWhiteSpace name then return! Error "EmptyName"
        else return name
    }

    let createVoiceDefinitionGroup (v: MusiScore.Shared.DataTransfer.Admin.CreateVoiceDefinitionGroupDto) : Validation<NewVoiceDefinitionGroup, _> = validation {
        let! name = voiceDefinitionGroupName v.Name
        return { Name = name; SortOrder = Some v.SortOrder }
    }

    let updateVoiceDefinitionGroup (v: MusiScore.Shared.DataTransfer.Admin.UpdateVoiceDefinitionGroupDto) = validation {
        let! name = v.Name |> Option.map voiceDefinitionGroupName |> Validation.accumulateOption
        return { VoiceDefinitionGroupUpdate.Name = name; SortOrder = v.SortOrder }
    }

module Serialize =
    module Print =
        let existingTag (v: ExistingTag) =
            let settings = {|
                OverviewDisplayFormat = v.Settings.OverviewDisplayFormat
            |}
            {| Key = v.Key; Title = v.Title; Settings = settings; Value = v.Value |}

        let voiceDefinitionGroup (v: VoiceDefinitionGroupWithVoices) =
            {|
                GroupName = v.Group |> Option.map _.Name
                Voices = v.VoiceDefinitions |> List.map _.Name
            |}

    module Admin =
        let existingTag (v: ExistingTag) : MusiScore.Shared.DataTransfer.Admin.ExistingTag =
            let settings = {|
                ValueType =
                    match v.Settings.ValueType with
                    | TagValueTypeText -> "text"
                    | TagValueTypeMultiLineText -> "multi-line-text"
                OverviewDisplayFormat = v.Settings.OverviewDisplayFormat
            |}
            { Key = v.Key; Title = v.Title; Settings = settings; Value = v.Value; OtherValues = v.OtherValues }
        let printConfigDeleteError e =
            match e with
            | InvalidReplacementConfigId -> "InvalidReplacementConfigId"

        let voiceDefinition (url: string) (v: VoiceDefinition) =
            {|
                Id = v.Id
                Name = v.Name
                MemberCount = v.MemberCount
                Links = {|
                    Self = url
                |}
            |}

        let voiceDefinitionWithStats (url: string) (v: VoiceDefinitionWithStats) =
            {|
                Id = v.Id
                Name = v.Name
                MemberCount = v.MemberCount
                Compositions = v.Compositions
                Links = {|
                    Self = url
                |}
            |}

        let voiceDefinitionGroup (url: string) (v: VoiceDefinitionGroup) =
            {|
                Id = v.Id
                Name = v.Name
                Links = {|
                    Self = url
                |}
            |}

        let saveVoiceError e =
            match e with
            | DuplicateVoiceDefinitionName -> "DuplicateName"
            | UnknownVoiceDefinitionGroup -> "UnknownGroup"

        let saveVoiceDefinitionGroupError e =
            match e with
            | DuplicateVoiceDefinitionGroupName -> "DuplicateName"

        let voiceDefinitionDeleteError e =
            match e with
            | InvalidReplacementVoiceDefinitionId -> "InvalidReplacementVoiceDefinitionId"
