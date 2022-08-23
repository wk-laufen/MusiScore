[<AutoOpen>]
module MusiScore.Client.Update

open Aether
open Aether.Operators
open Elmish
open MusiScore.Shared.DataTransfer.Print
open Resta.UriTemplates
open System.Net.Http
open System.Net.Http.Json

[<AutoOpen>]
module private Optics =
    module Model =
        let compositions_ : Lens<_, _> =
            (fun x -> x.Compositions),
            (fun v x -> { x with Compositions = v })
    module Deferred =
        let loaded_ : Prism<_, _> =
            let getter = function
                | Loaded v -> Some v
                | Loading
                | LoadFailed _ -> None
            let setter v x =
                Deferred.map (fun _ -> v) x
            (getter, setter)
    module LoadedCompositionsModel =
        let selectedComposition_ : Lens<_, _> =
            (fun x -> x.SelectedComposition),
            (fun v (x: LoadedCompositionsModel) -> { x with SelectedComposition = v })
    module LoadedVoicesModel =
        let selectedVoice_ : Lens<_, _> =
            (fun x -> x.SelectedVoice),
            (fun v x -> { x with SelectedVoice = v })

let update (httpClient: HttpClient) message model =
    let selectedComposition_: Prism<Model,(CompositionDto * Deferred<LoadedVoicesModel,exn>) option> = Model.compositions_ >-> Deferred.loaded_ >?> LoadedCompositionsModel.selectedComposition_
    let selectedCompositionVoices_ = selectedComposition_ >?> Option.value_ >?> snd_
    let selectedVoice_ = selectedCompositionVoices_ >?> Deferred.loaded_ >?> LoadedVoicesModel.selectedVoice_
    let printStatus_ = selectedVoice_ >?> Option.value_ >?> snd_

    match message, model with
    | SetPage page, model ->
        { model with page = page }, Cmd.none
    | LoadActiveCompositions, model ->
        let mapCompositions v =
            false,
            v |> Array.map (fun (v: ActiveCompositionDto) -> { Title = v.Title; IsActive = true; ShowVoicesUrl = v.ShowVoicesUrl })
        model |> Optic.set Model.compositions_ Deferred.Loading,
        Cmd.OfTask.either (fun (url: string) -> httpClient.GetFromJsonAsync<ActiveCompositionDto array>(url)) "/api/print/compositions?activeOnly=true" (mapCompositions >> Ok >> LoadCompositionsResult) (Error >> LoadCompositionsResult)
    | LoadAllCompositions, model ->
        let mapCompositions v = (true, v)
        model |> Optic.set Model.compositions_ Deferred.Loading,
        Cmd.OfTask.either (fun (url: string) -> httpClient.GetFromJsonAsync<CompositionDto array>(url)) "/api/print/compositions" (mapCompositions >> Ok >> LoadCompositionsResult) (Error >> LoadCompositionsResult)
    | LoadCompositionsResult (Ok (isAll, compositions)), model ->
        model |> Optic.set Model.compositions_ (Deferred.Loaded { IsShowingAllCompositions = isAll; Compositions = Array.toList compositions; SelectedComposition = None }),
        Cmd.none
    | LoadCompositionsResult (Error e), model ->
        model |> Optic.set Model.compositions_ (Deferred.LoadFailed e),
        Cmd.none
    | SelectComposition composition, { Compositions = Loaded _ } ->
        model |> Optic.set selectedComposition_ (Some (composition, Deferred.Loading)),
        Cmd.ofMsg LoadVoices
    | SelectComposition _, model -> model, Cmd.none
    | LoadVoices, { Compositions = Loaded ({ SelectedComposition = Some (selectedComposition, _) }) } ->
        model |> Optic.set selectedCompositionVoices_ Deferred.Loading,
        Cmd.OfTask.either (fun (url: string) -> httpClient.GetFromJsonAsync<VoiceDto array>(url)) selectedComposition.ShowVoicesUrl (Ok >> LoadVoicesResult) (Error >> LoadVoicesResult)
    | LoadVoices, model -> model, Cmd.none
    | LoadVoicesResult (Ok voices), { Compositions = Loaded { SelectedComposition = Some _ } } ->
        model |> Optic.set selectedCompositionVoices_ (Deferred.Loaded { Voices = Array.toList voices; SelectedVoice = None }),
        Cmd.none
    | LoadVoicesResult (Ok _), model -> model, Cmd.none
    | LoadVoicesResult (Error e), { Compositions = Loaded { SelectedComposition = Some _ } } ->
        model |> Optic.set selectedCompositionVoices_ (Deferred.LoadFailed e),
        Cmd.none
    | LoadVoicesResult (Error _), model -> model, Cmd.none
    | SelectVoice voice, { Compositions = Loaded { SelectedComposition = Some (_, Loaded _) } } ->
        model |> Optic.set selectedVoice_ (Some (voice, None)),
        Cmd.none
    | SelectVoice _, model -> model, Cmd.none
    | PrintSelectedVoice, { Compositions = Loaded { SelectedComposition = Some (_, Loaded { SelectedVoice = Some (selectedVoice, _) }) } } ->
        let printUrl = UriTemplate(selectedVoice.PrintUrl).GetResolver().Bind("count", "1").Resolve()
        let sendRequest (url: string) = task {
            let! response = httpClient.PostAsync(url, content = null)
            response.EnsureSuccessStatusCode() |> ignore
        }
        model |> Optic.set printStatus_ (Some Deferred.Loading),
        Cmd.OfTask.either sendRequest printUrl (Ok >> PrintSelectedVoiceResult) (Error >> PrintSelectedVoiceResult)
    | PrintSelectedVoice, model -> model, Cmd.none
    | PrintSelectedVoiceResult (Ok ()), { Compositions = Loaded { SelectedComposition = Some (_, Loaded { SelectedVoice = Some _ }) } } ->
        model |> Optic.set selectedComposition_ None,
        Cmd.none
    | PrintSelectedVoiceResult (Ok ()), model -> model, Cmd.none
    | PrintSelectedVoiceResult (Error e), { Compositions = Loaded { SelectedComposition = Some (_, Loaded { SelectedVoice = Some _ }) } } ->
        model |> Optic.set printStatus_ (Some (Deferred.LoadFailed e)),
        Cmd.none
    | PrintSelectedVoiceResult (Error e), model -> model, Cmd.none
