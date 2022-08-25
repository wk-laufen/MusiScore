namespace MusiScore.Client.Admin

open Elmish
open MusiScore.Shared.DataTransfer.Admin

type Model = {
    Compositions: Deferred<CompositionListDto, exn>
}

type Message =
    | LoadCompositions
    | LoadCompositionsResult of Result<CompositionListDto, exn>

module Model =
    let init =
        {
            Compositions = Deferred.Loading
        },
        Cmd.ofMsg LoadCompositions
