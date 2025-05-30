namespace MusiScore.Server

open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open MusiScore.Shared.DataTransfer.Admin
open System
open System.IO
open System.Text

[<ApiController>]
[<Route("api/admin")>]
[<Authorize("Notenarchivar")>]
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
                        Tags = v.Tags |> List.sortBy (fun v -> v.Title) |> List.map Serialize.Admin.existingTag
                        IsActive = v.IsActive
                        Links = {|
                            Self = this.Url.Action(nameof(this.UpdateComposition), {| compositionId = v.Id |})
                            Voices = this.Url.Action(nameof(this.CreateVoice), {| compositionId = v.Id |})
                        |}
                    })
                    |> Seq.toArray
                Links = {|
                    PrintConfigs = this.Url.Action(nameof(this.GetPrintConfigs))
                    InferPrintConfig = this.Url.Action(nameof(this.InferPrintConfig))
                    TestPrintConfig = this.Url.Action(nameof(this.TestPrintConfig))
                    Composition = this.Url.Action(nameof(this.CreateComposition))
                    Export = this.Url.Action(nameof(this.ExportCompositions))
                |}
            }
        }

    [<Route("print-configs")>]
    [<HttpGet>]
    member this.GetPrintConfigs () =
        async {
            let! printConfigs = db.GetPrintConfigs()
            return
                printConfigs
                |> List.map (fun v -> {
                    Key = v.Key
                    Name = v.Name
                    SortOrder = v.SortOrder
                    CupsCommandLineArgs = v.Settings.CupsCommandLineArgs
                    ReorderPagesAsBooklet = v.Settings.ReorderPagesAsBooklet
                    Links = {|
                        Self = this.Url.Action(nameof(this.UpdatePrintConfig), {| key = v.Key |})
                    |}
                })
        }

    [<Route("print-configs")>]
    [<HttpPost>]
    member this.CreatePrintConfig ([<FromBody>]printConfig: NewPrintConfigDto) =
        async {
            match Parse.printConfig printConfig with
            | Ok newPrintConfig ->
                match! db.CreatePrintConfig(newPrintConfig) with
                | Ok() ->
                    return this.Ok({
                        Key = newPrintConfig.Key
                        Name = newPrintConfig.Name
                        SortOrder = newPrintConfig.SortOrder
                        CupsCommandLineArgs = newPrintConfig.Settings.CupsCommandLineArgs
                        ReorderPagesAsBooklet = newPrintConfig.Settings.ReorderPagesAsBooklet
                        Links = {|
                            Self = this.Url.Action(nameof(this.UpdatePrintConfig), {| key = newPrintConfig.Key |})
                        |}
                    }) :> IActionResult
                | Error PrintConfigExists -> return this.BadRequest(["PrintConfigExists"])
            | Error list -> return this.BadRequest(list) :> IActionResult
        }

    [<Route("print-config")>]
    [<HttpQuery>]
    member this.InferPrintConfig ([<FromBody>]data: {| File: byte[] |}) =
        async {
            match Parse.voiceFile data.File with
            | Ok _file ->
                match! db.GetDefaultPrintConfig() with
                | Some printConfig ->
                    return this.Ok({| PrintConfig = printConfig.Key |}) :> IActionResult
                | None -> return this.StatusCode(StatusCodes.Status500InternalServerError, {| Message = "No default print config found" |})
            | Error list -> return this.BadRequest(list) :> IActionResult
        }

    [<Route("print-config/{key}")>]
    [<HttpPatch>]
    member this.UpdatePrintConfig (key: string, [<FromBody>]printConfig: PrintConfigUpdateDto) =
        async {
            match Parse.printConfigUpdateDto printConfig with
            | Ok printConfigUpdate ->
                do! db.UpdatePrintConfig key printConfigUpdate
                return this.NoContent() :> IActionResult
            | Error list -> return this.BadRequest(list) :> IActionResult
        }

    [<Route("test-print-config")>]
    [<HttpPost>]
    member this.TestPrintConfig ([<FromBody>]data: {| File: byte[]; PrintConfig: string |}) =
        async {
            match Parse.printConfigKey data.PrintConfig with
            | Ok printConfigKey ->
                match! db.GetPrintConfig printConfigKey with
                | Some printConfig ->
                    do! printer.PrintPdf data.File printConfig.Settings 1
                    return this.NoContent() :> IActionResult
                | None -> return this.BadRequest() :> IActionResult
            | Error list -> return this.BadRequest(list) :> IActionResult
        }

    [<Route("compositions")>]
    [<HttpPost>]
    member this.CreateComposition ([<FromBody>]composition: NewCompositionDto) =
        async {
            match Parse.newCompositionDto composition with
            | Ok newComposition ->
                let! composition = db.CreateComposition newComposition
                let result = {
                    Title = composition.Title
                    Tags = composition.Tags |> List.map Serialize.Admin.existingTag
                    IsActive = composition.IsActive
                    Links = {|
                        Self = this.Url.Action(nameof(this.UpdateComposition), {| compositionId = composition.Id |})
                        Voices = this.Url.Action(nameof(this.CreateVoice), {| compositionId = composition.Id |})
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
                |> List.groupBy _.Title // TODO add composer and/or arranger?
                |> List.collect (fun (folderName, compositions) ->
                    compositions
                    |> List.mapi (fun index composition ->
                        let folderName = if index = 0 then folderName else $"%s{folderName} (%d{index})"
                        ArchiveFolder (folderName,
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
                    Tags = updatedComposition.Tags |> List.map Serialize.Admin.existingTag
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
                    Tags = composition.Tags |> List.map Serialize.Admin.existingTag
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
                            PrintConfig = v.PrintConfig
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
                    PrintConfig = createVoice.PrintConfig
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
                    PrintConfig = updatedVoice.PrintConfig
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
