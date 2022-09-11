namespace MusiScore.Client.Print

open Elmish
open MusiScore.Shared.DataTransfer.Print

type LoadedVoicesModel = {
    Voices: VoiceDto list
    SelectedVoice: (VoiceDto * Deferred<unit, exn> option) option
}

type LoadedCompositionsModel = {
    Compositions: ActiveCompositionDto list
    SelectedComposition: (ActiveCompositionDto * Deferred<LoadedVoicesModel, exn>) option
}

type Model = {
    Compositions: Deferred<LoadedCompositionsModel, exn>
}

type Message =
    | LoadCompositions
    | LoadCompositionsResult of Result<ActiveCompositionDto array, exn>
    | SelectComposition of ActiveCompositionDto
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
        Cmd.ofMsg LoadCompositions
