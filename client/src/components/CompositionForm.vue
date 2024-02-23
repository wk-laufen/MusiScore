<script setup lang="ts">
import { ref, watch, toRef } from 'vue'
import uiFetch from './UIFetch'
import type { CompositionListItem, FullComposition, PrintSetting, SaveCompositionServerError, SaveVoiceServerErrors, Voice } from './AdminTypes'
import type { ValidationState } from './Validation'
import LoadingBar from './LoadingBar.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import TextInput from './TextInput.vue'
import FileInput from './FileInput.vue'
import SelectInput from './SelectInput.vue'
import PdfPreview from './PdfPreview.vue'

const deserializeFile = (text?: string) => {
  if (text === undefined) return undefined

  return Uint8Array.from(atob(text), m => m.codePointAt(0) as number)
}

const serializeFile = (content?: ArrayBuffer) => {
  if (content === undefined) return undefined

  return btoa(String.fromCodePoint(...new Uint8Array(content)))
}

const props = defineProps<{
  type: 'create' | 'edit'
  printSettingsUrl: string
  compositionUrl: string
}>()

const printSettings = ref<PrintSetting[]>()
const isLoadingPrintSettings = ref(false)
const hasLoadingPrintSettingsFailed = ref(false)
const loadPrintSettings = async () => {
  const result = await uiFetch(isLoadingPrintSettings, hasLoadingPrintSettingsFailed, props.printSettingsUrl)
  if (result.succeeded) {
    printSettings.value = (await result.response.json() as PrintSetting[])
  }
}
loadPrintSettings()

type EditableVoiceState =
    | { type: 'loadedVoice', isMarkedForDeletion: boolean, links: { self: string } }
    | { type: 'newVoice' }
    | { type: 'modifiedVoice', isMarkedForDeletion: boolean, links: { self: string } }
type EditableVoice = Omit<Voice, 'links' | 'file'> & {
  state: EditableVoiceState
  nameValidationState: ValidationState
  file?: Uint8Array
  fileValidationState: ValidationState
  printSettingValidationState: ValidationState
  isSaving: boolean
  hasSavingFailed: boolean
}
type EditableComposition = Omit<FullComposition, 'voices'> & { voices: EditableVoice[] }

const parseLoadedVoice = (voice: Voice) : EditableVoice => {
  return {
    state: { type: 'loadedVoice', isMarkedForDeletion: false, links: voice.links },
    name: voice.name,
    nameValidationState: { type: 'success' },
    file: deserializeFile(voice.file),
    fileValidationState: { type: 'success' },
    printSetting: voice.printSetting,
    printSettingValidationState: { type: 'success' },
    isSaving: false,
    hasSavingFailed: false
  }
}

const parseLoadedComposition = (loadedComposition: FullComposition) : EditableComposition => {
  const { voices, ...props } = loadedComposition
  return {
    ...props,
    voices: voices.map(parseLoadedVoice)
  }
}

const composition = ref<EditableComposition>()
const activeVoice = ref<EditableVoice>()
const isLoading = ref(false)
const hasLoadingFailed = ref(false)
const loadComposition = async () => {
  switch (props.type) {
    case 'create':
      composition.value = {
        title: "",
        isActive: true,
        links: {
          self: props.compositionUrl,
          voices: undefined
        },
        voices: []
      }
      break
    case 'edit': {
      const result = await uiFetch(isLoading, hasLoadingFailed, props.compositionUrl)
      if (result.succeeded) {
        const loadedComposition = (await result.response.json() as FullComposition)
        composition.value = parseLoadedComposition(loadedComposition)
        activeVoice.value = composition.value.voices.length > 0 ? composition.value.voices[0] : undefined
      }
      break
    }
  }
}
loadComposition()

const titleValidationState = ref<ValidationState>({ type: "notValidated" })

const activeVoiceFile = ref<File>()
watch(activeVoiceFile, async v =>
{
  if (activeVoice.value === undefined) return
  if (v === undefined) {
    activeVoice.value = undefined
    return
  }
  if (!activeVoice.value.name) {
    activeVoice.value.name = v.name.substring(0, v.name.lastIndexOf('.'))
  }
  activeVoice.value.file = new Uint8Array(await v.arrayBuffer())
})

const addVoice = () => {
  if (composition.value === undefined) return

  composition.value.voices.push({
    state: { type: 'newVoice' },
    name: '',
    nameValidationState: { type: 'notValidated' },
    file: undefined,
    fileValidationState: { type: 'notValidated' },
    printSetting: '',
    printSettingValidationState: { type: 'notValidated' },
    isSaving: false,
    hasSavingFailed: false
  })
  activeVoice.value = composition.value.voices[composition.value.voices.length - 1]
}

const deleteVoice = (voice: EditableVoice) => {
  if (composition.value === undefined) return

  if (voice.state.type === 'newVoice') {
    const voiceIndex = composition.value.voices.indexOf(voice)
    if (activeVoice.value === voice) {
      if (voiceIndex + 1 < composition.value.voices.length) {
        activeVoice.value = composition.value.voices[voiceIndex + 1]
      }
      else if (voiceIndex - 1 >= 0) {
        activeVoice.value = composition.value.voices[voiceIndex - 1]
      }
      else {
        activeVoice.value = undefined
      }
    }
    composition.value.voices = composition.value.voices.filter(v => v !== voice)
  }
  else {
    voice.state.isMarkedForDeletion = !voice.state.isMarkedForDeletion
  }
}

