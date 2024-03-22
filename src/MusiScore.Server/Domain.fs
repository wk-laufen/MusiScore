namespace MusiScore.Server

open FsToolkit.ErrorHandling

type Voice = {
    Id: string
    Name: string
}

type ActiveComposition = {
    Id: string
    Title: string
    Voices: Voice list
}

type Composition = {
    Id: string
    Title: string
    IsActive: bool
}

type PrintSetting =
    | Duplex
    | A4ToA3Duplex
    | A4ToBooklet
module PrintSetting =
    let toDto v =
        match v with
        | Duplex -> "duplex"
        | A4ToA3Duplex -> "a4-to-a3-duplex"
        | A4ToBooklet -> "a4-to-booklet"

type PrintableVoice = {
    File: byte[]
    PrintSetting: PrintSetting
}

type NewComposition = {
    Title: string
    IsActive: bool
}

type CompositionUpdate = {
    Title: string option
    IsActive: bool option
}

type CreateVoice = {
    Name: string
    File: byte[]
    PrintSetting: PrintSetting
}

type UpdateVoice = {
    Name: string option
    File: byte[] option
    PrintSetting: PrintSetting option
}

type FullVoice = {
    Id: string
    Name: string
    File: byte[]
    PrintSetting: PrintSetting
}

type VoicePrintSetting = {
    Key: PrintSetting
    Name: string
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

    let newCompositionDto (v: MusiScore.Shared.DataTransfer.Admin.NewCompositionDto) isActive = validation {
        let! title = compositionTitle v.Title
        return { NewComposition.Title = title; IsActive = isActive }
    }

    let compositionUpdateDto (v: MusiScore.Shared.DataTransfer.Admin.CompositionUpdateDto) = validation {
        let! title = v.Title |> Option.map compositionTitle |> Validation.accumulateOption
        return { Title = title; IsActive = v.IsActive }
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

    let printSetting (name: string) = validation {
        if name.Equals("duplex", System.StringComparison.InvariantCultureIgnoreCase) then return Duplex
        elif name.Equals("a4-to-a3-duplex", System.StringComparison.InvariantCultureIgnoreCase) then return A4ToA3Duplex
        elif name.Equals("a4-to-booklet", System.StringComparison.InvariantCultureIgnoreCase) then return A4ToBooklet
        else return! Error "UnknownPrintSetting"
    }

    let createVoiceDto (v: MusiScore.Shared.DataTransfer.Admin.CreateVoiceDto) = validation {
        let! name = voiceName v.Name
        and! file = voiceFile v.File
        and! printSetting = printSetting v.PrintSetting
        return { CreateVoice.Name = name; File = file; PrintSetting = printSetting }
    }

    let updateVoiceDto (v: MusiScore.Shared.DataTransfer.Admin.UpdateVoiceDto) = validation {
        let! name = v.Name |> Option.map voiceName |> Validation.accumulateOption
        and! file = v.File |> Option.map voiceFile |> Validation.accumulateOption
        and! printSetting = v.PrintSetting |> Option.map printSetting |> Validation.accumulateOption
        return { Name = name; File = file; PrintSetting = printSetting }
    }
