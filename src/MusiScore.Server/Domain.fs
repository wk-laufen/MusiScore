namespace MusiScore.Server

open FsToolkit.ErrorHandling
open System
open System.Text.RegularExpressions

type Voice = {
    Id: string
    Name: string
}

type ActiveComposition = {
    Id: string
    Title: string
    Composer: string option
    Arranger: string option
    Voices: Voice list
}

type Composition = {
    Id: string
    Title: string
    Composer: string option
    Arranger: string option
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
}

type PrintConfigCreateError =
    | PrintConfigExists

type PrintableVoice = {
    File: byte[]
    PrintSettings: PrintSettings
}

type NewComposition = {
    Title: string
    Composer: string option
    Arranger: string option
    IsActive: bool
}

type CompositionUpdate = {
    Title: string option
    Composer: string option option
    Arranger: string option option
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
        if System.String.IsNullOrWhiteSpace title then return! Error "EmptyTitle"
        else return title
    }

    let newCompositionDto (v: MusiScore.Shared.DataTransfer.Admin.NewCompositionDto) = validation {
        let! title = compositionTitle v.Title
        return { NewComposition.Title = title; Composer = v.Composer; Arranger = v.Arranger; IsActive = v.IsActive |> Option.defaultValue false }
    }

    let private noneIfEmpty v =
        if System.String.IsNullOrWhiteSpace v then None
        else Some v

    let compositionUpdateDto (v: MusiScore.Shared.DataTransfer.Admin.CompositionUpdateDto) = validation {
        let! title = v.Title |> Option.map compositionTitle |> Validation.accumulateOption
        let composer = if v.UpdateComposer |> Option.defaultValue false then Some (v.Composer |> Option.bind noneIfEmpty) else None
        let arranger = if v.UpdateArranger |> Option.defaultValue false then Some (v.Arranger |> Option.bind noneIfEmpty) else None
        return { Title = title; Composer = composer; Arranger = arranger; IsActive = v.IsActive }
    }

    let voiceName (name: string) = validation {
        if System.String.IsNullOrWhiteSpace name then return! Error "EmptyName"
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
        if System.String.IsNullOrWhiteSpace name then return! Error "EmptyName"
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
        return { Name = name; ReorderPagesAsBooklet = v.ReorderPagesAsBooklet; CupsCommandLineArgs = v.CupsCommandLineArgs }
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
