namespace MusiScore.Server

open Microsoft.AspNetCore.Mvc
open MusiScore.Shared.DataTransfer.Print
open Resta.UriTemplates
open System

[<ApiController>]
[<Route("api/print")>]
type PrintController(db: Db, printer: Printer) =
    inherit ControllerBase()

    [<Route("compositions")>]
    [<HttpGet>]
    member this.GetActiveCompositions () =
        async {
            let! compositions = db.GetActiveCompositions()
            return
                compositions
                |> Seq.sortBy (fun v -> v.Title)
                |> Seq.map (fun v -> {
                    Title = v.Title
                    ShowVoicesUrl = this.Url.Action(nameof(this.GetVoices), {| compositionId = v.Id |})
                })
                |> Seq.cast<obj>
                |> Seq.toArray
        }

    [<Route("compositions/{compositionId}/voices")>]
    [<HttpGet>]
    member this.GetVoices (compositionId: string) =
        async {
            let! voices = db.GetCompositionVoices(compositionId)
            return
                voices
                |> Seq.sortBy (fun v -> v.Name)
                |> Seq.map (fun v -> {
                    Name = v.Name
                    PrintUrl =
                        let baseUrl = this.Url.Action(nameof(this.PrintVoice), {| compositionId = compositionId; voiceId = v.Id |})
                        UriTemplate($"{baseUrl}{{?count}}").Template
                })
        }

    [<Route("compositions/{compositionId}/voices/{voiceId}")>]
    [<HttpPost>]
    member _.PrintVoice (compositionId: string, voiceId: string, [<FromQuery>]count: Nullable<int>) =
        async {
            let count = Option.ofNullable count |> Option.defaultValue 1
            let! voice = db.GetPrintableVoice(compositionId, voiceId)
            let content =
                match voice.PrintSetting with
                | Duplex -> voice.File
                | A4ToA3Duplex -> voice.File
                | A4ToBooklet -> PDF.reorderAsBooklet voice.File
            do! printer.PrintPdf content voice.PrintSetting count
        }
