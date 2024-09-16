<script setup lang="ts">
import { ref, watch, toRef, computed } from 'vue'
import { uiFetchAuthorized } from './UIFetch'
import { deserializeFile, serializeFile, type CompositionListItem, type FullComposition, type PrintConfig, type SaveCompositionServerError, type SaveVoiceServerError, type Voice } from './AdminTypes'
import type { ValidationState } from './Validation'
import LoadingBar from './LoadingBar.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import LoadButton from './LoadButton.vue'
import TextInput from './TextInput.vue'
import FileInput from './FileInput.vue'
import SelectInput from './SelectInput.vue'
import PdfPreview from './PdfPreview.vue'
import { first, last } from 'lodash-es'
import { Pdf, type PDFFile, type PdfModification } from './Pdf'
import _ from 'lodash'

const serializeVoiceFile = async (content: Uint8Array | undefined, modifications: PdfModification[]) => {
  if (content === undefined) return undefined
  const file = await Pdf.applyModifications(content, modifications)
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
}>()

const modifyType = ref(props.type)

const printConfigs = ref<PrintConfig[]>()
const isLoadingPrintConfigs = ref(false)
const hasLoadingPrintConfigsFailed = ref(false)
const loadPrintConfigs = async () => {
  const result = await uiFetchAuthorized(isLoadingPrintConfigs, hasLoadingPrintConfigsFailed, props.printConfigsUrl)
  if (result.succeeded) {
    printConfigs.value = (await result.response.json() as PrintConfig[])
  }
}
loadPrintConfigs()

type EditableVoiceState =
    | { type: 'loadedVoice', isMarkedForDeletion: boolean, links: { self: string } }
    | { type: 'newVoice' }
    | { type: 'modifiedVoice', isMarkedForDeletion: boolean, links: { self: string } }
type EditableVoice = Omit<Voice, 'links' | 'file'> & {
  id: number
  state: EditableVoiceState
  nameValidationState: ValidationState
  originalFile?: Uint8Array
  fileModifications: ({ id: string; isDraft: boolean } & PdfModification)[]
  fileValidationState: ValidationState
  printConfigValidationState: ValidationState
  isSaving: boolean
  hasSavingFailed: boolean
}
type EditableComposition = {
  title: string
  titleValidationState: ValidationState
  composer: string | null
  arranger: string | null
  voices: EditableVoice[]
  links: {
    self: string
    voices?: string
  }
}

