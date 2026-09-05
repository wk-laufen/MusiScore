namespace MusiScore.Server

open Microsoft.AspNetCore.Mvc
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
            let! voiceDefinitionGroups = db.GetGroupedVoiceDefinitions()
            return {|
                Voices = voiceDefinitionGroups |> List.map Serialize.Print.voiceDefinitionGroup
                Compositions = 
                    compositions
                    |> Seq.sortBy (fun v -> v.Title)
                    |> Seq.map (fun composition ->
                        {|
                            Title = composition.Title
                            Tags = composition.Tags |> List.map Serialize.Print.existingTag
                            Voices =
                                composition.Voices
                                |> List.collect (fun v ->
                                    let printUrl = this.Url.Action(nameof(this.PrintVoice), {| compositionId = composition.Id; voiceId = v.Id |})
                                    [ for name in v.Names -> {| Name = name; PrintUrl = printUrl |} ]
                                )
                        |}
                    )
            |}
        }

    [<Route("compositions/{compositionId}/voices/{voiceId}")>]
    [<HttpPost>]
    member _.PrintVoice (compositionId: string, voiceId: string, [<FromQuery>]count: Nullable<int>) =
        async {
            let count = Option.ofNullable count |> Option.defaultValue 1
            let! voice = db.GetPrintableVoice(compositionId, voiceId)
            do! printer.PrintPdf voice.File voice.PrintSettings count
        }
