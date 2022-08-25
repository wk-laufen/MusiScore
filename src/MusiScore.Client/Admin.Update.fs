module MusiScore.Client.Admin.Update

open Aether
open Elmish
open MusiScore.Shared.DataTransfer.Admin
open System.Net.Http
open System.Net.Http.Json

[<AutoOpen>]
module private Optics =
    module Model =
        let compositions_ : Lens<Model, _> =
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

let update (httpClient: HttpClient) message model =
    match message, model with
    | LoadCompositions, model ->
        model |> Optic.set Model.compositions_ Deferred.Loading,
        Cmd.OfTask.either (fun (url: string) -> httpClient.GetFromJsonAsync<CompositionListDto>(url)) "/api/admin/compositions" (Ok >> LoadCompositionsResult) (Error >> LoadCompositionsResult)
    | LoadCompositionsResult (Ok compositionList), model ->
        model |> Optic.set Model.compositions_ (Deferred.Loaded compositionList),
        Cmd.none
    | LoadCompositionsResult (Error e), model ->
        model |> Optic.set Model.compositions_ (Deferred.LoadFailed e),
        Cmd.none

