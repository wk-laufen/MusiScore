module MusiScore.Client.Admin.View

open Bolero.Html
open MusiScore.Client
open MusiScore.Shared.DataTransfer.Admin

let compositionView (composition: ExistingCompositionDto) compositionDeleteState dispatch =
    div {
        attr.``class`` "flex items-stretch border rounded font-semibold text-blue-700 border-blue-500 divide-x divide-blue-500"
        span {
            attr.``class`` "grow flex items-center justify-center text-center !p-8 w-60"
            composition.Title
        }
        button {
            attr.``class`` "p-4"
            attr.title (if composition.IsActive then "Markierung entfernen" else "Als aktuelles Stück markieren")
            on.click (fun _ -> dispatch (ToggleActivateComposition composition))
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
            cond compositionDeleteState <| function
                | Some (compositionToDelete, None) when compositionToDelete = composition ->
                    button {
                        attr.``class`` "p-4 grow bg-blue-500"
                        attr.title "Wirklich löschen"
                        on.click (fun _ -> dispatch (DeleteComposition composition))
                        i { attr.``class`` "fa-solid fa-trash-can text-white" }
                    }
                | Some (compositionToDelete, Some Deferred.Loading) when compositionToDelete = composition ->
                    div {
                        attr.``class`` "px-2 py-3 grow"
                        div {
                            attr.``class`` "spinner spinner-blue"
                        }
                    }
                | Some (compositionToDelete, Some (Deferred.LoadFailed _)) when compositionToDelete = composition ->
                    button {
                        attr.``class`` "p-4 grow bg-blue-500"
                        attr.title "Löschen erneut versuchen"
                        on.click (fun _ -> dispatch (DeleteComposition composition))
                        i { attr.``class`` "text-red-500 fa-solid fa-trash-can" }
                    }
                | Some (compositionToDelete, Some (Deferred.Loaded ())) when compositionToDelete = composition ->
                    empty ()
                | Some (_, _)
                | None ->
                    button {
                        attr.``class`` "p-4 grow"
                        attr.title "Löschen"
                        on.click (fun _ -> dispatch (DeleteComposition composition))
                        i { attr.``class`` "fa-solid fa-trash-can" }
                    }
        }
    }

let compositionListView (loadedCompositions: ListCompositionsModel) dispatch =
    concat {
        div {
            attr.``class`` "flex items-center gap-2 m-4"
            div {
                input {
                    attr.``class`` "input-text"
                    attr.``type`` "search"
                    attr.placeholder "Filter"
                    bind.input.string loadedCompositions.CompositionFilter.Text (ChangeCompositionFilterText >> dispatch)
                }
            }
            div {
                label {
                    input {
                        attr.``class`` "appearance-none h-4 w-4 border border-gray-300 rounded-sm bg-white checked:bg-blue-600 checked:border-blue-600 focus:outline-none transition duration-200 mt-1 align-top bg-no-repeat bg-center bg-contain float-left mr-2 cursor-pointer"
                        attr.``type`` "checkbox"
                        bind.``checked`` loadedCompositions.CompositionFilter.ActiveOnly (ShowActiveCompositionsOnly >> dispatch)
                    }
                    "Nur aktuelle Stücke anzeigen"
                }
            }
        }
        let filteredCompositions =
            loadedCompositions.Compositions
            |> List.filter (fun v ->
                v.Title.Contains(loadedCompositions.CompositionFilter.Text, System.StringComparison.InvariantCultureIgnoreCase) &&
                    (not loadedCompositions.CompositionFilter.ActiveOnly || v.IsActive)
            )
        div {
            attr.``class`` "flex flex-col items-stretch m-4"
            cond (List.isEmpty filteredCompositions) <| function
            | true ->
                ViewComponents.infoNotification "Keine Stücke vorhanden."
            | false -> concat {
                let compositionsByFirstChar =
                    filteredCompositions
                    |> List.groupBy (fun v -> Seq.tryHead v.Title)
                for (firstChar, compositions) in compositionsByFirstChar do
                    ViewComponents.divider (firstChar |> Option.map string |> Option.defaultValue "<leer>")
                    div {
                        attr.``class`` "flex flex-wrap items-stretch gap-2 m-4"
                        for composition in compositions do
                            compositionView composition loadedCompositions.CompositionDeleteState dispatch
                    }
            }
        }
    }

