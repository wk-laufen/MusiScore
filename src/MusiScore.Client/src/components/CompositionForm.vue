<script setup lang="ts">
import { ref, watch, toRef } from 'vue'
import uiFetch from './UIFetch'
import type { CompositionListItem, FullComposition, PrintSetting, SaveCompositionServerError, SaveVoiceServerError, Voice } from './AdminTypes'
import type { ValidationState } from './Validation'
import LoadingBar from './LoadingBar.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import TextInput from './TextInput.vue'
import FileInput from './FileInput.vue'
import SelectInput from './SelectInput.vue'
import PdfPreview from './PdfPreview.vue'
import { chunk, first, last } from 'lodash-es'
import { Pdf, type PdfModification } from './Pdf'

const deserializeFile = (text: string | undefined) => {
  if (text === undefined) return undefined

  return Uint8Array.from(atob(text), m => m.codePointAt(0) as number)
}

const serializeFile = (content: ArrayBuffer | undefined) => {
  if (content === undefined) return undefined

  const encodedContent = chunk(new Uint8Array(content), 0x10FFF) // see https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/fromCodePoint
    .map(chunk => String.fromCodePoint(...chunk))
    .reduce((a, b) => a + b)
  return btoa(encodedContent)
}

const serializeVoiceFile = async (content: Uint8Array | undefined, modifications: PdfModification[]) => {
  if (content === undefined) return undefined
  return serializeFile(await Pdf.applyModifications(content, modifications))
}

defineEmits<{
  'cancelEdit': []
}>()

const props = defineProps<{
  type: 'create' | 'edit'
  printSettingsUrl: string
  compositionUrl: string
}>()

const modifyType = ref(props.type)

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
  originalFile?: Uint8Array
  fileModifications: ({ id: string } & PdfModification)[]
  fileValidationState: ValidationState
  printSettingValidationState: ValidationState
  isSaving: boolean
  hasSavingFailed: boolean
}
type EditableComposition = {
  title: string
  titleValidationState: ValidationState
  voices: EditableVoice[]
  links: {
    self: string
    voices?: string
  }
}

const parseLoadedVoice = (voice: Voice) : EditableVoice => {
  return {
    state: { type: 'loadedVoice', isMarkedForDeletion: false, links: voice.links },
    name: voice.name,
    nameValidationState: { type: 'success' },
    originalFile: deserializeFile(voice.file),
    fileModifications: [],
    fileValidationState: { type: 'success' },
    printSetting: voice.printSetting,
    printSettingValidationState: { type: 'success' },
    isSaving: false,
    hasSavingFailed: false
  }
}

const composition = ref<EditableComposition>()
const activeVoice = ref<EditableVoice>()
const isLoading = ref(false)
const hasLoadingFailed = ref(false)
const loadComposition = async () => {
  switch (modifyType.value) {
    case 'create':
      composition.value = {
        title: '',
        titleValidationState: { type: 'success' },
        voices: [],
        links: {
          self: props.compositionUrl,
          voices: undefined
        }
      }
      break
    case 'edit': {
      const result = await uiFetch(isLoading, hasLoadingFailed, props.compositionUrl)
      if (result.succeeded) {
        const loadedComposition = (await result.response.json() as FullComposition)
        composition.value = {
          title: loadedComposition.title,
          titleValidationState: { type: 'success' },
          voices: loadedComposition.voices.map(parseLoadedVoice),
          links: loadedComposition.links
        }
        activeVoice.value = first(composition.value.voices)
      }
      break
    }
  }
}
loadComposition()

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
  activeVoice.value.originalFile = new Uint8Array(await v.arrayBuffer())
})

const voiceFileWithModifications = ref<Uint8Array>()
watch(
  [() => activeVoice.value?.originalFile, () => activeVoice.value?.fileModifications], async ([originalFile, fileModifications]) => {
  if (originalFile === undefined || fileModifications === undefined) return

  voiceFileWithModifications.value = await Pdf.applyModifications(originalFile, fileModifications)
}, { deep: true })

let nextModificationId = 1
const addVoiceFileModification = (modification: PdfModification) => {
  if (activeVoice.value === undefined) return

  activeVoice.value.fileModifications.push({ id: `${nextModificationId++}`, ...modification })
}

watch(activeVoice, (oldActiveVoice, newActiveVoice) => {
  if (newActiveVoice !== undefined && oldActiveVoice === newActiveVoice && newActiveVoice.state.type === 'loadedVoice') {
    newActiveVoice.state = { ...newActiveVoice.state, type: 'modifiedVoice' }
  }
}, { deep: true })

