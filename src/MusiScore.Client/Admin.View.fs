module MusiScore.Client.Admin.View

open Bolero.Html
open MusiScore.Client
open MusiScore.Shared.DataTransfer.Admin

let compositionListView (loadedCompositions: CompositionListDto) loadingComposition dispatch =
    div {
        attr.``class`` "flex flex-col items-stretch m-4"
        let compositionsByFirstChar =
            loadedCompositions.Compositions
            |> Seq.groupBy (fun v -> Seq.tryHead v.Title)
        for (firstChar, compositions) in compositionsByFirstChar do
            ViewComponents.divider (firstChar |> Option.map string |> Option.defaultValue "<leer>")
            div {
                attr.``class`` "flex flex-wrap items-stretch gap-2 m-4"
                for composition in compositions do
                    div {
                        attr.``class`` "flex items-stretch border rounded font-semibold text-blue-700 border-blue-500 divide-x"
                        span {
                            attr.``class`` "grow flex items-center justify-center !p-8 w-60"
                            composition.Title
                        }
                        button {
                            attr.``class`` "p-4"
                            attr.title (if composition.IsActive then "Markierung entfernen" else "Als aktuelles Stück markieren")
                            i {
                                attr.``class`` (sprintf "%s fa-star" (if composition.IsActive then "fa-solid" else "fa-regular"))
                            }
                        }
                        div {
                            attr.``class`` "flex flex-col justify-items-stretch divide-y"
                            button {
                                attr.``class`` "p-4 grow"
                                attr.title "Bearbeiten"
                                i { attr.``class`` "fa-solid fa-pencil" }
                            }
                            button {
                                attr.``class`` "p-4 grow"
                                attr.title "Löschen"
                                i { attr.``class`` "fa-solid fa-trash-can" }
                            }
                        }
                    }
            }
    }

let view (model: Model) dispatch =
    concat {
        h1 {
            attr.``class`` "text-3xl p-8 bg-gold text-white small-caps"
            i {
                attr.``class`` "fa-solid fa-music mr-2"
            }
            "MusiScore - Administration"
        }
        div {
            attr.``class`` "grow overflow-y-auto"

            match model.Compositions with
            | Deferred.Loading -> ViewComponents.loading
            | Deferred.LoadFailed e ->
                ViewComponents.errorNotificationWithRetry "Fehler beim Laden." (fun () -> dispatch LoadCompositions)
            | Deferred.Loaded loadedCompositions ->
                compositionListView loadedCompositions None dispatch
        }
    }
