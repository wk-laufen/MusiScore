module MusiScore.Client.Admin.View

open Bolero
open Bolero.Html
open MusiScore.Client

let view model dispatch =
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
                $"Compositions: %A{loadedCompositions}"
        }
    }
