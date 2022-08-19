namespace MusiScore.Server

type ActiveComposition = {
    Id: string
    Title: string
}

type Composition = {
    Id: string
    Title: string
    IsActive: bool
}

type Voice = {
    Id: string
    Name: string
}

type PrintSetting =
    | Duplex
    | A4ToA3Duplex
    | A4ToBooklet
module PrintSetting =
    let tryParseDto (v: string) =
        if v.Equals("duplex", System.StringComparison.InvariantCultureIgnoreCase) then Ok Duplex
        elif v.Equals("a4-to-a3-duplex", System.StringComparison.InvariantCultureIgnoreCase) then Ok A4ToA3Duplex
        elif v.Equals("a4-to-booklet", System.StringComparison.InvariantCultureIgnoreCase) then Ok A4ToBooklet
        else Error $"Invalid print setting \"%s{v}\""
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
module NewComposition =
    let tryParseDto (v: MusiScore.Shared.DataTransfer.Admin.NewCompositionDto) isActive =
        if System.String.IsNullOrWhiteSpace v.Title then Error "Invalid composition title"
        else Ok { Title = v.Title; IsActive = isActive }

type CompositionUpdate = {
    Title: string option
    IsActive: bool option
}
module CompositionUpdate =
    let tryParseDto (v: MusiScore.Shared.DataTransfer.Admin.CompositionUpdateDto) =
        let title =
            match v.Title with
            | Some title when System.String.IsNullOrWhiteSpace title -> Error "Invalid composition title"
            | title -> Ok title
        match title with
        | Ok title -> Ok { Title = title; IsActive = v.IsActive }
        | Error message -> Error message

type CreateVoice = {
    Name: string
    File: byte[]
    PrintSetting: PrintSetting
}
module CreateVoice =
    let tryParseDto (v: MusiScore.Shared.DataTransfer.Admin.CreateVoiceDto) =
        match v.Name, v.File, PrintSetting.tryParseDto v.PrintSetting with
        | name, _, _ when System.String.IsNullOrWhiteSpace name -> Error "Invalid voice name"
        | _, file, _ when isNull file || Array.isEmpty file -> Error "Invalid voice file" // TODO check if valid pdf?
        | _, _, Error printSettingMessage -> Error printSettingMessage
        | name, file, Ok printSetting ->
            Ok { Name = name; File = file; PrintSetting = printSetting }

type UpdateVoice = {
    Name: string option
    File: byte[] option
    PrintSetting: PrintSetting option
}
module UpdateVoice =
    let tryParseDto (v: MusiScore.Shared.DataTransfer.Admin.UpdateVoiceDto) =
        let printSetting =
            match v.PrintSetting |> Option.map PrintSetting.tryParseDto with
            | Some (Ok printSetting) -> Ok (Some printSetting)
            | Some (Error printSettingMessage) -> Error printSettingMessage
            | None -> Ok None
        match v.Name, v.File, printSetting with
        | Some name, _, _ when System.String.IsNullOrWhiteSpace name -> Error "Invalid voice name"
        | _, Some file, _ when isNull file || Array.isEmpty file -> Error "Invalid voice file" // TODO check if valid pdf?
        | _, _, Error printSettingMessage -> Error printSettingMessage
        | name, file, Ok printSetting ->
            Ok { Name = name; File = file; PrintSetting = printSetting }

type FullVoice = {
    Id: string
    Name: string
    PrintSetting: PrintSetting
}