let nextVoiceId = 1
const parseLoadedVoice = (voice: Voice, voiceId?: number) : EditableVoice => {
  return {
    id: voiceId || nextVoiceId++,
    state: { type: 'loadedVoice', isMarkedForDeletion: false, links: voice.links },
    name: voice.name,
    nameValidationState: { type: 'success' },
    originalFile: deserializeFile(voice.file),
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
    case 'create':
      composition.value = {
        title: '',
        titleValidationState: { type: 'success' },
        composer: null,
        arranger: null,
        voices: [],
        links: {
          self: props.compositionUrl,
          voices: undefined
        }
      }
      break
    case 'edit': {
      const result = await uiFetchAuthorized(isLoading, hasLoadingFailed, props.compositionUrl)
      if (result.succeeded) {
        const loadedComposition = (await result.response.json() as FullComposition)
        composition.value = {
          title: loadedComposition.title,
          titleValidationState: { type: 'success' },
          composer: loadedComposition.composer,
          arranger: loadedComposition.arranger,
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

const voiceFileWithModifications = ref<PDFFile>()
watch(
  [() => activeVoice.value?.originalFile, () => activeVoice.value?.fileModifications], async ([originalFile, fileModifications]) => {
  if (originalFile === undefined || fileModifications === undefined) {
    voiceFileWithModifications.value = undefined
    return
  }

  voiceFileWithModifications.value = await Pdf.applyModifications(originalFile, fileModifications)
}, { deep: true })

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

const printWithPrintDialog = async (file: Uint8Array) => {
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

const selectedFilePages = ref([] as readonly number[])
const selectAllPages = () => {
  if (voiceFileWithModifications.value === undefined) {
    selectedFilePages.value = []
    return
  }
  selectedFilePages.value = _.range(0, voiceFileWithModifications.value.pageCount)
}

watch(() => voiceFileWithModifications.value?.pageCount, pageCount => {
  if (pageCount === undefined) {
    selectedFilePages.value = []
    return
  }
  selectedFilePages.value = selectedFilePages.value.filter(v => v < pageCount)
})

const lastFileModification = computed(() => activeVoice.value?.fileModifications.at(-1))

watch(selectedFilePages, selectedPages => {
  if (lastFileModification.value?.isDraft) {
    lastFileModification.value.pages = selectedPages
  }
})

let nextModificationId = 1
const addVoiceFileModification = (modification: { isDraft: boolean } & PdfModification) => {
  if (activeVoice.value === undefined) return

  activeVoice.value.fileModifications.push({ id: `${nextModificationId++}`, ...modification })
}

const pagesToString = (pages: readonly number[]) => {
  const pageNumbers = _.orderBy(pages.map(v => v + 1))
  if (pageNumbers.length === 0) return `Keine Seiten`
  if (pageNumbers.length === 1) return `Seite ${pageNumbers[0]}`
  const lastPage = pageNumbers.pop()
  return `Seiten ${pageNumbers.join(', ')} und ${lastPage}`
}

watch(activeVoice, (oldActiveVoice, newActiveVoice) => {
  if (newActiveVoice !== undefined && oldActiveVoice === newActiveVoice && newActiveVoice.state.type === 'loadedVoice') {
    newActiveVoice.state = { ...newActiveVoice.state, type: 'modifiedVoice' }
  }
}, { deep: true })

const addVoice = () => {
  if (composition.value === undefined) return

  composition.value.voices.push({
    id: nextVoiceId++,
    state: { type: 'newVoice' },
    name: '',
    nameValidationState: { type: 'notValidated' },
    originalFile: undefined,
    fileModifications: [],
    fileValidationState: { type: 'notValidated' },
    printConfig: '',
    printConfigValidationState: { type: 'notValidated' },
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
  if (voice.state.type === 'loadedVoice') {
    return voice
  }

  const url = getVoiceUrl(voice) || newVoiceUrl
  const httpMethod = getVoiceSaveMethod(voice)
  if (httpMethod === 'DELETE') {
    const result = await uiFetchAuthorized(toRef(voice, 'isSaving'), toRef(voice, 'hasSavingFailed'), url, { method: httpMethod })
    if (result.succeeded) {
      return undefined
    }
    else {
      return voice
    }
  }
  else {
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
        composer: composition.value.composer,
        arranger: composition.value.arranger
      })
    }
    case 'edit': return {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        title: composition.value.title,
        updateComposer: true,
        composer: composition.value.composer,
        updateArranger: true,
        arranger: composition.value.arranger
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
      <TextInput title="Titel" :validation-state="composition.titleValidationState" v-model="composition.title" class="mt-6" />
      <TextInput title="Komponist" :validation-state="{ type: 'success' }" v-model="composition.composer" :required="false" class="mt-6" />
      <TextInput title="Arrangeur" :validation-state="{ type: 'success' }" v-model="composition.arranger" :required="false" class="mt-6" />
      <h3 class="text-xl small-caps mt-4">Stimmen</h3>
      <ul class="nav-container">
        <li v-for="voice in composition.voices" :key="voice.id">
          <a @click="activeVoice = voice" class="nav-item !flex items-center !pr-2" :class="{ active: activeVoice === voice }">
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
          <a class="nav-item !py-5" @click="addVoice()">+ Neue Stimme</a>
        </li>
      </ul>
      <div v-if="activeVoice !== undefined">
        <TextInput title="Name" :validation-state="activeVoice.nameValidationState" v-model="activeVoice.name" class="mt-6" />
        <FileInput title="PDF-Datei" :validation-state="activeVoice.fileValidationState" v-model="activeVoiceFile" class="mt-6" />
        <div class="mt-6 flex gap-2">
          <SelectInput v-if="printConfigs !== undefined" title="Druckeinstellung" :options="printConfigs.map(v => ({ key: v.key, value: v.name}))" :validation-state="activeVoice.printConfigValidationState" v-model="activeVoice.printConfig" />
          <ErrorWithRetry v-else-if="hasLoadingPrintConfigsFailed" type="inline" @retry="loadPrintConfigs" class="self-end">Fehler beim Laden der Druckeinstellungen.</ErrorWithRetry>
        </div>
        <div class="mt-6">
          <span class="input-label">PDF drucken</span>
          <div class="flex flex-row flex-wrap gap-2">
            <LoadButton v-if="voiceFileWithModifications !== undefined && activeVoice.printConfig !== ''"
              :loading="isPrinting"
              class="btn-blue"
              @click="printWithPrintConfig(activeVoice)">
              Mit Druckeinstellungen drucken
              <span v-if="hasPrintingFailed" class="ml-2 text-musi-red" title="Fehler beim Drucken">
                <font-awesome-icon :icon="['fas', 'info-circle']" />
              </span>
            </LoadButton>
            <button v-else class="btn btn-blue" disabled="true">Mit Druckeinstellungen drucken</button>
            
            <button v-if="voiceFileWithModifications !== undefined" class="btn btn-blue" @click="printWithPrintDialog(voiceFileWithModifications.data)">Mit Druckdialog drucken</button>
            <button v-else class="btn btn-blue" disabled="true">Mit Druckdialog drucken</button>
          </div>
        </div>
        <div class="mt-6">
          <span class="input-label">PDF bearbeiten</span>
          <div class="flex flex-row flex-wrap gap-2">
            <button class="btn btn-blue" @click="selectAllPages()" :disabled="selectedFilePages.length === voiceFileWithModifications?.pageCount">Alle Seiten auswählen</button>
            <button class="btn btn-blue" @click="selectedFilePages = []" :disabled="selectedFilePages.length === 0">Seitenauswahl aufheben</button>
          </div>
          <div class="mt-2 flex flex-row flex-wrap gap-2">
            <button class="btn btn-green" @click="addVoiceFileModification({ type: 'scaleToA4', pages: selectedFilePages, isDraft: false })" :disabled="selectedFilePages.length === 0">Seitenformat auf A4 ändern</button>
            <button class="btn btn-green" @click="addVoiceFileModification({ type: 'zoom', pages: selectedFilePages, relativeBounds: { x: 0.01, y: 0.01, width: 0.98, height: 0.98 }, isDraft: true })" :disabled="selectedFilePages.length === 0">Zoomen</button>
            <button class="btn btn-green" @click="addVoiceFileModification({ type: 'remove', pages: selectedFilePages, isDraft: false})" :disabled="selectedFilePages.length === 0">Seiten entfernen</button>
            <button class="btn btn-green" @click="addVoiceFileModification({ type: 'rotate', pages: selectedFilePages, degrees: 0, isDraft: true })" :disabled="selectedFilePages.length === 0">Seiten drehen</button>
            <button class="btn btn-green" @click="addVoiceFileModification({ type: 'cutPageLeftRight', pages: selectedFilePages, isDraft: false })" :disabled="selectedFilePages.length === 0">Seiten in linke und rechte Hälfte teilen</button>
            <button class="btn btn-green" @click="addVoiceFileModification({ type: 'orderPages', pages: selectedFilePages, isDraft: false })" :disabled="selectedFilePages.length < 1">Seiten nach Markierungsreihenfolge sortieren</button>
          </div>
          <ol class="mt-2 list-decimal list-inside">
            <li v-for="modification in activeVoice.fileModifications" :key="modification.id">
              <template v-if="modification.type === 'scaleToA4'">Seitenformat auf A4 ändern</template>
              <template v-else-if="modification.type === 'zoom'">
                <div class="inline-flex flex-row items-center gap-2">
                  <span>{{ pagesToString(modification.pages) }} zoomen</span>
                  <template v-if="modification.isDraft">
                    <div class="grid grid-cols-3 grid-rows-4 gap-1">
                      <a title="Ausschnitt nach oben bewegen" class="col-start-2 row-span-2 btn btn-blue" @click="modification.relativeBounds.y += 0.02"><font-awesome-icon :icon="['fas', 'arrow-up']" /></a>
                      <a title="Ausschnitt nach unten bewegen" class="col-start-2 row-span-2 btn btn-blue" @click="modification.relativeBounds.y -= 0.02"><font-awesome-icon :icon="['fas', 'arrow-down']" /></a>
                      <a title="Ausschnitt nach links bewegen" class="col-start-1 row-start-2 row-span-2 btn btn-blue" @click="modification.relativeBounds.x -= 0.02"><font-awesome-icon :icon="['fas', 'arrow-left']" /></a>
                      <a title="Ausschnitt nach rechts bewegen" class="col-start-3 row-start-2 row-span-2 btn btn-blue" @click="modification.relativeBounds.x += 0.02"><font-awesome-icon :icon="['fas', 'arrow-right']" /></a>
                    </div>
                    <a title="Ausschnitt verkleinern" class="btn btn-blue" @click="modification.relativeBounds.x -= 0.01; modification.relativeBounds.y -= 0.01; modification.relativeBounds.width += 0.02; modification.relativeBounds.height += 0.02"><font-awesome-icon :icon="['fas', 'magnifying-glass-minus']" /></a>
                    <a title="Ausschnitt vergrößern" class="btn btn-blue" @click="modification.relativeBounds.x += 0.01; modification.relativeBounds.y += 0.01; modification.relativeBounds.width -= 0.02; modification.relativeBounds.height -= 0.02"><font-awesome-icon :icon="['fas', 'magnifying-glass-plus']" /></a>
                  </template>
                </div>
              </template>
              <template v-else-if="modification.type === 'remove'">
                <span>{{ pagesToString(modification.pages) }} entfernen</span>
              </template>
              <template v-else-if="modification.type === 'rotate'">
                <span>{{ pagesToString(modification.pages) }} um
                  <input v-if="modification.isDraft" class="input-text !w-20" type="number" step="0.1" v-model="modification.degrees">
                  <template v-else>{{ modification.degrees }}</template>
                  Grad drehen</span>
              </template>
              <template v-else-if="modification.type === 'cutPageLeftRight'">
                <span>{{ pagesToString(modification.pages) }} in linke und rechte Hälfte teilen</span>
              </template>
              <template v-else-if="modification.type === 'orderPages'">
                <span>{{ pagesToString(modification.pages) }} sortieren</span>
              </template>
              <a v-if="modification.isDraft" title="Änderung akzeptieren" class="ml-2 btn btn-green !py-1 !px-2" @click="modification.isDraft = false">✔</a>
              <a v-if="modification === lastFileModification" title="Änderung verwerfen" class="ml-2 btn btn-red !py-1 !px-2" @click="activeVoice.fileModifications.pop()">❌</a>
            </li>
          </ol>
        </div>
        <PdfPreview
          :file="voiceFileWithModifications?.data"
          v-model:selected-pages="selectedFilePages"
          :is-rotating="lastFileModification?.type === 'rotate' && lastFileModification.isDraft"
          class="mt-6" />
      </div>
    </template>
  </div>

  <Teleport to="#command-bar">
    <button class="btn btn-solid btn-gold !px-8 !py-4" :disabled="isSaving" @click="$emit('cancelEdit')">Zurück zur Übersicht</button>
    <LoadButton :loading="isSaving" class="btn-solid btn-gold !px-8 !py-4" @click="saveComposition">Speichern</LoadButton>
  </Teleport>
</template>
