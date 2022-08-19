namespace MusiScore.Server

open Microsoft.AspNetCore.Mvc
open MusiScore.Shared
open Resta.UriTemplates

[<ApiController>]
[<Route("api/compositions")>]
type CompositionController(db: Db) =
    inherit ControllerBase()
    
    member private _.Url with get() = base.Url

    [<Route("")>]
    [<HttpGet>]
    member this.GetActiveCompositions () =
        async {
            let! activeCompositions = db.GetActiveCompositions()
            return
                activeCompositions
                |> Seq.map (fun v -> {
                    Title = v.Title
                    ShowVoicesLink = this.Url.Action(nameof(this.GetVoices), {| compositionId = v.Id |})
                })
                |> Seq.toArray
        }

    [<Route("{compositionId}/voices")>]
    [<HttpGet>]
    member this.GetVoices (compositionId: string) =
        async {
            let! voices = db.GetCompositionVoices(compositionId)
            return
                voices
                |> Seq.sortBy (fun v -> v.Name)
                |> Seq.map (fun v -> {
                    Name = v.Name
                    PrintLink =
                        let baseUrl = this.Url.Action(nameof(this.PrintVoice), {| compositionId = compositionId; voiceId = v.Id |})
                        UriTemplate($"{baseUrl}{{?count}}").Template
                })
        }

    [<Route("{compositionId}/voices/{voiceId}")>]
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
