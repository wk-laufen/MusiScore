namespace MusiScore.Client

open Bolero
open Elmish
open MusiScore.Shared.DataTransfer.Print

type Page =
    | [<EndPoint "/">] ListActiveCompositions
    | [<EndPoint "/admin">] Admin

type LoadedVoicesModel = {
    Voices: VoiceDto list
    SelectedVoice: (VoiceDto * Deferred<unit, exn> option) option
}

type LoadedCompositionsModel = {
    IsShowingAllCompositions: bool
    Compositions: CompositionDto list
    SelectedComposition: (CompositionDto * Deferred<LoadedVoicesModel, exn>) option
}

type Model = {
    page: Page
    Compositions: Deferred<LoadedCompositionsModel, exn>
}

type Message =
    | SetPage of Page
    | LoadActiveCompositions
    | LoadAllCompositions
    | LoadCompositionsResult of Result<bool * CompositionDto array, exn>
    | SelectComposition of CompositionDto
    | LoadVoices
    | LoadVoicesResult of Result<VoiceDto array, exn>
    | SelectVoice of VoiceDto
    | PrintSelectedVoice
    | PrintSelectedVoiceResult of Result<unit, exn>

module Model =
    let init =
        {
            page = ListActiveCompositions
            Compositions = Loading
        },
        Cmd.ofMsg LoadActiveCompositions
