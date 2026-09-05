namespace MusiScore.Server

open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open MusiScore.Shared.DataTransfer.Admin
open System
open System.IO
open System.Net.Mime
open System.Text

[<ApiController>]
[<Route("api/admin")>]
[<Authorize("Notenarchivar")>]
[<RequestSizeLimit(1L * 1024L * 1024L * 1024L)>]
type AdminController(db: Db, printer: Printer, downloadTokens: DownloadTokenStore) =
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
                            Print = this.Url.Action(nameof(this.PrintComposition), {| compositionId = v.Id |})
                        |}
                    })
                    |> Seq.toArray
                Links = {|
                    PrintConfigs = this.Url.Action(nameof(this.GetPrintConfigs))
                    InferPrintConfig = this.Url.Action(nameof(this.InferPrintConfig))
                    TestPrintConfig = this.Url.Action(nameof(this.TestPrintConfig))
                    Composition = this.Url.Action(nameof(this.CreateComposition))
                    CompositionTemplate = this.Url.Action(nameof(this.GetCompositionTemplate))
                    ExportToken = this.Url.Action(nameof(this.CreateExportToken))
                    VoiceDefinitions = this.Url.Action(nameof(this.GetVoiceDefinitions))
                    VoiceDefinitionGroups = this.Url.Action(nameof(this.GetVoiceDefinitionGroups))
                |}
            }
        }

    [<Route("compositions/template")>]
    [<HttpGet>]
    member _.GetCompositionTemplate() =
        async {
            let! tags = db.GetTags()
            return {
                Title = ""
                Tags = tags |> List.map Serialize.Admin.existingTag
                IsActive = false
                Voices = [||]
            }
        }

    [<Route("print-configs")>]
    [<HttpGet>]
    member this.GetPrintConfigs () =
        async {
            let! printConfigs = db.GetPrintConfigsWithStats()
            return
                printConfigs
                |> List.map (fun v -> {
                    Key = v.Key
                    Name = v.Name
                    SortOrder = v.SortOrder
                    CupsCommandLineArgs = v.Settings.CupsCommandLineArgs
                    ReorderPagesAsBooklet = v.Settings.ReorderPagesAsBooklet
                    Compositions = v.Compositions |> List.map (fun v -> { Title = v.Title; Voices = v.Voices })
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
                        Compositions = []
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

    [<Route("print-config/{key}")>]
    [<HttpDelete>]
    member this.DeletePrintConfig (key: string, [<FromBody>]options: PrintConfigDeleteDto) =
        async {
            match! db.DeletePrintConfig key options.ReplacementConfigId with
            | Ok () -> return this.NoContent() :> IActionResult
            | Error e -> return this.BadRequest([ Serialize.Admin.printConfigDeleteError e ])
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
                        Print = this.Url.Action(nameof(this.PrintComposition), {| compositionId = composition.Id |})
                    |}
                }
                return this.Ok(result) :> IActionResult
            | Error list -> return this.BadRequest(list) :> IActionResult
        }

    /// The browser can't send the `Authorization` header when downloading a file by navigating to its URL,
    /// so it gets a short-lived token to put in the export URL instead.
    [<Route("compositions/export-token")>]
    [<HttpPost>]
    member this.CreateExportToken ([<FromQuery>]filterText: string, [<FromQuery>]activeOnly: bool) =
        let exportUrl =
            this.Url.Action(
                nameof(this.ExportCompositions),
                {| filterText = filterText; activeOnly = activeOnly; token = downloadTokens.Create() |}
            )
        this.Ok({| Url = exportUrl |})

    [<Route("compositions/export")>]
    [<HttpGet>]
    [<AllowAnonymous>]
    member this.ExportCompositions ([<FromQuery>]filterText: string, [<FromQuery>]activeOnly: bool, [<FromQuery>]token: string) =
        async {
            if not <| downloadTokens.IsValid token then
                return this.Unauthorized() :> IActionResult
            else
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
                                    return [
                                        Toml.getCompositionMetadata composition composition.Voices
                                        |> Encoding.UTF8.GetBytes
                                        |> ArchiveFile.ofBytes ".metadata.toml"

                                        yield!
                                            composition.Voices
                                            |> List.map (fun v ->
                                                let fileName = v.Names |> String.concat ", "
                                                ArchiveFile ($"%s{fileName}.pdf", db.CopyVoiceFileTo v.Id)
                                            )
                                    ]
                                }
                            )
                        )
                    )
                    |> Zip.createFile
                let archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose)
                return this.File(archiveStream, "application/zip", "Notenarchiv.zip") :> IActionResult
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
                        Print = this.Url.Action(nameof(this.PrintComposition), {| compositionId = compositionId |})
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
            let! voices = db.GetCompositionVoices(compositionId)
            return
                {
                    Title = composition.Title
                    Tags = composition.Tags |> List.map Serialize.Admin.existingTag
                    IsActive = composition.IsActive
                    Links = {|
                        Self = this.Url.Action(nameof(this.UpdateComposition), {| compositionId = compositionId |})
                        Voices = this.Url.Action(nameof(this.CreateVoice), {| compositionId = compositionId |})
                        Print = this.Url.Action(nameof(this.PrintComposition), {| compositionId = compositionId |})
                    |}
                    Voices =
                        voices
                        |> Seq.map (fun voice -> {
                            Names = voice.Names
                            PrintConfig = voice.PrintConfigId
                            Links = {|
                                Self = this.Url.Action(nameof(this.UpdateVoice), {| compositionId = compositionId; voiceId = voice.Id |})
                                Sheet = this.Url.Action(nameof(this.GetVoiceSheet), {| compositionId = compositionId; voiceId = voice.Id |})
                            |}
                        })
                        |> Seq.toArray
                }
        }

    [<Route("compositions/{compositionId}/print")>]
    [<HttpGet>]
    member this.GetCompositionPrintSettings (compositionId: string) =
        async {
            let! voiceDefinitions = db.GetVoiceDefinitions()
            let! voices = db.GetCompositionVoices compositionId
            return
                voices
                |> List.collect _.Names
                |> List.map (fun voice -> {
                    Name = voice
                    Count = voiceDefinitions |> List.find (fun v -> v.Name = voice) |> _.MemberCount
                })
        }

    [<Route("compositions/{compositionId}/print")>]
    [<HttpPost>]
    member this.PrintComposition (compositionId: string, [<FromBody>]settings: VoicePrintSettingsDto list) =
        async {
            let! voices = db.GetFullCompositionVoices compositionId
            let! printConfigs = db.GetPrintConfigs()

            return!
                settings
                |> List.filter (fun v -> v.Count > 0)
                |> List.choose (fun voicePrintSettings ->
                    voices
                    |> List.tryFind (fun v -> v.Names |> List.contains voicePrintSettings.Name)
                    |> Option.bind(fun voice ->
                        printConfigs
                        |> List.tryFind (fun (v: PrintConfig) -> v.Key = voice.PrintConfig)
                        |> Option.bind (fun printConfig ->
                            Some {|
                                Name = voicePrintSettings.Name
                                Count = voicePrintSettings.Count
                                PrintConfig = printConfig
                                File = voice.File
                            |}
                        )
                    )
                )
                |> List.map (fun voice -> async {
                    try
                        do! printer.PrintPdf voice.File voice.PrintConfig.Settings voice.Count
                        return { VoiceName = voice.Name; Result = "Success" }
                    with _ -> return { VoiceName = voice.Name; Result = "PrintingFailed" }
                })
                |> Async.Sequential
        }

    [<Route("compositions/{compositionId}/voices")>]
    [<HttpPost>]
    member this.CreateVoice (compositionId: string, [<FromBody>]voice: CreateVoiceDto) =
        async {
            let! voiceDefinitions = db.GetVoiceDefinitions()
            match Parse.createVoiceDto voice voiceDefinitions with
            | Ok createVoice ->
                let! voiceDefinitions =
                    createVoice.Definitions
                    |> List.map db.GetOrCreateVoiceDefinition
                    |> Async.Sequential
                match! db.CreateVoice compositionId [ for v in voiceDefinitions -> v.Id ] createVoice.File createVoice.PrintConfig with
                | Ok voiceId ->
                    let result = {
                        Names = [ for v in voiceDefinitions -> v.Name ]
                        PrintConfig = createVoice.PrintConfig
                        Links = {|
                            Self = this.Url.Action(nameof(this.UpdateVoice), {| compositionId = compositionId; voiceId = voiceId |})
                            Sheet = this.Url.Action(nameof(this.GetVoiceSheet), {| compositionId = compositionId; voiceId = voiceId |})
                        |}
                    }
                    return this.Ok(result) :> IActionResult
                | Error UnknownPrintConfig -> return this.BadRequest(["InvalidKey"])
            | Error list -> return this.BadRequest(list) :> IActionResult
        }

    [<Route("compositions/{compositionId}/voices/{voiceId}")>]
    [<HttpPatch>]
    member this.UpdateVoice (compositionId: string, voiceId: string, [<FromBody>]voice: UpdateVoiceDto) =
        async {
            let! voiceDefinitions = db.GetVoiceDefinitions()
            match Parse.updateVoiceDto voice voiceDefinitions with
            | Ok updateVoice ->
                let! voiceDefinitionIds = async {
                    match updateVoice.Definitions with
                    | Some definitions ->
                        let! result = definitions |> List.map db.GetOrCreateVoiceDefinition |> Async.Sequential
                        return Some [ for v in result -> v.Id ]
                    | None -> return None
                }
                do! db.UpdateVoice compositionId voiceId voiceDefinitionIds updateVoice.File updateVoice.PrintConfig
                let! updatedVoice = db.GetVoice voiceId
                let result = {
                    Names = updatedVoice.Names
                    PrintConfig = updatedVoice.PrintConfigId
                    Links = {|
                        Self = this.Url.Action(nameof(this.UpdateVoice), {| compositionId = compositionId; voiceId = voiceId |})
                        Sheet = this.Url.Action(nameof(this.GetVoiceSheet), {| compositionId = compositionId; voiceId = voiceId |})
                    |}
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

    [<Route("compositions/{compositionId}/voices/{voiceId}/sheet")>]
    [<HttpGet>]
    member this.GetVoiceSheet (compositionId: string) (voiceId: string) =
        async {
            let! voice = db.GetPrintableVoice (compositionId, voiceId)
            return this.File(voice.File, MediaTypeNames.Application.Pdf)
        }

    [<Route("voice-definitions")>]
    [<HttpGet>]
    member this.GetVoiceDefinitions () =
        async {
            let! voiceDefinitions = db.GetVoiceDefinitions()
            return voiceDefinitions |> List.map (fun (v: VoiceDefinition) ->
                let url = this.Url.Action(nameof(this.UpdateVoiceDefinition), {| voiceDefinitionId = v.Id |})
                Serialize.Admin.voiceDefinition url v
            )
        }

    [<Route("voice-definitions")>]
    [<HttpPost>]
    member this.CreateVoiceDefinition ([<FromBody>]voiceDefinition: CreateVoiceDefinitionDto) =
        async {
            match Parse.createVoiceDefinition voiceDefinition with
            | Ok newVoiceDefinition ->
                match! db.CreateVoiceDefinition newVoiceDefinition with
                | Ok (voiceDefinition: VoiceDefinition) ->
                    let url = this.Url.Action(nameof(this.UpdateVoiceDefinition), {| voiceDefinitionId = voiceDefinition.Id |})
                    return this.Ok(Serialize.Admin.voiceDefinition url voiceDefinition) :> IActionResult
                | Error error -> return this.BadRequest([ Serialize.Admin.saveVoiceError error ])
            | Error errors -> return this.BadRequest(errors)
        }

    [<Route("voice-definitions/{voiceDefinitionId}")>]
    [<HttpDelete>]
    member this.DeleteVoiceDefinition (voiceDefinitionId: string, [<FromBody>]options: VoiceDefinitionDeleteDto) =
        async {
            match! db.DeleteVoiceDefinition voiceDefinitionId options.ReplacementVoiceDefinitionId with
            | Ok () -> return this.NoContent() :> IActionResult
            | Error e -> return this.BadRequest([ Serialize.Admin.voiceDefinitionDeleteError e ])
        }

    [<Route("voice-definitions/{voiceDefinitionId}")>]
    [<HttpPatch>]
    member this.UpdateVoiceDefinition (voiceDefinitionId: string, [<FromBody>]voiceDefinition: UpdateVoiceDefinitionDto) =
        async {
            match Parse.updateVoiceDefinition voiceDefinition with
            | Ok voiceDefinitionUpdate ->
                match! db.UpdateVoiceDefinition voiceDefinitionId voiceDefinitionUpdate with
                | Ok (updatedVoiceDefinition: VoiceDefinition) ->
                    let url = this.Url.Action(nameof(this.UpdateVoiceDefinition), {| voiceDefinitionId = updatedVoiceDefinition.Id |})
                    return this.Ok(Serialize.Admin.voiceDefinition url updatedVoiceDefinition) :> IActionResult
                | Error e -> return this.BadRequest([ Serialize.Admin.saveVoiceError e ])
            | Error errors -> return this.BadRequest(errors)
        }

    /// Returns all voice definitions grouped by their voice definition group, ready for display.
    [<Route("voice-definition-groups")>]
    [<HttpGet>]
    member this.GetVoiceDefinitionGroups () =
        async {
            let! voiceDefinitionGroups = db.GetGroupedVoiceDefinitions()
            let serializeVoiceDefinition (v: VoiceDefinitionWithStats) =
                let url = this.Url.Action(nameof(this.UpdateVoiceDefinition), {| voiceDefinitionId = v.Id |})
                Serialize.Admin.voiceDefinitionWithStats url v
            return
                voiceDefinitionGroups
                |> List.map (fun v ->
                    match v.Group with
                    | Some group ->
                        let url = this.Url.Action(nameof(this.UpdateVoiceDefinitionGroup), {| voiceDefinitionGroupId = group.Id |})
                        {|
                            Type = "UserGroup"
                            Id = group.Id
                            Name = group.Name
                            VoiceDefinitions = v.VoiceDefinitions |> List.map serializeVoiceDefinition
                            Links = {| Self = url |}
                        |} :> obj
                    | None ->
                        {|
                            Type = "NoGroup"
                            VoiceDefinitions = v.VoiceDefinitions |> List.map serializeVoiceDefinition
                        |}
                )
        }

    [<Route("voice-definition-groups")>]
    [<HttpPost>]
    member this.CreateVoiceDefinitionGroup ([<FromBody>]group: CreateVoiceDefinitionGroupDto) =
        async {
            match Parse.createVoiceDefinitionGroup group with
            | Ok newGroup ->
                match! db.CreateVoiceDefinitionGroup newGroup with
                | Ok (group: VoiceDefinitionGroup) ->
                    let url = this.Url.Action(nameof(this.UpdateVoiceDefinitionGroup), {| voiceDefinitionGroupId = group.Id |})
                    return this.Ok(Serialize.Admin.voiceDefinitionGroup url group) :> IActionResult
                | Error error -> return this.BadRequest([ Serialize.Admin.saveVoiceDefinitionGroupError error ])
            | Error errors -> return this.BadRequest(errors)
        }

    [<Route("voice-definition-groups/{voiceDefinitionGroupId}")>]
    [<HttpPatch>]
    member this.UpdateVoiceDefinitionGroup (voiceDefinitionGroupId: string, [<FromBody>]group: UpdateVoiceDefinitionGroupDto) =
        async {
            match Parse.updateVoiceDefinitionGroup group with
            | Ok groupUpdate ->
                match! db.UpdateVoiceDefinitionGroup voiceDefinitionGroupId groupUpdate with
                | Ok (updatedGroup: VoiceDefinitionGroup) ->
                    let url = this.Url.Action(nameof(this.UpdateVoiceDefinitionGroup), {| voiceDefinitionGroupId = updatedGroup.Id |})
                    return this.Ok(Serialize.Admin.voiceDefinitionGroup url updatedGroup) :> IActionResult
                | Error e -> return this.BadRequest([ Serialize.Admin.saveVoiceDefinitionGroupError e ])
            | Error errors -> return this.BadRequest(errors)
        }

    [<Route("voice-definition-groups/{voiceDefinitionGroupId}")>]
    [<HttpDelete>]
    member _.DeleteVoiceDefinitionGroup (voiceDefinitionGroupId: string) =
        async {
            do! db.DeleteVoiceDefinitionGroup voiceDefinitionGroupId
        }
