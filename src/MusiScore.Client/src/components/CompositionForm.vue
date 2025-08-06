<script setup lang="ts">
import { ref, watch, toRef } from 'vue'
import { uiFetchAuthorized } from './UIFetch'
import { serializeFile, type CompositionListItem, type FullComposition, type PrintConfig, type SaveCompositionServerError, type SaveVoiceServerError, type ExistingTag, type Voice, type CompositionTemplate, type VoiceDefinition } from './AdminTypes'
import type { ValidationState } from './Validation'
import LoadingBar from './LoadingBar.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import LoadButton from './LoadButton.vue'
import TextInput from './TextInput.vue'
import FileInput from './FileInput.vue'
import SelectInput from './SelectInput.vue'
import VoiceSheetEditor from './VoiceSheetEditor.vue'
import { first, last } from 'lodash-es'
import { Pdf, type PdfModification } from './Pdf'
import _ from 'lodash'
import VoiceForm from './VoiceForm.vue'

// see https://stackoverflow.com/a/7616484
const getBlobHash = (data: Uint8Array) => {
  let hash = 0
  for (const v of data) {
    hash = (hash << 5) - hash + v
    hash |= 0
  }
  return `${hash}`
};

const getFileHash = (file: Loadable<Uint8Array>) => {
  return file.type === 'loaded' ? getBlobHash(file.data) : undefined
}

const serializeVoiceFile = async (content: Loadable<Uint8Array>, modifications: PdfModification[]) => {
  if (content.type !== 'loaded') return undefined
  const file = await Pdf.applyModifications(content.data, modifications)
  return serializeFile(file.data)
}

defineEmits<{
  'cancelEdit': []
}>()

const props = defineProps<{
  type: 'create' | 'edit'
  printConfigsUrl: string
  testPrintConfigUrl: string
  compositionUrl: string
  compositionTemplateUrl: string
  voiceDefinitionsUrl: string
}>()

const modifyType = ref(props.type)

const printConfigs = ref<PrintConfig[]>()
const isLoadingPrintConfigs = ref(false)
const hasLoadingPrintConfigsFailed = ref(false)
const loadPrintConfigs = async () => {
  const result = await uiFetchAuthorized(isLoadingPrintConfigs, hasLoadingPrintConfigsFailed, props.printConfigsUrl)
  if (result.succeeded) {
    printConfigs.value = await result.response.json()
  }
}
loadPrintConfigs()

type Loadable<T> =
  { type: 'empty' } |
  { type: 'notLoaded', url: string } |
  { type: 'loading', url: string } |
  { type: 'loadingFailed', url: string } |
  { type: 'loaded', data: T }

type EditableVoiceState =
    | { type: 'loadedVoice', isMarkedForDeletion: boolean, links: { self: string } }
    | { type: 'newVoice' }
    | { type: 'modifiedVoice', isMarkedForDeletion: boolean, links: { self: string } }
type LoadedVoice = Pick<EditableVoice, 'name' | 'printConfig'> & { fileHash: string | undefined }
type EditableVoice = Omit<Voice, 'links'> & {
  loadedData: LoadedVoice | undefined
  id: number
  state: EditableVoiceState
  nameValidationState: ValidationState
  originalFile: Loadable<Uint8Array>
  fileModifications: ({ id: string; isDraft: boolean } & PdfModification)[]
  fileValidationState: ValidationState
  printConfigValidationState: ValidationState
  isSaving: boolean
  hasSavingFailed: boolean
}
type EditableComposition = {
  title: string
  titleValidationState: ValidationState
  tags: ExistingTag[]
  voices: EditableVoice[]
  links: {
    self: string
    voices?: string
  }
}