const isSavingComposition = ref(false)
const hasSavingCompositionFailed = ref(false)
const saveVoice = async (voice: EditableVoice, newVoiceUrl: string) => {
  // TODO get existing voice url from voice
  const result = await uiFetch(toRef(voice.isSaving), toRef(voice.hasSavingFailed), newVoiceUrl, {
    method: props.type === 'create' ? 'POST' : 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      name: voice.name,
      file: serializeFile(voice.file),
      printSetting: voice.printSetting
    })
  })
  if (result.succeeded) {
    const voice = await result.response.json() as Voice
    return parseLoadedVoice(voice)
  }
  else if (result.response !== undefined) {
    const errors = await result.response.json() as SaveVoiceServerErrors
    for (const error of errors) {
      if (error.errorCode === 'EmptyName') {
        voice.nameValidationState = { type: 'error', error: 'Bitte geben Sie den Namen der Stimme ein.' }
      }
      if (error.errorCode === 'EmptyFile') {
        voice.fileValidationState = { type: 'error', error: 'Bitte wählen Sie eine PDF-Datei aus.' }
      }
      else if (error.errorCode === 'InvalidFile') {
        voice.fileValidationState = { type: 'error', error: 'Die PDF-Datei kann nicht gelesen werden.' }
      }
      if (error.errorCode === 'UnknownPrintSetting') {
        voice.fileValidationState = { type: 'error', error: 'Die PDF-Datei kann nicht gelesen werden.' }
      }
    }
    return voice
  }
  // TODO else?
}

const saveVoices = async (newVoiceUrl: string) => {
  if (composition.value === undefined) return

  return await Promise.all(composition.value.voices.map(voice => saveVoice(voice, newVoiceUrl)))
}

const saveComposition = async () => {
  if (composition.value === undefined) return

  titleValidationState.value = { type: 'notValidated' }
  
  const result = await uiFetch(isSavingComposition, hasSavingCompositionFailed, props.compositionUrl, {
    method: props.type === 'create' ? 'POST' : 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title: composition.value.title })
  })
  if (result.succeeded) {
    const compositionListItem = await result.response.json() as CompositionListItem
    saveVoices(compositionListItem.links.voices)
  }
  else if (result.response !== undefined) {
    const error = await result.response.json() as SaveCompositionServerError
    if (error.errorCode === 'EmptyTitle') {
      titleValidationState.value = { type: 'error', error: 'Bitte geben Sie den Titel des Stücks ein.' }
    }
  }
  // TODO else?
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
      <TextInput title="Titel" :validation-state="titleValidationState" v-model="composition.title" />
      <h3 class="text-xl small-caps mt-4">Stimmen</h3>
      <ul class="nav-container">
        <li v-for="voice in composition.voices" :key="JSON.stringify(voice)">
          <a @click="activeVoice = voice" class="nav-item !pr-2" :class="{ active: activeVoice === voice }">
            <span :class="{
              'text-green-500': voice.state.type === 'newVoice',
              'text-yellow-500': voice.state.type === 'modifiedVoice' && !voice.state.isMarkedForDeletion,
              'text-musi-red line-through': (voice.state.type === 'loadedVoice' || voice.state.type === 'modifiedVoice') && voice.state.isMarkedForDeletion }">
              {{ voice.name || '<leer>' }}
            </span>
            <button class="p-2" title="Löschen" @click.stop="deleteVoice(voice)">
              <font-awesome-icon class="mr-2" :icon="['fas', 'trash']" />
            </button>
          </a>
        </li>
        <li>
          <a class="nav-item !py-5" @click="addVoice()">+ Neue Stimme</a>
        </li>
      </ul>
      <div v-if="activeVoice !== undefined">
        <TextInput title="Name" :validation-state="activeVoice.nameValidationState" v-model="activeVoice.name" />
        <FileInput title="PDF-Datei" :validation-state="activeVoice.fileValidationState" v-model="activeVoiceFile" />
        <div class="flex gap-2">
          <SelectInput v-if="printSettings !== undefined" title="Druckeinstellung" :options="printSettings.map(v => ({ key: v.key, value: v.name}))" :validation-state="activeVoice.printSettingValidationState" v-model="activeVoice.printSetting" />
          <ErrorWithRetry v-else-if="hasLoadingPrintSettingsFailed" type="inline" @retry="loadPrintSettings" class="self-end">Fehler beim Laden der Druckeinstellungen.</ErrorWithRetry>
        </div>
        <PdfPreview :file="activeVoice.file" class="mt-6" />
      </div>
    </template>
  </div>

  <Teleport to="#command-bar">
    <button class="btn btn-solid btn-gold !px-8 !py-4" classes="{ 'btn-loading': isSavingComposition }" @click="saveComposition">Speichern</button>
  </Teleport>
</template>
