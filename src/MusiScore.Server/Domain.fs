namespace MusiScore.Server

open FsToolkit.ErrorHandling
open System
open System.Text.RegularExpressions

type Voice = {
    Id: string
    Name: string
}
module Voice =
    let tryGetSortOrder (patterns: Regex list) voiceName =
        patterns
        |> List.indexed
        |> List.tryPick (fun (i, v) ->
            let m = v.Match(voiceName)
            if m.Success then Some (i + 1)
            else None
        )
    let sortBySortOrder (patterns: Regex list) list nameFn =
        list
        |> List.sortBy (nameFn >> fun voiceName ->
            let patternSortOrder =
                tryGetSortOrder patterns voiceName
                |> Option.defaultValue Int32.MaxValue
            (patternSortOrder, voiceName.ToLowerInvariant())
        )

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

type CreateVoice = {
    Name: string
    File: byte[]
    PrintConfig: string
}

type UpdateVoice = {
    Name: string option
    File: byte[] option
    PrintConfig: string option
}

type FullVoice = {
    Id: string
    Name: string
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

    let private noneIfEmpty v =
        if String.IsNullOrWhiteSpace v then None
        else Some v

    let compositionUpdateDto (v: MusiScore.Shared.DataTransfer.Admin.CompositionUpdateDto) = validation {
        let! title = v.Title |> Option.map compositionTitle |> Validation.accumulateOption
        let! tagsToAdd = v.TagsToAdd |> Option.defaultValue [] |> List.map newTag |> List.sequenceValidationA |> Validation.map (List.map AddTag)
        let! tagsToRemove = v.TagsToRemove |> Option.defaultValue [] |> List.map tagKey |> List.sequenceValidationA |> Validation.map (List.map RemoveTag)
        return { Title = title; TagUpdates = tagsToRemove @ tagsToAdd; IsActive = v.IsActive }
    }

    let voiceName (name: string) = validation {
        if String.IsNullOrWhiteSpace name then return! Error "EmptyName"
        else return name
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

    let createVoiceDto (v: MusiScore.Shared.DataTransfer.Admin.CreateVoiceDto) = validation {
        let! name = voiceName v.Name
        and! file = voiceFile v.File
        and! printConfig = printConfigKey v.PrintConfig
        return { CreateVoice.Name = name; File = file; PrintConfig = printConfig }
    }

    let updateVoiceDto (v: MusiScore.Shared.DataTransfer.Admin.UpdateVoiceDto) = validation {
        let! name = v.Name |> Option.map voiceName |> Validation.accumulateOption
        and! file = v.File |> Option.map voiceFile |> Validation.accumulateOption
        and! printConfig = v.PrintConfig |> Option.map printConfigKey |> Validation.accumulateOption
        return { Name = name; File = file; PrintConfig = printConfig }
    }

    let regex v = validation {
        try
            return Regex(v)
        with _ -> return! Error ()
    }

    let voiceSortOrderPatterns (v: string list) = validation {
        return! v
            |> List.mapi (fun i v -> 
                if v = "" then Error [ {| Type = "EmptyPattern"; LineIndex = i |} ]
                else regex v |> Validation.mapError (fun () -> {| Type = "InvalidPattern"; LineIndex = i |})
            )
            |> List.traverseValidationA id
    }

module Serialize =
    module Print =
        let existingTag (v: ExistingTag) : MusiScore.Shared.DataTransfer.Print.ExistingTag =
            let settings = {|
                OverviewDisplayFormat = v.Settings.OverviewDisplayFormat
            |}
            { Key = v.Key; Title = v.Title; Settings = settings; Value = v.Value }

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
