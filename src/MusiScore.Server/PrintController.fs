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
            let! events = db.GetEvents()
            let! voiceDefinitionGroups = db.GetGroupedVoiceDefinitions()
            let serializeComposition (composition: ActiveComposition) =
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
            let compositions = compositions |> List.sortWith (fun a b -> Collation.compare a.Title b.Title)
            return {|
                Voices = voiceDefinitionGroups |> List.map Serialize.Print.voiceDefinitionGroup
                // sorted by name, oldest first - a single group without name if there are no events at all
                Events =
                    if List.isEmpty events then
                        [ {| Name = None; Compositions = compositions |> List.map serializeComposition |} ]
                    else
                        events
                        |> List.filter _.IsActive
                        |> List.sortWith (fun a b -> Collation.compare a.Name b.Name)
                        |> List.map (fun event ->
                            {|
                                Name = Some event.Name
                                Compositions =
                                    compositions
                                    |> List.filter (fun v -> List.contains event.Id v.EventIds)
                                    |> List.map serializeComposition
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
