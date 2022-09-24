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

let divider (text: string) =
    div {
        attr.``class`` "flex p-4 items-center"
        div { attr.``class`` "grow border-t mt-px border-gray-400" }
        span {
            attr.``class`` "shrink mx-4 text-gray-400 text-sm"
            text
        }
        div { attr.``class`` "grow border-t mt-px border-gray-400" }
    }

let infoNotification (text: string) =
    div {
        attr.``class`` "flex items-center justify-center gap-2"
        i { attr.``class`` "fa-solid fa-circle-info fa-lg text-blue-700" }
        span {
            attr.``class`` "text-lg text-blue-700"
            text
        }
    }


let errorNotification (text: string) =
    div {
        attr.``class`` "flex items-center justify-center gap-2"
        i { attr.``class`` "fa-solid fa-triangle-exclamation fa-lg text-red-700" }
        span {
            attr.``class`` "text-lg text-red-700"
            text
        }
    }

let errorNotificationWithRetry text onRetry =
    div {
        attr.``class`` "flex flex-col items-center justify-center gap-2 m-4"
        errorNotification text
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
