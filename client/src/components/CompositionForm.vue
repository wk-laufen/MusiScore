<script setup lang="ts">
import { ref } from 'vue'
import uiFetch from './UIFetch'
import type { CompositionListItem, FullComposition } from './AdminTypes'
import LoadingBar from './LoadingBar.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'

const emit = defineEmits<{
  (e: 'compositionSaved', oldComposition: CompositionListItem | undefined, newComposition: CompositionListItem) : void
}>()

const props = defineProps<{
  type: 'create' | 'edit'
  compositionUrl: string
}>()

const composition = ref<FullComposition>()

const isLoading = ref(false)
const hasLoadingFailed = ref(false)
const loadComposition = async () => {
  const result = await uiFetch(isLoading, hasLoadingFailed, props.compositionUrl)
  if (result.succeeded) {
    composition.value = (await result.response.json() as FullComposition)
  }
}
loadComposition()

const isSavingComposition = ref(false)
const hasSavingCompositionFailed = ref(false)
const saveComposition = async () => {
  if (!compositionList.value || !editComposition.value) return

  const result = await uiFetch(isSavingComposition, hasSavingCompositionFailed, props.compositionUrl, {
    method: props.type === 'create' ? 'POST' : 'PUT',
    body: JSON.stringify(composition)
  })
  if (result.succeeded) {
    emit('compositionSaved', oldComposition, newComposition)
  }
}
</script>

<template>
  <div class="p-4">
    <h2 class="text-2xl small-caps">
        {{ type === 'create' ? "Stück anlegen" : "Stück bearbeiten" }}
    </h2>
    
    <LoadingBar v-if="isLoading" />
    <ErrorWithRetry v-if="hasLoadingFailed" @retry="loadComposition">Fehler beim Laden.</ErrorWithRetry>
    <template v-else-if="composition !== undefined">
      <TextInput title="Titel" v-model="composition.title" />
        <!-- ViewComponents.Form.Input.text "Titel" (SetTitle >> SetEditCompositionFormInput >> dispatch) model.Title

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
                } -->
        </template>
  </div>

  <Teleport to="#command-bar">
    <button class="btn btn-solid btn-gold !px-8 !py-4" classes="{ 'btn-loading': isSavingComposition }" @click="saveComposition">Speichern</button>
  </Teleport>
</template>
