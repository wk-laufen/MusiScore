module MusiScore.Client.Print.View

open Bolero.Html
open MusiScore.Client
open MusiScore.Shared.DataTransfer.Print

let compositionListView (loadedCompositions: LoadedCompositionsModel) loadingComposition dispatch =
    div {
        attr.``class`` "flex flex-wrap items-stretch justify-center gap-2 m-4"
        for composition in loadedCompositions.Compositions do
            cond (Some composition = loadingComposition) <| function
                | true ->
                    button {
                        attr.``class`` "relative btn btn-blue !p-8 w-60 opacity-50 after:inline-block after:absolute after:left-1/2 after:top-1/2 after:w-4 after:h-4 after:-ml-2 after:-mt-2 after:border-b-2 after:border-blue-500 after:rounded-full after:animate-spin"
                        composition.Title
                    }
                | false ->
                    button {
                        attr.``class`` "relative btn btn-blue !p-8 w-60"
                        on.click (fun _ -> dispatch (SelectComposition composition))
                        composition.Title
                        cond (loadedCompositions.IsShowingAllCompositions && composition.IsActive) <| function
                            | true ->
                                concat {
                                    i { attr.``class`` "absolute left-4 top-4 fa-solid fa-star" }
                                    i { attr.``class`` "absolute right-4 top-4 fa-solid fa-star" }
                                    i { attr.``class`` "absolute left-4 bottom-4 fa-solid fa-star" }
                                    i { attr.``class`` "absolute right-4 bottom-4 fa-solid fa-star" }
                                }
                            | false -> empty ()
                    }
    }

let voiceListView (composition: CompositionDto) loadedVoices dispatch =
    div {
        attr.``class`` "p-4"
        h1 {
            attr.``class`` "text-2xl small-caps"
            composition.Title
        }
        div {
            attr.``class`` "flex flex-wrap items-stretch justify-center gap-2 m-4"
            for voice in loadedVoices.Voices do
            cond loadedVoices.SelectedVoice <| function
                | Some (selectedVoice, Some (Deferred.LoadFailed _)) when selectedVoice = voice ->
                    button {
                        attr.``class`` "btn btn-blue w-60"
                        on.click (fun _ -> dispatch (SelectVoice voice))
                        div {
                            attr.``class`` "flex flex-col items-center justify-center"
                            span { voice.Name }
                            span { attr.``class`` "text-red-500"; "Drucken fehlgeschlagen" }
                        }
                    }
                | Some (selectedVoice, Some Deferred.Loading) when selectedVoice = voice ->
                    button {
                        attr.``class`` "relative btn btn-blue !p-8 w-60 opacity-50 after:inline-block after:absolute after:left-1/2 after:top-1/2 after:w-4 after:h-4 after:-ml-2 after:-mt-2 after:border-b-2 after:border-blue-500 after:rounded-full after:animate-spin"
                        voice.Name
                    }
                | Some (selectedVoice, None) when selectedVoice = voice ->
                    button {
                        attr.``class`` "btn btn-solid btn-blue w-60"
                        on.click (fun _ -> dispatch PrintSelectedVoice)
                        div {
                            attr.``class`` "flex flex-col items-center justify-center"
                            span { voice.Name }
                            span { "Drucken" }
                        }
                    }
                | _ ->
                    button {
                        attr.``class`` "btn btn-blue !p-8 w-60"
                        on.click (fun _ -> dispatch (SelectVoice voice))
                        voice.Name
                    }
        }
    }

let loadCompositionsButton isShowingAllCompositions dispatch =
    let (command, text) =
        if isShowingAllCompositions then (LoadActiveCompositions, "Nur aktuelle Stücke anzeigen")
        else (LoadAllCompositions, "Alle Stücke anzeigen")
    button {
        attr.``class`` "btn btn-solid btn-gold !px-8 !py-4"
        on.click (fun _ -> dispatch command)
        text
    }

let cancelButton dispatch =
    button {
        attr.``class`` "btn btn-solid btn-gold !px-8 !py-4"
        on.click (fun _ -> dispatch LoadActiveCompositions)
        "Abbrechen"
    }

let commandBar model dispatch =
    div {
        attr.``class`` "basis-auto grow-0 shrink-0 flex flex-row-reverse m-4"

        match model.Compositions with
        | Deferred.Loading -> empty ()
        | Deferred.LoadFailed e -> empty ()
        | Deferred.Loaded loadedCompositions ->
            match loadedCompositions.SelectedComposition with
            | None ->
                loadCompositionsButton loadedCompositions.IsShowingAllCompositions dispatch
            | Some (_, Deferred.Loading) -> empty ()
            | Some (_, Deferred.LoadFailed _e) ->
                cancelButton dispatch
            | Some (_, Deferred.Loaded _) ->
                cancelButton dispatch
    }

let view model dispatch =
    concat {
        h1 {
            attr.``class`` "text-3xl p-8 bg-gold text-white small-caps"
            i {
                attr.``class`` "fa-solid fa-music mr-2"
            }
            "MusiScore"
        }
        div {
            attr.``class`` "grow overflow-y-auto"

            match model.Compositions with
            | Deferred.Loading -> ViewComponents.loading
            | Deferred.LoadFailed e ->
                ViewComponents.errorNotificationWithRetry "Fehler beim Laden." (fun () -> dispatch LoadActiveCompositions)
            | Deferred.Loaded loadedCompositions ->
                match loadedCompositions.SelectedComposition with
                | None -> compositionListView loadedCompositions None dispatch
                | Some (composition, Deferred.Loading) ->
                    compositionListView loadedCompositions (Some composition) dispatch
                | Some (_composition, Deferred.LoadFailed _e) ->
                    ViewComponents.errorNotificationWithRetry "Fehler beim Laden." (fun () -> dispatch LoadVoices)
                | Some (composition, Deferred.Loaded voices) ->
                    voiceListView composition voices dispatch
        }
        commandBar model dispatch
    }
