namespace MusiScore.Server

open Microsoft.AspNetCore.Mvc
open MusiScore.Shared.DataTransfer.Admin
open System
open System.IO
open System.Text

[<ApiController>]
[<Route("api/admin")>]
type AdminController(db: Db) =
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
                        UpdateUrl = this.Url.Action(nameof(this.UpdateComposition), {| compositionId = v.Id |})
                        DeleteUrl = this.Url.Action(nameof(this.DeleteComposition), {| compositionId = v.Id |})
                        GetVoicesUrl = this.Url.Action(nameof(this.GetVoices), {| compositionId = v.Id |})
                        CreateVoiceUrl = this.Url.Action(nameof(this.CreateVoice), {| compositionId = v.Id |})
                    })
                    |> Seq.toArray
                GetPrintSettingsUrl = this.Url.Action(nameof(this.GetPrintSettings))
                CreateCompositionUrl = this.Url.Action(nameof(this.CreateComposition))
                ExportCompositionsUrl = this.Url.Action(nameof(this.ExportCompositions))
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

    [<Route("compositions")>]
    [<HttpPost>]
    member this.CreateComposition ([<FromBody>]composition: NewCompositionDto) =
        async {
            match NewComposition.tryParseDto composition false with
            | Ok newComposition ->
                let! compositionId = db.CreateComposition newComposition
                let result = {
                    Title = newComposition.Title
                    IsActive = newComposition.IsActive
                    UpdateUrl = this.Url.Action(nameof(this.UpdateComposition), {| compositionId = compositionId |})
                    DeleteUrl = this.Url.Action(nameof(this.DeleteComposition), {| compositionId = compositionId |})
                    GetVoicesUrl = this.Url.Action(nameof(this.GetVoices), {| compositionId = compositionId |})
                    CreateVoiceUrl = this.Url.Action(nameof(this.CreateVoice), {| compositionId = compositionId |})
                }
                return this.Ok(result) :> IActionResult
            | Error message -> return this.BadRequest(message) :> IActionResult
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
            match CompositionUpdate.tryParseDto composition with
            | Ok compositionUpdate ->
                let! updatedComposition = db.UpdateComposition compositionId compositionUpdate
                let result = {
                    Title = updatedComposition.Title
                    IsActive = updatedComposition.IsActive
                    UpdateUrl = this.Url.Action(nameof(this.UpdateComposition), {| compositionId = compositionId |})
                    DeleteUrl = this.Url.Action(nameof(this.DeleteComposition), {| compositionId = compositionId |})
                    GetVoicesUrl = this.Url.Action(nameof(this.GetVoices), {| compositionId = compositionId |})
                    CreateVoiceUrl = this.Url.Action(nameof(this.CreateVoice), {| compositionId = compositionId |})
                }
                return this.Ok(result) :> IActionResult
            | Error message -> return this.BadRequest(message) :> IActionResult
        }

    [<Route("compositions/{compositionId}")>]
    [<HttpDelete>]
    member _.DeleteComposition (compositionId: string) =
        async {
            do! db.DeleteComposition compositionId
        }

    [<Route("compositions/{compositionId}/voices")>]
    [<HttpGet>]
    member this.GetVoices (compositionId: string) =
        async {
            let! voices = db.GetFullCompositionVoices(compositionId)
            return
                voices
                |> Seq.map (fun v -> {
                    Name = v.Name
                    File = v.File
                    PrintSetting = PrintSetting.toDto v.PrintSetting
                    UpdateUrl = this.Url.Action(nameof(this.UpdateVoice), {| compositionId = compositionId; voiceId = v.Id |})
                    DeleteUrl = this.Url.Action(nameof(this.DeleteVoice), {| compositionId = compositionId; voiceId = v.Id |})
                })
                |> Seq.toArray
        }

    [<Route("compositions/{compositionId}/voices")>]
    [<HttpPost>]
    member this.CreateVoice (compositionId: string, [<FromBody>]voice: CreateVoiceDto) =
        async {
            match CreateVoice.tryParseDto voice with
            | Ok createVoice ->
                let! voiceId = db.CreateVoice compositionId createVoice
                let result = {
                    Name = createVoice.Name
                    File = createVoice.File
                    PrintSetting = PrintSetting.toDto createVoice.PrintSetting
                    UpdateUrl = this.Url.Action(nameof(this.UpdateVoice), {| compositionId = compositionId; voiceId = voiceId |})
                    DeleteUrl = this.Url.Action(nameof(this.DeleteVoice), {| compositionId = compositionId; voiceId = voiceId |})
                }
                return this.Ok(result) :> IActionResult
            | Error message -> return this.BadRequest(message) :> IActionResult
        }

    [<Route("compositions/{compositionId}/voices/{voiceId}")>]
    [<HttpPatch>]
    member this.UpdateVoice (compositionId: string, voiceId: string, [<FromBody>]voice: UpdateVoiceDto) =
        async {
            match UpdateVoice.tryParseDto voice with
            | Ok updateVoice ->
                let! updatedVoice = db.UpdateVoice compositionId voiceId updateVoice
                let result = {
                    Name = updatedVoice.Name
                    File = updatedVoice.File
                    PrintSetting = PrintSetting.toDto updatedVoice.PrintSetting
                    UpdateUrl = this.Url.Action(nameof(this.UpdateVoice), {| compositionId = compositionId; voiceId = voiceId |})
                    DeleteUrl = this.Url.Action(nameof(this.DeleteVoice), {| compositionId = compositionId; voiceId = voiceId |})
                }
                return this.Ok(result) :> IActionResult
            | Error message -> return this.BadRequest(message) :> IActionResult
        }

    [<Route("compositions/{compositionId}/voices/{voiceId}")>]
    [<HttpDelete>]
    member _.DeleteVoice (compositionId: string) (voiceId: string) =
        async {
            do! db.DeleteVoice compositionId voiceId
        }