let nextVoiceId = 1
const parseLoadedVoice = (voice: Voice, voiceId?: number) : EditableVoice => {
  return {
    loadedData: { name: voice.name, printConfig: voice.printConfig, fileHash: undefined },
    id: voiceId || nextVoiceId++,
    state: { type: 'loadedVoice', isMarkedForDeletion: false, links: voice.links },
    name: voice.name,
    nameValidationState: { type: 'success' },
    originalFile: { type: 'notLoaded', url: voice.links.sheet },
    fileModifications: [],
    fileValidationState: { type: 'success' },
    printConfig: voice.printConfig,
    printConfigValidationState: { type: 'success' },
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
    case 'create': {
      const result = await uiFetchAuthorized(isLoading, hasLoadingFailed, props.compositionTemplateUrl)
      if (result.succeeded) {
        const template = await result.response.json() as CompositionTemplate
        composition.value = {
          title: template.title,
          titleValidationState: { type: 'success' },
          tags: template.tags,
          voices: [],
          links: {
            self: props.compositionUrl,
            voices: undefined
          }
        }
        activeVoice.value = first(composition.value.voices)
      }
      break
    }
    case 'edit': {
      const result = await uiFetchAuthorized(isLoading, hasLoadingFailed, props.compositionUrl)
      if (result.succeeded) {
        const loadedComposition = (await result.response.json() as FullComposition)
        composition.value = {
          title: loadedComposition.title,
          titleValidationState: { type: 'success' },
          tags: loadedComposition.tags,
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

const voiceDefinitions = ref<VoiceDefinition[]>()
const isLoadingVoiceDefinitions = ref(false)
const hasLoadingVoiceDefinitionsFailed = ref(false)
const loadVoiceDefinitions = async () => {
  const result = await uiFetchAuthorized(isLoadingVoiceDefinitions, hasLoadingVoiceDefinitionsFailed, props.voiceDefinitionsUrl)
  if (result.succeeded) {
    voiceDefinitions.value = await result.response.json() as VoiceDefinition[]
  }
  else {
    voiceDefinitions.value = undefined
  }
}
loadVoiceDefinitions()

const loadVoiceSheet = async () => {
  if (activeVoice.value === undefined) {
    return
  }

  if (activeVoice.value.originalFile.type !== 'notLoaded' && activeVoice.value.originalFile.type != 'loadingFailed') {
    return
  }

  activeVoice.value.originalFile = { type: 'loading', url: activeVoice.value.originalFile.url }

  const result = await uiFetchAuthorized(ref(false), ref(false), activeVoice.value.originalFile.url)
  if (result.succeeded) {
    const fileContent = await result.response.bytes()
    if (activeVoice.value.loadedData !== undefined) {
      activeVoice.value.loadedData.fileHash = getBlobHash(fileContent)
    }
    activeVoice.value.originalFile = { 'type': 'loaded', data: fileContent }
  }
  else {
    activeVoice.value.originalFile = { 'type': 'loadingFailed', url: activeVoice.value.originalFile.url }
  }
}
watch(activeVoice, loadVoiceSheet)

const activeVoiceFile = ref<File>()
watch(activeVoiceFile, async v => {
  if (activeVoice.value === undefined) return
  if (v === undefined) {
    activeVoice.value = undefined
    return
  }
  if (!activeVoice.value.name) {
    activeVoice.value.name = v.name.substring(0, v.name.lastIndexOf('.'))
  }
  activeVoice.value.originalFile = { type: 'loaded', data: new Uint8Array(await v.arrayBuffer()) }
})

const isPrinting = ref(false)
const hasPrintingFailed = ref(false)
const printWithPrintConfig = async (voice: EditableVoice) => {
  await uiFetchAuthorized(isPrinting, hasPrintingFailed, props.testPrintConfigUrl, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      file: await serializeVoiceFile(voice.originalFile, voice.fileModifications),
      printConfig: voice.printConfig
    })
  })
}

const printWithPrintDialog = async (voice: EditableVoice) => {
  const file = await serializeVoiceFile(voice.originalFile, voice.fileModifications)
  if (file === undefined) return

  const pdfBlob = new Blob([file], { type: 'application/pdf' })
  const pdfUrl = URL.createObjectURL(pdfBlob)

  var iframe = document.createElement('iframe')
  iframe.style.display = 'none'
  iframe.src = pdfUrl
  document.body.appendChild(iframe)
  await new Promise<void>((resolve, _reject) => iframe.onload = () => resolve())
  if (iframe.contentWindow === null) {
    // TODO show error
    return
  }
  iframe.contentWindow.focus()
  iframe.contentWindow.print()
}

watch(activeVoice, (oldActiveVoice, newActiveVoice) => {
  if (newActiveVoice === undefined || oldActiveVoice !== newActiveVoice || (newActiveVoice.state.type !== 'loadedVoice' && newActiveVoice.state.type !== 'modifiedVoice')) {
    return
  }
  const currentData : LoadedVoice = {
    name: newActiveVoice.name,
    printConfig: newActiveVoice.printConfig,
    fileHash: getFileHash(newActiveVoice.originalFile),
  }

  const isUnmodified = newActiveVoice.fileModifications.length === 0 && _.isEqual(newActiveVoice.loadedData, currentData)
  const newState : typeof newActiveVoice.state = isUnmodified
    ? { ...newActiveVoice.state, type: 'loadedVoice' }
    : { ...newActiveVoice.state, type: 'modifiedVoice' }
  if (_.isEqual(newActiveVoice.state, newState)) {
    return
  }
  newActiveVoice.state = newState
}, { deep: true })

const addVoice = (data: { name: EditableVoice['name'], originalFile: EditableVoice['originalFile'], printConfig: EditableVoice['printConfig'] }) => {
  if (composition.value === undefined) return

  composition.value.voices.push({
    loadedData: { ...data, fileHash: getFileHash(data.originalFile) },
    ...data,
    id: nextVoiceId++,
    state: { type: 'newVoice' },
    nameValidationState: { type: 'notValidated' },
    fileModifications: [],
    fileValidationState: { type: 'notValidated' },
    printConfigValidationState: { type: 'notValidated' },
    isSaving: false,
    hasSavingFailed: false
  })
}

const addVoiceAndActivate = () => {
  if (composition.value === undefined) return

  addVoice({ name: '', originalFile: { type: 'empty' }, printConfig: '' })
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
    case 'loadedVoice': return voice.state.isMarkedForDeletion ? 'DELETE' : undefined
    case 'modifiedVoice': return voice.state.isMarkedForDeletion ? 'DELETE' : 'PATCH'
  }
}
const saveVoice = async (voice: EditableVoice, newVoiceUrl: string) => {
  const url = getVoiceUrl(voice) || newVoiceUrl
  const httpMethod = getVoiceSaveMethod(voice)
  switch (httpMethod) {
    case 'DELETE': {
      const result = await uiFetchAuthorized(toRef(voice, 'isSaving'), toRef(voice, 'hasSavingFailed'), url, { method: httpMethod })
      if (result.succeeded) {
        return undefined
      }
      else {
        return voice
      }
    }
    case 'PATCH':
    case 'POST': {
      const result = await uiFetchAuthorized(toRef(voice, 'isSaving'), toRef(voice, 'hasSavingFailed'), url, {
        method: httpMethod,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: voice.name,
          file: await serializeVoiceFile(voice.originalFile, voice.fileModifications),
          printConfig: voice.printConfig
        })
      })
      if (result.succeeded) {
        const newVoice = await result.response.json() as Voice
        return parseLoadedVoice(newVoice, voice.id)
      }
      else if (result.response !== undefined && result.response.status === 400) {
        const errors = await result.response.json() as SaveVoiceServerError[]
        voice.nameValidationState = errors.includes('EmptyName') ? { type: 'error', error: 'Bitte geben Sie den Namen der Stimme ein.' } : { type: 'success' }
        voice.fileValidationState = errors.includes('EmptyFile')
          ? { type: 'error', error: 'Bitte wählen Sie eine PDF-Datei aus.' }
          : errors.includes('InvalidFile')
            ? { type: 'error', error: 'Die PDF-Datei kann nicht gelesen werden.' }
            : { type: 'success' }
        voice.printConfigValidationState = errors.includes('UnknownPrintConfig') ? { type: 'error', error: 'Bitte wählen Sie eine gültige Druckeinstellung aus.' } : { type: 'success' }
        return voice
      }
      else {
        // TODO what happend here?
        return voice
      }
    }
    case undefined:
      return voice
  }
}

