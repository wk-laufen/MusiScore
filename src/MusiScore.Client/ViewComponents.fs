module MusiScore.Client.ViewComponents

open Bolero.Html

let loading =
    div {
        attr.``class`` "flex flex-col items-center justify-center m-4"
        div {
            attr.``class`` "w-8 h-8 border-b-2 border-gold rounded-full animate-spin mb-4"
        }
        span { "Laden. Bitte warten..." }
    }

let errorNotificationWithRetry (text: string) onRetry =
    div {
        attr.``class`` "flex flex-col items-center justify-center gap-2 m-4"
        div {
            attr.``class`` "flex items-center justify-center gap-2"
            i { attr.``class`` "fa-solid fa-triangle-exclamation fa-lg text-red-700" }
            span {
                attr.``class`` "text-lg text-red-700"
                text
            }
        }
        button {
            attr.``class`` "btn btn-blue"
            on.click (fun _ -> onRetry())
            div {
                attr.``class`` "flex items-center gap-2"
                i { attr.``class`` "fa-solid fa-arrows-rotate" }
                span { "Erneut versuchen" }
            }
        }
    }