const addVoice = () => {
  if (composition.value === undefined) return

  composition.value.voices.push({
    state: { type: 'newVoice' },
    name: '',
    nameValidationState: { type: 'notValidated' },
    originalFile: undefined,
    fileModifications: [],
    fileValidationState: { type: 'notValidated' },
    printSetting: '',
    printSettingValidationState: { type: 'notValidated' },
    isSaving: false,
    hasSavingFailed: false
  })
  activeVoice.value = last(composition.value.voices)
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

const getVoiceUrl = (voice: EditableVoice) => {
  switch (voice.state.type) {
    case 'newVoice': return undefined
    case 'loadedVoice':
    case 'modifiedVoice': return voice.state.links.self
  }
}
const getVoiceSaveMethod = (voice: EditableVoice) => {
  switch (voice.state.type) {
    case 'newVoice': return 'POST'
    case 'loadedVoice':
    case 'modifiedVoice':
      return voice.state.isMarkedForDeletion ? 'DELETE' : 'PATCH'
  }
}
const saveVoice = async (voice: EditableVoice, newVoiceUrl: string) => {
  const url = getVoiceUrl(voice) || newVoiceUrl
  const httpMethod = getVoiceSaveMethod(voice)
  if (httpMethod === 'DELETE') {
    const result = await uiFetch(toRef(voice, 'isSaving'), toRef(voice, 'hasSavingFailed'), url, { method: httpMethod })
    if (result.succeeded) {
      return undefined
    }
    else {
      return voice
    }
  }
  else {
    const result = await uiFetch(toRef(voice, 'isSaving'), toRef(voice, 'hasSavingFailed'), url, {
      method: httpMethod,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        name: voice.name,
        file: await serializeVoiceFile(voice.originalFile, voice.fileModifications),
        printSetting: voice.printSetting
      })
    })
    if (result.succeeded) {
      const voice = await result.response.json() as Voice
      return parseLoadedVoice(voice)
    }
    else if (result.response !== undefined && result.response.status === 400) {
      const errors = await result.response.json() as SaveVoiceServerError[]
      voice.nameValidationState = errors.includes('EmptyName') ? { type: 'error', error: 'Bitte geben Sie den Namen der Stimme ein.' } : { type: 'success' }
      voice.fileValidationState = errors.includes('EmptyFile')
        ? { type: 'error', error: 'Bitte wählen Sie eine PDF-Datei aus.' }
        : errors.includes('InvalidFile')
          ? { type: 'error', error: 'Die PDF-Datei kann nicht gelesen werden.' }
          : { type: 'success' }
      voice.printSettingValidationState = errors.includes('UnknownPrintSetting') ? { type: 'error', error: 'Bitte wählen Sie eine gültige Druckeinstellung aus.' } : { type: 'success' }
      return voice
    }
    else {
      return voice
    }
  }
}

const saveVoices = async (voices: EditableVoice[], newVoiceUrl: string) => {
  return await Promise.all(voices.map(voice => saveVoice(voice, newVoiceUrl)))
}

const getCompositionSaveMethod = () => {
  switch (modifyType.value) {
    case 'create': return 'POST'
    case 'edit': return 'PATCH'
  }
}

const updateActiveVoice = (savedVoices: (EditableVoice | undefined)[], activeVoiceIndex: number) => {
  activeVoice.value = savedVoices.slice(activeVoiceIndex)
    .concat(savedVoices.slice(0, activeVoiceIndex).reverse())
    .find(v => v !== undefined)
}

const isSaving = ref(false)
const isSavingComposition = ref(false)
const hasSavingCompositionFailed = ref(false)

const saveComposition = async () => {
  if (composition.value === undefined) return

  composition.value.titleValidationState = { type: 'notValidated' }

  try {
    isSaving.value = true
    const result = await uiFetch(isSavingComposition, hasSavingCompositionFailed, composition.value.links.self, {
      method: getCompositionSaveMethod(),
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title: composition.value.title })
    })
    if (result.succeeded) {
      modifyType.value = 'edit'
      const compositionListItem = await result.response.json() as CompositionListItem
      const activeVoiceIndex = activeVoice.value !== undefined ? composition.value.voices.indexOf(activeVoice.value) : -1
      const activeVoiceIndexOrFirst = activeVoiceIndex === -1 ? 0 : activeVoiceIndex
      const savedVoices = await saveVoices(composition.value.voices, compositionListItem.links.voices)
      composition.value = {
        ...compositionListItem,
        titleValidationState: { type: 'success' },
        voices: savedVoices.filter(v => v !== undefined) as EditableVoice[]
      }
      updateActiveVoice(savedVoices, activeVoiceIndexOrFirst)
    }
    else if (result.response !== undefined && result.response.status === 400) {
      const errors = await result.response.json() as SaveCompositionServerError[]
      for (const error of errors) {
        if (error === 'EmptyTitle') {
          composition.value.titleValidationState = { type: 'error', error: 'Bitte geben Sie den Titel des Stücks ein.' }
        }
      }
    }
  }
  finally {
    isSaving.value = false
  }
}
</script>

