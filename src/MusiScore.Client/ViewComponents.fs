module MusiScore.Client.ViewComponents

open Bolero.Html
open Microsoft.AspNetCore.Components.Forms

let loading =
    div {
        attr.``class`` "flex flex-col items-center justify-center m-4"
        div {
            attr.``class`` "spinner spinner-gold mb-4"
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

module Form =
    module Input =
        let text (title: string) onChange formInput =
            div {
                label {
                    attr.``class`` "input"

                    span {
                        attr.``class`` "input-label"
                        title
                    }
                    input {
                        attr.``class`` "input-text"
                        attr.``type`` "text"
                        attr.required true
                        bind.input.string formInput.Text onChange
                    }
                    cond formInput.ValidationState <| function
                        | ValidationError msg ->
                            span {
                                attr.``class`` "text-sm text-pink-600"
                                msg
                            }
                        | NotValidated
                        | ValidationSuccess _ -> empty ()
                }
            }

        let file (title: string) onChange value =
            div {
                label {
                    attr.``class`` "input"

                    span {
                        attr.``class`` "input-label"
                        title
                    }
                    comp<InputFile> {
                        attr.``class`` "hidden"
                        attr.accept ".pdf" 
                        attr.callback "OnChange" (fun (e: InputFileChangeEventArgs) -> onChange e)
                    }
                    div {
                        attr.``class`` "flex gap-4 items-center"

                        div {
                            attr.``class`` "btn btn-blue"
                            "Auswählen"
                        }

                        cond value <| function
                            | None
                            | Some (Ok _) -> empty ()
                            | Some (Error _) ->
                                span {
                                    attr.``class`` "text-red-500"
                                    "Fehler beim Laden der Datei"
                                }
                    }
                }
            }

        let select (title: string) onChange value values =
            div {
                label {
                    attr.``class`` "input"

                    span {
                        attr.``class`` "input-label"
                        title
                    }
                    select {
                        attr.``class`` "px-3 py-1.5 border border-gray-300 rounded transition ease-in-out focus:border-blue-600"
                        bind.input.string value onChange

                        for (key: string, text: string) in values do
                            option {
                                attr.value key
                                text
                            }
                    }
                }
            }
