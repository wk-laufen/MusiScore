namespace MusiScore.Client.Print

open Elmish
open MusiScore.Shared.DataTransfer.Print

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
    Compositions: Deferred<LoadedCompositionsModel, exn>
}

type Message =
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
            Compositions = Loading
        },
        Cmd.ofMsg LoadActiveCompositions