let voicesTabs (editVoices: EditVoicesModel) dispatch =
    ul {
        attr.``class`` "nav-container"

        for voice in editVoices.Voices do
            li {
                a {
                    let classes =
                        [
                            "nav-item"
                            "!pr-2"
                            if editVoices.SelectedVoice = Some voice.Id then "active"
                        ]
                        |> String.concat " "
                    attr.``class`` classes
                    on.click (fun _ -> dispatch (SelectEditCompositionVoice voice.Id))
                    span {
                        let classes =
                            [
                                match voice.State with
                                | LoadedVoice (false, _) -> ()
                                | CreatedVoice -> "text-green-500"
                                | ModifiedVoice (false, _) -> "text-yellow-500"
                                | LoadedVoice (true, _)
                                | ModifiedVoice (true, _) -> "text-red-500 line-through"
                            ]
                            |> String.concat " "
                        attr.``class`` classes
                        if voice.Name.Text = "" then "<leer>" else voice.Name.Text
                    }
                    button {
                        attr.``class`` "p-2"
                        attr.title "Löschen"
                        on.click (fun _ -> dispatch (DeleteEditCompositionVoice voice.Id))
                        on.stopPropagation "onclick" true
                        i { attr.``class`` "fa-solid fa-trash-can" }
                    }
                }
            }
        
        li {
            a {
                attr.``class`` "nav-item !py-5"
                on.click (fun _ -> dispatch AddEditCompositionVoice)
                "+ Neue Stimme"
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

        ViewComponents.Form.Input.text "Titel" (SetTitle >> SetEditCompositionFormInput >> dispatch) model.Title

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
                            voicesTabs editVoices dispatch
                            cond (EditVoicesModel.tryGetSelectedVoice editVoices) <| function
                            | Some voice ->
                                div {
                                    ViewComponents.Form.Input.text "Name" (SetVoiceName >> SetEditCompositionFormInput >> dispatch) voice.Name
                                    ViewComponents.Form.Input.file "PDF-Datei" (fun e -> dispatch (SetEditCompositionFormInput (SetVoiceFile e.File))) voice.File
                                    cond (snd model.VoicePrintSettings) <| function
                                        | None
                                        | Some Deferred.Loading -> empty()
                                        | Some (Deferred.Loaded printSettings) ->
                                            let printSettingOptions =
                                                printSettings
                                                |> List.map (fun v -> (v.Key, v.Name))
                                            ViewComponents.Form.Input.select "Druckeinstellung" (SetPrintSetting >> SetEditCompositionFormInput >> dispatch) voice.PrintSetting printSettingOptions
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
    concat {
        cond model <| function
            | Model.EditComposition ({ SaveState = Some saveState }) when saveState.CountSaving > 0 ->
                empty ()
            | Model.EditComposition ({ SaveState = Some saveState }) when saveState.CountSaveError > 0 ->
                div {
                    attr.``class`` "basis-auto grow-0 shrink-0 flex justify-end m-4 mb-0"
                    span {
                        attr.``class`` "text-sm text-red-500"
                        "Fehler beim Speichern."
                    }
                }
            | Model.EditComposition ({ SaveState = Some saveState }) when saveState.CountSaved > 0 ->
                div {
                    attr.``class`` "basis-auto grow-0 shrink-0 flex justify-end m-4 mb-0"
                    span {
                        attr.``class`` "text-sm text-green-500"
                        "Stück erfolgreich gespeichert."
                    }
                }
            | _ -> empty ()
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
                                "btn btn-solid btn-gold !px-8 !py-4"
                                match model with
                                | Model.EditComposition ({ SaveState = Some saveState }) when saveState.CountSaving > 0 -> "btn-loading"
                                | _ -> ()
                            ]
                            |> String.concat " "
                        attr.``class`` classes
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
                compositionListView loadedCompositions dispatch
            | Model.EditComposition subModel ->
                editCompositionView subModel dispatch
        }
        commandBar model dispatch
    }
