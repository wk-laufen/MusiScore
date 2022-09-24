module MusiScore.Client.Admin.View

open Bolero.Html
open Microsoft.AspNetCore.Components.Forms
open MusiScore.Client
open MusiScore.Shared.DataTransfer.Admin

let compositionListView (loadedCompositions: CompositionListDto) loadingComposition dispatch =
    div {
        attr.``class`` "flex flex-col items-stretch m-4"
        cond (Array.isEmpty loadedCompositions.Compositions) <| function
        | true ->
            ViewComponents.infoNotification "Keine Stücke vorhanden."
        | false -> concat {
            let compositionsByFirstChar =
                loadedCompositions.Compositions
                |> Array.groupBy (fun v -> Seq.tryHead v.Title)
            for (firstChar, compositions) in compositionsByFirstChar do
                ViewComponents.divider (firstChar |> Option.map string |> Option.defaultValue "<leer>")
                div {
                    attr.``class`` "flex flex-wrap items-stretch gap-2 m-4"
                    for composition in compositions do
                        div {
                            attr.``class`` "flex items-stretch border rounded font-semibold text-blue-700 border-blue-500 divide-x divide-blue-500"
                            span {
                                attr.``class`` "grow flex items-center justify-center text-center !p-8 w-60"
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
                                attr.``class`` "flex flex-col justify-items-stretch divide-y divide-blue-500"
                                button {
                                    attr.``class`` "p-4 grow"
                                    attr.title "Bearbeiten"
                                    on.click (fun _ -> dispatch (EditComposition composition))
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
    }

let formInputText (title: string) onChange formInput =
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
                on.input (fun v -> onChange (v.Value :?> string))
                attr.value formInput.Text
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

let formInputFile (title: string) onChange value =
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

let formInputSelect (title: string) onChange value values =
    div {
        label {
            attr.``class`` "input"

            span {
                attr.``class`` "input-label"
                title
            }
            select {
                attr.``class`` "px-3 py-1.5 border border-gray-300 rounded transition ease-in-out focus:border-blue-600"
                on.change (fun v -> onChange (v.Value :?> string))

                for (key, text: string) in values do
                    option {
                        attr.value key
                        attr.selected (value = key)
                        text
                    }
            }
        }
    }


let editCompositionView model dispatch =
    div {
        attr.``class`` "p-4"
        h2 {
            attr.``class`` "text-2xl small-caps"
            if model.State = CreatedComposition then "Stück anlegen" else "Stück bearbeiten"
        }

        formInputText "Titel" (SetTitle >> SetEditCompositionFormInput >> dispatch) model.Title

        cond model.Voices <| function
            | None -> empty ()
            | Some editVoices ->
                concat {
                    h3 {
                        attr.``class`` "text-xl small-caps mt-4"
                        "Stimmen"
                    }
                    cond editVoices <| function
                    | Deferred.Loading ->
                        div {
                            attr.``class`` "mt-4"
                            "Stimmen werden geladen..."
                        }
                    | Deferred.Loaded editVoices ->
                        concat {
                            ul {
                                attr.``class`` "nav-container"

                                for voice in editVoices.Voices do
                                    li {
                                        a {
                                            let classes =
                                                [
                                                    if editVoices.SelectedVoice = Some voice.Id then "active"
                                                    match voice.State with
                                                    | LoadedVoice _ -> ""
                                                    | CreatedVoice -> "text-green-500"
                                                    | ModifiedVoice _ -> "text-yellow-500"
                                                    | DeletedVoice _ -> "text-red-500"
                                                ]
                                                |> List.map (sprintf " %s")
                                                |> String.concat ""
                                            attr.``class`` (sprintf "nav-item%s" classes)
                                            on.click (fun _ -> dispatch (SelectEditCompositionVoice voice.Id))
                                            if voice.Name.Text = "" then "<leer>" else voice.Name.Text
                                        }
                                    }
                                
                                li {
                                    a {
                                        attr.``class`` "nav-item"
                                        on.click (fun _ -> dispatch AddEditCompositionVoice)
                                        "+ Neue Stimme"
                                    }
                                }
                            }
                            cond (EditVoicesModel.tryGetSelectedVoice editVoices) <| function
                            | Some voice ->
                                div {
                                    formInputText "Name" (SetVoiceName >> SetEditCompositionFormInput >> dispatch) voice.Name
                                    formInputFile "PDF-Datei" (fun e -> dispatch (SetEditCompositionFormInput (SetVoiceFile e.File))) voice.File
                                    cond (snd model.VoicePrintSettings) <| function
                                        | None
                                        | Some Deferred.Loading -> empty()
                                        | Some (Deferred.Loaded printSettings) ->
                                            let printSettingOptions =
                                                printSettings
                                                |> List.map (fun v -> (v.Key, v.Name))
                                            formInputSelect "Druckeinstellung" (SetPrintSetting >> SetEditCompositionFormInput >> dispatch) voice.PrintSetting printSettingOptions
                                        | Some (Deferred.LoadFailed e) ->
                                            ViewComponents.errorNotificationWithRetry "Fehler beim Laden der Druckeinstellungen" (fun () -> dispatch LoadEditCompositionVoicePrintSettings)
                                    let hasLoadPdfModuleError =
                                        match model.PdfModule with
                                        | Some (Error _) -> true
                                        | _ -> false
                                    let hasRenderError =
                                        match editVoices.RenderPreviewError with
                                        | Some _ -> true
                                        | _ -> false
                                    cond (hasLoadPdfModuleError || hasRenderError) <| function
                                        | true -> ViewComponents.errorNotification "Fehler beim Laden der PDF-Anzeige"
                                        | false -> empty ()
                                    div {
                                        attr.``class`` "voice-preview flex flex-wrap gap-4 p-4"
                                    }
                                }
                            | None -> empty()
                        }
                    | Deferred.LoadFailed _ ->
                        ViewComponents.errorNotificationWithRetry "Fehler beim Laden" (fun () -> dispatch LoadEditCompositionVoices)
                }
    }

let commandBar model dispatch =
    let totalSaveState =
        match model with
        | Model.EditComposition ({ Voices = Some (Deferred.Loaded editVoicesModel) } as editCompositionModel) ->
            let saveStates =
                [
                    editCompositionModel.SaveState
                    yield! editVoicesModel.Voices |> List.map (fun v -> v.SaveState)
                ]
            let countSaving = saveStates |> List.sumBy (function | Some Deferred.Loading -> 1 | _ -> 0)
            let countSaved = saveStates |> List.sumBy (function | Some (Deferred.Loaded ()) -> 1 | _ -> 0)
            let countSaveError = saveStates |> List.sumBy (function | Some (Deferred.LoadFailed _) -> 1 | _ -> 0)
            if countSaving > 0 then Some Deferred.Loading
            elif countSaveError > 0 then Some (Deferred.LoadFailed (exn "Dummy"))
            elif countSaved > 0 then Some (Deferred.Loaded ())
            else None
        | _ -> None
    concat {
        cond totalSaveState <| function
            | None
            | Some Deferred.Loading -> empty()
            | Some (Deferred.LoadFailed _) ->
                div {
                    attr.``class`` "basis-auto grow-0 shrink-0 flex justify-end m-4 mb-0"
                    span {
                        attr.``class`` "text-sm text-red-500"
                        "Fehler beim Speichern."
                    }
                }
            | Some (Deferred.Loaded ()) ->
                div {
                    attr.``class`` "basis-auto grow-0 shrink-0 flex justify-end m-4 mb-0"
                    span {
                        attr.``class`` "text-sm text-green-500"
                        "Stück erfolgreich gespeichert."
                    }
                }
        div {
            attr.``class`` "basis-auto grow-0 shrink-0 flex justify-end m-4 gap-4"
            
            cond model <| function
            | ListCompositions _ ->
                button {
                    attr.``class`` "btn btn-solid btn-gold !px-8 !py-4"
                    on.click (fun _ -> dispatch CreateComposition)
                    "Neues Stück hinzufügen"
                }
            | Model.EditComposition subModel ->
                concat {
                    button {
                        attr.``class`` "btn btn-solid btn-gold !px-8 !py-4"
                        on.click (fun _ -> dispatch LoadCompositions)
                        "Zurück"
                    }
                    button {
                        let classes =
                            [
                                match totalSaveState with
                                | None
                                | Some (Deferred.Loaded ())
                                | Some (Deferred.LoadFailed _) -> ()
                                | Some Deferred.Loading -> "btn-loading"
                            ]
                            |> List.map (sprintf " %s")
                            |> String.concat ""
                        attr.``class`` (sprintf "btn btn-solid btn-gold !px-8 !py-4%s" classes)
                        on.click (fun _ -> dispatch SaveComposition)
                        "Speichern"
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

            cond model <| function
            | ListCompositions Deferred.Loading -> ViewComponents.loading
            | ListCompositions (Deferred.LoadFailed e) ->
                ViewComponents.errorNotificationWithRetry "Fehler beim Laden." (fun () -> dispatch LoadCompositions)
            | ListCompositions (Deferred.Loaded loadedCompositions) ->
                compositionListView loadedCompositions None dispatch
            | Model.EditComposition subModel ->
                editCompositionView subModel dispatch
        }
        commandBar model dispatch
    }