const saveVoices = async (voices: EditableVoice[], newVoiceUrl: string) => {
  return await Promise.all(voices.map(voice => saveVoice(voice, newVoiceUrl)))
}

const getCompositionSaveHttpParams = () => {
  if (composition.value === undefined) return undefined

  switch (modifyType.value) {
    case 'create': return {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        title: composition.value.title,
        tags: composition.value.tags.filter(v => v.value).map(v => ({ key: v.key, value: v.value })),
      })
    }
    case 'edit': return {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        title: composition.value.title,
        tagsToAdd: composition.value.tags.filter(v => v.value).map(v => ({ key: v.key, value: v.value})),
        tagsToRemove: composition.value.tags.filter(v => !v.value).map(v => v.key),
      })
    }
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
    const result = await uiFetchAuthorized(isSavingComposition, hasSavingCompositionFailed, composition.value.links.self, getCompositionSaveHttpParams())
    if (result.succeeded) {
      modifyType.value = 'edit'
      const compositionListItem = await result.response.json() as CompositionListItem
      const activeVoiceIndex = activeVoice.value !== undefined ? composition.value.voices.indexOf(activeVoice.value) : -1
      const activeVoiceIndexOrFirst = activeVoiceIndex === -1 ? 0 : activeVoiceIndex
      const savedVoices = await saveVoices(composition.value.voices, compositionListItem.links.voices)
      composition.value = {
        ...compositionListItem,
        tags: compositionListItem.tags,
        titleValidationState: { type: 'success' },
        voices: savedVoices.filter(v => v !== undefined) as EditableVoice[],
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

  await loadVoiceDefinitions()
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
      <TextInput title="Titel" :validation-state="composition.titleValidationState" v-model="composition.title" class="mt-6" />
      <div class="flex flex-col md:flex-row md:flex-wrap gap-4 mt-6">
        <template v-for="tag in composition.tags" :key="tag.key">
          <TextInput v-if="tag.settings.valueType === 'text'" :title="tag.title" :required="false" :suggestions="tag.otherValues" :validation-state="{ type: 'success' }" v-model="tag.value" />
          <div v-else-if="tag.settings.valueType === 'multi-line-text'">
            <label class="input">
              <span class="input-label">{{ tag.title }}</span>
              <textarea class="input-textarea" v-model="tag.value"></textarea>
            </label>
          </div>
        </template>
      </div>
      <h3 class="text-xl small-caps mt-4">Stimmen</h3>
      <ul class="nav-container">
        <li v-for="voice in composition.voices" :key="voice.id">
          <a @click="activeVoice = voice" class="nav-item flex! items-center pr-2!" :class="{ active: activeVoice === voice }">
            <span :class="{
              'text-green-500': voice.state.type === 'newVoice',
              'text-yellow-500': voice.state.type === 'modifiedVoice' && !voice.state.isMarkedForDeletion,
              'text-musi-red line-through': (voice.state.type === 'loadedVoice' || voice.state.type === 'modifiedVoice') && voice.state.isMarkedForDeletion }">
              {{ voice.name || '<leer>' }}
            </span>
            <LoadingBar v-if="voice.isSaving" type="minimal" class="m-2 mr-0 w-5 h-5" />
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
          <a class="nav-item py-5!" @click="addVoiceAndActivate()">+ Neue Stimme</a>
        </li>
      </ul>
      <div v-if="activeVoice !== undefined">
        <div class="flex gap-2 items-center mt-6">
          <VoiceForm :voices="voiceDefinitions || []" v-model="activeVoice.name" label="Name" />
          <LoadingBar v-if="isLoadingVoiceDefinitions" type="minimal" />
          <ErrorWithRetry v-else-if="hasLoadingVoiceDefinitionsFailed" type="inline" @retry="loadVoiceDefinitions">Fehler beim Laden der Stimmen.</ErrorWithRetry>
        </div>
        <FileInput title="PDF-Datei" :validation-state="activeVoice.fileValidationState" v-model="activeVoiceFile" class="mt-6" />
        <div class="mt-6 flex gap-2">
          <SelectInput v-if="printConfigs !== undefined" title="Druckeinstellung" :options="printConfigs.map(v => ({ key: v.key, value: v.name}))" :validation-state="activeVoice.printConfigValidationState" v-model="activeVoice.printConfig" />
          <ErrorWithRetry v-else-if="hasLoadingPrintConfigsFailed" type="inline" @retry="loadPrintConfigs" class="self-end">Fehler beim Laden der Druckeinstellungen.</ErrorWithRetry>
        </div>
        <div class="mt-6">
          <span class="input-label">PDF drucken</span>
          <div class="flex flex-row flex-wrap gap-2">
            <LoadButton v-if="activeVoice.originalFile.type === 'loaded' && activeVoice.printConfig !== ''"
              :loading="isPrinting"
              class="btn-blue"
              @click="printWithPrintConfig(activeVoice)">
              Mit Druckeinstellungen drucken
              <span v-if="hasPrintingFailed" class="ml-2 text-musi-red" title="Fehler beim Drucken">
                <font-awesome-icon :icon="['fas', 'info-circle']" />
              </span>
            </LoadButton>
            <button v-else class="btn btn-blue" disabled="true">Mit Druckeinstellungen drucken</button>
            
            <button v-if="activeVoice.originalFile.type === 'loaded'" class="btn btn-blue" @click="printWithPrintDialog(activeVoice)">Mit Druckdialog drucken</button>
            <button v-else class="btn btn-blue" disabled="true">Mit Druckdialog drucken</button>
          </div>
        </div>
        <div class="mt-6">
          <span class="input-label">PDF bearbeiten</span>
          <LoadingBar v-if="activeVoice.originalFile.type === 'loading'"></LoadingBar>
          <ErrorWithRetry v-else-if="activeVoice.originalFile.type === 'loadingFailed'" type="banner" @retry="loadVoiceSheet" class="self-end">Fehler beim Laden der Vorschau.</ErrorWithRetry>
          <VoiceSheetEditor v-else-if="activeVoice.originalFile.type === 'loaded'"
            :original-file="activeVoice.originalFile.data"
            v-model:file-modifications="activeVoice.fileModifications"
            @extract-pages="doc => addVoice({name: '', originalFile: { type: 'loaded', data: doc }, printConfig: '' })"></VoiceSheetEditor>
        </div>
      </div>
    </template>
  </div>

  <Teleport to="#command-bar">
    <button class="btn btn-solid btn-gold px-8! py-4!" :disabled="isSaving" @click="$emit('cancelEdit')">Zurück zur Übersicht</button>
    <LoadButton :loading="isSaving" class="btn-solid btn-gold px-8! py-4!" @click="saveComposition">Speichern</LoadButton>
  </Teleport>
</template>
