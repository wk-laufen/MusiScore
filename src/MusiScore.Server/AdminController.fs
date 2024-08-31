﻿namespace MusiScore.Server

open Microsoft.AspNetCore.Mvc
open MusiScore.Shared.DataTransfer.Admin
open System
open System.IO
open System.Text

[<ApiController>]
[<Route("api/admin")>]
type AdminController(db: Db, printer: Printer) =
    inherit ControllerBase()

    [<Route("compositions")>]
    [<HttpGet>]
    member this.GetCompositions () =
        async {
            let! compositions = db.GetCompositions()
            return {
                Compositions =
                    compositions
                    |> Seq.sortBy (fun v -> v.Title)
                    |> Seq.map (fun v -> {
                        Title = v.Title
                        IsActive = v.IsActive
                        Links = {|
                            Self = this.Url.Action(nameof(this.UpdateComposition), {| compositionId = v.Id |})
                            Voices = this.Url.Action(nameof(this.CreateVoice), {| compositionId = v.Id |})
                        |}
                    })
                    |> Seq.toArray
                Links = {|
                    PrintSettings = this.Url.Action(nameof(this.GetPrintSettings))
                    InferPrintSetting = this.Url.Action(nameof(this.InferPrintSetting))
                    TestPrintSetting = this.Url.Action(nameof(this.TestPrintSetting))
                    Composition = this.Url.Action(nameof(this.CreateComposition))
                    Export = this.Url.Action(nameof(this.ExportCompositions))
                |}
            }
        }

    [<Route("print-settings")>]
    [<HttpGet>]
    member _.GetPrintSettings () =
        async {
            let! printSettings = db.GetPrintSettings()
            return
                printSettings
                |> List.map (fun v -> {
                    Key = PrintSetting.toDto v.Key
                    Name = v.Name
                })
        }

    [<Route("print-setting")>]
    [<HttpQuery>]
    member this.InferPrintSetting ([<FromBody>]data: {| File: byte[] |}) =
        async {
            match Parse.voiceFile data.File with
            | Ok file ->
                let printSetting =
                    PDF.getPageSizes file
                    |> PrintSetting.inferFromPDFPageSizes
                    |> PrintSetting.toDto
                return this.Ok({| PrintSetting = printSetting |}) :> IActionResult
            | Error list -> return this.BadRequest(list) :> IActionResult
        }

    [<Route("test-print-setting")>]
    [<HttpPost>]
    member this.TestPrintSetting ([<FromBody>]data: {| File: byte[]; PrintSetting: string |}) =
        async {
            match Parse.printSetting data.PrintSetting with
            | Ok printSetting ->
                do! printer.PrintPdf data.File printSetting 1
                return this.NoContent() :> IActionResult
            | Error list -> return this.BadRequest(list) :> IActionResult
        }

    [<Route("compositions")>]
    [<HttpPost>]
    member this.CreateComposition ([<FromBody>]composition: NewCompositionDto) =
        async {
            match Parse.newCompositionDto composition with
            | Ok newComposition ->
                let! compositionId = db.CreateComposition newComposition
                let result = {
                    Title = newComposition.Title
                    IsActive = newComposition.IsActive
                    Links = {|
                        Self = this.Url.Action(nameof(this.UpdateComposition), {| compositionId = compositionId |})
                        Voices = this.Url.Action(nameof(this.CreateVoice), {| compositionId = compositionId |})
                    |}
                }
                return this.Ok(result) :> IActionResult
            | Error list -> return this.BadRequest(list) :> IActionResult
        }

    [<Route("compositions/export")>]
    member this.ExportCompositions ([<FromQuery>]filterText: string, [<FromQuery>]activeOnly: bool) =
        async {
            let filterText = filterText |> Option.ofObj |> Option.defaultValue ""
            let! compositions = db.GetCompositions()
            let filteredCompositions =
                compositions
                |> List.filter (fun v ->
                    v.Title.Contains(filterText, StringComparison.InvariantCultureIgnoreCase) &&
                        (not activeOnly || v.IsActive)
                )
            let! archivePath =
                filteredCompositions
                |> List.map (fun composition ->
                    ArchiveFolder (composition.Title,
                        async {
                            let! voices = db.GetFullCompositionVoices(composition.Id)
                            return [
                                ArchiveFile (".metadata.toml", Toml.getCompositionMetadata composition voices |> Encoding.UTF8.GetBytes)
                                yield!
                                    voices
                                    |> List.map (fun v -> ArchiveFile ($"{v.Name}.pdf", v.File))
                            ]
                        }
                    )
                )
                |> Zip.createFile
            let archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose)
            return this.File(archiveStream, "application/zip", "MusiScore.zip")
        }

    [<Route("compositions/{compositionId}")>]
    [<HttpPatch>]
    member this.UpdateComposition (compositionId: string, [<FromBody>]composition: CompositionUpdateDto) =
        async {
            match Parse.compositionUpdateDto composition with
            | Ok compositionUpdate ->
                let! updatedComposition = db.UpdateComposition compositionId compositionUpdate
                let result = {
                    Title = updatedComposition.Title
                    IsActive = updatedComposition.IsActive
                    Links = {|
                        Self = this.Url.Action(nameof(this.UpdateComposition))
                        Voices = this.Url.Action(nameof(this.CreateVoice), {| compositionId = compositionId |})
                    |}
                }
                return this.Ok(result) :> IActionResult
            | Error list -> return this.BadRequest(list) :> IActionResult
        }

    [<Route("compositions/{compositionId}")>]
    [<HttpDelete>]
    member _.DeleteComposition (compositionId: string) =
        async {
            do! db.DeleteComposition compositionId
        }

    [<Route("compositions/{compositionId}")>]
    [<HttpGet>]
    member this.GetFullComposition (compositionId: string) =
        async {
            let! composition = db.GetComposition(compositionId)
            let! voices = db.GetFullCompositionVoices(compositionId)
            return
                {
                    Title = composition.Title
                    IsActive = composition.IsActive
                    Links = {|
                        Self = this.Url.Action(nameof(this.UpdateComposition), {| compositionId = compositionId |})
                        Voice = this.Url.Action(nameof(this.CreateVoice), {| compositionId = compositionId |})
                    |}
                    Voices =
                        voices
                        |> Seq.sortBy (fun v -> v.Name)
                        |> Seq.map (fun v -> {
                            Name = v.Name
                            File = v.File
                            PrintSetting = PrintSetting.toDto v.PrintSetting
                            Links = {| Self = this.Url.Action(nameof(this.UpdateVoice), {| compositionId = compositionId; voiceId = v.Id |}) |}
                        })
                        |> Seq.toArray
                }
        }

    [<Route("compositions/{compositionId}/voices")>]
    [<HttpPost>]
    member this.CreateVoice (compositionId: string, [<FromBody>]voice: CreateVoiceDto) =
        async {
            match Parse.createVoiceDto voice with
            | Ok createVoice ->
                let! voiceId = db.CreateVoice compositionId createVoice
                let result = {
                    Name = createVoice.Name
                    File = createVoice.File
                    PrintSetting = PrintSetting.toDto createVoice.PrintSetting
                    Links = {|
                        Self = this.Url.Action(nameof(this.UpdateVoice), {| compositionId = compositionId; voiceId = voiceId |})
                    |}
                }
                return this.Ok(result) :> IActionResult
            | Error list -> return this.BadRequest(list) :> IActionResult
        }

    [<Route("compositions/{compositionId}/voices/{voiceId}")>]
    [<HttpPatch>]
    member this.UpdateVoice (compositionId: string, voiceId: string, [<FromBody>]voice: UpdateVoiceDto) =
        async {
            match Parse.updateVoiceDto voice with
            | Ok updateVoice ->
                let! updatedVoice = db.UpdateVoice compositionId voiceId updateVoice
                let result = {
                    Name = updatedVoice.Name
                    File = updatedVoice.File
                    PrintSetting = PrintSetting.toDto updatedVoice.PrintSetting
                    Links = {| Self = this.Url.Action(nameof(this.UpdateVoice), {| compositionId = compositionId; voiceId = voiceId |}) |}
                }
                return this.Ok(result) :> IActionResult
            | Error list -> return this.BadRequest(list) :> IActionResult
        }

    [<Route("compositions/{compositionId}/voices/{voiceId}")>]
    [<HttpDelete>]
    member _.DeleteVoice (compositionId: string) (voiceId: string) =
        async {
            do! db.DeleteVoice compositionId voiceId
        }
