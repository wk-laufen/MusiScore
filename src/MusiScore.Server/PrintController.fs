namespace MusiScore.Server

open Microsoft.AspNetCore.Mvc
open MusiScore.Shared.DataTransfer.Print
open Resta.UriTemplates
open System

[<ApiController>]
[<Route("api/print")>]
type PrintController(db: Db) =
    inherit ControllerBase()

    [<Route("compositions")>]
    [<HttpGet>]
    member this.GetCompositions ([<FromQuery>]activeOnly: Nullable<bool>) =
        async {
            if activeOnly.GetValueOrDefault(false) then
                let! compositions = db.GetActiveCompositions()
                return
                    compositions
                    |> Seq.map (fun v -> {
                        Title = v.Title
                        ShowVoicesUrl = this.Url.Action(nameof(this.GetVoices), {| compositionId = v.Id |})
                    })
                    |> Seq.cast<obj>
                    |> Seq.toArray
            else
                let! compositions = db.GetCompositions()
                return
                    compositions
                    |> Seq.map (fun v -> {
                        Title = v.Title
                        IsActive = v.IsActive
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
    member _.PrintVoice (compositionId: string, voiceId: string, [<FromQuery>]count: System.Nullable<int>) =
        async {
            let count = Option.ofNullable count |> Option.defaultValue 1
            let! voice = db.GetPrintableVoice(compositionId, voiceId)
            let content =
                match voice.PrintSetting with
                | Duplex -> voice.File
                | A4ToA3Duplex -> voice.File
                | A4ToBooklet -> PDF.reorderAsBooklet voice.File
            do! Printer.printPdf content voice.PrintSetting count
        }
