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
            let! voiceSortOrderPatterns = db.GetVoiceSortOrderPatterns()
            return
                compositions
                |> Seq.sortBy (fun v -> v.Title)
                |> Seq.map (fun composition ->
                    let allVoices =
                        composition.Voices
                        |> List.map (fun voice ->
                            (voice, Voice.tryGetSortOrder voiceSortOrderPatterns voice.Name)
                        )
                        |> List.sortBy snd
                    let voicesWithSortOrder =
                        allVoices |> List.choose (fun (voice, sortOrder) -> match sortOrder with | Some v -> Some (voice, v) | None -> None)
                    {
                        Title = composition.Title
                        Tags = composition.Tags |> List.map Serialize.Print.existingTag
                        Voices = [
                            for (v, sortOrder) in voicesWithSortOrder ->
                                let printUrl = this.Url.Action(nameof(this.PrintVoice), {| compositionId = composition.Id; voiceId = v.Id |})
                                {| Name = v.Name; GlobalSortOrder = sortOrder; PrintUrl = printUrl |}
                        ]
                    }
                )
        }


    [<Route("compositions/{compositionId}/voices/{voiceId}")>]
    [<HttpPost>]
    member _.PrintVoice (compositionId: string, voiceId: string, [<FromQuery>]count: Nullable<int>) =
        async {
            let count = Option.ofNullable count |> Option.defaultValue 1
            let! voice = db.GetPrintableVoice(compositionId, voiceId)
            do! printer.PrintPdf voice.File voice.PrintSettings count
        }