<template>
  <div class="p-4">
    <h2 class="text-2xl small-caps">
        {{ modifyType === 'create' ? "Stück anlegen" : "Stück bearbeiten" }}
    </h2>
    
    <LoadingBar v-if="isLoading" />
    <ErrorWithRetry v-if="hasLoadingFailed" @retry="loadComposition">Fehler beim Laden.</ErrorWithRetry>
    <template v-else-if="composition !== undefined">
      <p v-if="hasSavingCompositionFailed" class="mt-4 text-musi-red">Fehler beim Speichern des Stücks.</p>
      <TextInput title="Titel" :validation-state="composition.titleValidationState" v-model="composition.title" />
      <h3 class="text-xl small-caps mt-4">Stimmen</h3>
      <ul class="nav-container">
        <li v-for="voice in composition.voices" :key="JSON.stringify(voice)">
          <a @click="activeVoice = voice" class="nav-item !flex items-center !pr-2" :class="{ active: activeVoice === voice }">
            <span :class="{
              'text-green-500': voice.state.type === 'newVoice',
              'text-yellow-500': voice.state.type === 'modifiedVoice' && !voice.state.isMarkedForDeletion,
              'text-musi-red line-through': (voice.state.type === 'loadedVoice' || voice.state.type === 'modifiedVoice') && voice.state.isMarkedForDeletion }">
              {{ voice.name || '<leer>' }}
            </span>
            <span v-if="voice.isSaving" class="btn-loading m-2 mr-0 w-5 h-5 inline-block"></span>
            <template v-else>
              <button class="p-2 hover:text-musi-red" title="Löschen" @click.stop="deleteVoice(voice)">
                <font-awesome-icon :icon="['fas', 'trash']" />
              </button>
              <span v-if="voice.hasSavingFailed" class="p-2 text-musi-red" title="Fehler beim Speichern">
                <font-awesome-icon :icon="['fas', 'info-circle']" />
              </span>
            </template>
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
        <div class="my-2">
          <span class="input-label">PDF bearbeiten</span>
          <div class="flex flex-row gap-2">
            <a class="btn btn-blue" @click="addVoiceFileModification({ type: 'scaleToA4' })">Seitenformat auf A4 ändern</a>
            <a class="btn btn-blue" @click="addVoiceFileModification({ type: 'zoom', relativeBounds: { x: 0, y: 0, width: 1, height: 1 } })">Zoomen</a>
            <a class="btn btn-blue" @click="addVoiceFileModification({ type: 'remove' })">Seiten entfernen</a>
            <a class="btn btn-blue" @click="addVoiceFileModification({ type: 'rotate', degrees: 0 })">Seiten drehen</a>
            <a class="btn btn-blue" @click="addVoiceFileModification({ type: 'cutPageLeftRight' })">Seiten in linke und rechte Hälfte teilen</a>
          </div>
          <ol class="mt-2 list-decimal list-inside">
            <li v-for="modification in activeVoice.fileModifications" :key="modification.id">
              <template v-if="modification.type === 'scaleToA4'">Seitenformat auf A4 ändern</template>
              <template v-else-if="modification.type === 'zoom'">
                <div class="inline-flex flex-row items-baseline gap-2">
                  <span>Zoomen -</span>
                  <label>X: <input class="input-text !w-20" type="number" step="0.01" v-model="modification.relativeBounds.x"></label>
                  <label>Y: <input class="input-text !w-20" type="number" step="0.01" v-model="modification.relativeBounds.y"></label>
                  <label>Breite: <input class="input-text !w-20" type="number" step="0.01" v-model="modification.relativeBounds.width"></label>
                  <label>Höhe: <input class="input-text !w-20" type="number" step="0.01" v-model="modification.relativeBounds.height"></label>
                </div>
              </template>
              <template v-else-if="modification.type === 'remove'">Seiten entfernen</template>
              <template v-else-if="modification.type === 'rotate'">
                <span>Seiten um <input class="input-text !w-20" type="number" step="0.1" v-model="modification.degrees"> Grad drehen</span>
              </template>
              <template v-else-if="modification.type === 'cutPageLeftRight'">
                <span>Seiten in linke und rechte Hälfte teilen</span>
              </template>
            </li>
          </ol>
        </div>
        <PdfPreview :file="voiceFileWithModifications" class="mt-6" />
      </div>
    </template>
  </div>

  <Teleport to="#command-bar">
    <button class="btn btn-solid btn-gold !px-8 !py-4" :disabled="isSaving" @click="$emit('cancelEdit')">Zurück zur Übersicht</button>
    <button class="btn btn-solid btn-gold !px-8 !py-4" :class="{ 'btn-loading': isSaving }" @click="saveComposition">Speichern</button>
  </Teleport>
</template>
