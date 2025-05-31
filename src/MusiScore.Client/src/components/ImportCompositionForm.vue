<script setup lang="ts">
import { computed, ref, toRef, watch } from 'vue'
import FolderInput from './FolderInput.vue'
import LoadButton from './LoadButton.vue'
import _ from 'lodash'
import type { ValidationState } from './Validation'
import { uiFetchAuthorized } from './UIFetch'
import { serializeFile, type CompositionListItem, type SaveCompositionServerError, type SaveVoiceServerError, type Voice as VoiceDto, type VoiceFileServerError } from './AdminTypes'
import toml from 'toml'
import pLimit from 'p-limit'

const props = defineProps<{
  compositionUrl: string
  inferPrintConfigUrl: string
}>()

defineEmits<{
  'cancelImport': []
}>()

const isSaving = ref(false)

const files = ref<File[]>()

type Voice = {
  id: string
  name: string
  nameValidationState: ValidationState
  isEditingName: boolean
  printConfig: string | undefined
  printConfigValidationState: ValidationState
  enabled: boolean
  file: string | ReadableStream<Uint8Array>
  fileValidationState: ValidationState
  isSaving: boolean
  hasSavingFailed: boolean
  isSaved: boolean
}

type Composition = {
  id: string
  title: string
  titleValidationState: ValidationState
  tags: { key: string, value: string }[]
  isEditingTitle: boolean
  isActive: boolean
  enabled: boolean
  isSaving: boolean
  hasSavingFailed: boolean
  isSaved: boolean
  voicesUrl: string | undefined
  voices: Voice[]
}

let nextId = 1

type Metadata = {
  compositionName: string | undefined
  data?: {
    composition?: {
      title?: string
      tags?: { key?: string, value?: string }[]
      is_active?: boolean
      voices?: {
        name?: string
        print_config?: string
      } []
    }
  }
}

const tryReadMetadata = async (file: File) : Promise<Metadata | undefined> => {
  const compositionName = file.webkitRelativePath.split('/').at(-2)
  try {
    return {
      compositionName,
      data: toml.parse(await file.text())
    }
  }
  catch (e) {
    return undefined
  }
}

const compositions = ref<Composition[]>()
watch(files, async files => {
  const metadata =
    (await Promise.all(_(files)
      .filter(v => v.name === '.metadata.toml')
      .map(tryReadMetadata)
      .value()
    ))
    .filter(v => v !== undefined)
  compositions.value = _(files)
    .filter(v => v.type === 'application/pdf')
    .map(v => ({ directoryName: v.webkitRelativePath.split('/').at(-2) || '<unbekannt>', fileName: v.name, content: v.stream() }))
    .groupBy(v => v.directoryName)
    .map((value, key) : Composition => {
      const compositionMetadata = metadata.find(v => v.compositionName === key)
      return {
        id: `${nextId++}`,
        title: compositionMetadata?.data?.composition?.title || key,
        titleValidationState: { type: 'notValidated' },
        isEditingTitle: false,
        tags: compositionMetadata?.data?.composition?.tags?.flatMap(v => v.key && v.value ? [{ key: v.key, value: v.value }] : []) || [],
        isActive: compositionMetadata?.data?.composition?.is_active || false,
        enabled: true,
        isSaving: false,
        hasSavingFailed: false,
        isSaved: false,
        voicesUrl: undefined,
        voices: value.map((v) : Voice => {
          const voiceName = v.fileName.replace(/\.[^.]*$/, '')
          const voiceMetadata = compositionMetadata?.data?.composition?.voices?.find?.(v => v.name === voiceName)
          return {
            id: `${nextId++}`,
            name: voiceName,
            nameValidationState: { type: 'notValidated' },
            isEditingName: false,
            printConfig: voiceMetadata?.print_config,
            printConfigValidationState: { type: 'notValidated' },
            enabled: true,
            file: v.content,
            fileValidationState: { type: 'notValidated' },
            isSaving: false,
            hasSavingFailed: false,
            isSaved: false
          }
        })
      }
    })
    .value()
})

const inferPrintConfig = async (voice: Voice) => {
  if (typeof voice.file !== 'string') {
    voice.file = serializeFile(await new Response(voice.file).bytes())
  }

  const result = await uiFetchAuthorized(
    toRef(voice, 'isSaving'),
    toRef(voice, 'hasSavingFailed'),
    props.inferPrintConfigUrl,
    {
      method: 'QUERY', // needs Node 22.2.0 with vite dev server, see https://github.com/nodejs/node/issues/51562#issuecomment-2151103116
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        file: voice.file
      })
    }
  )
  if (result.succeeded) {
    const response = (await result.response.json() as { printConfig: string })
    return response.printConfig
  }
  else if (result.response !== undefined && result.response.status === 400) {
    const errors = await result.response.json() as VoiceFileServerError[]
    voice.fileValidationState = errors.includes('EmptyFile')
      ? { type: 'error', error: 'Bitte wählen Sie eine PDF-Datei aus.' }
      : errors.includes('InvalidFile')
        ? { type: 'error', error: 'Die PDF-Datei kann nicht gelesen werden.' }
        : { type: 'success' }
  }
  else {
    // TODO what happend here?
  }
}

const saveVoice = async (voiceUrl: string, voice: Voice) => {
  if (voice.isSaved) return

  if (typeof voice.file !== 'string') {
    voice.file = serializeFile(await new Response(voice.file).bytes())
  }
  if (voice.printConfig === undefined) {
    voice.printConfig = await inferPrintConfig(voice)
  }
  if (voice.printConfig === undefined) return

  const result = await uiFetchAuthorized(
    toRef(voice, 'isSaving'),
    toRef(voice, 'hasSavingFailed'),
    voiceUrl,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        name: voice.name,
        file: voice.file,
        printConfig: voice.printConfig
      })
    }
  )
  if (result.succeeded) {
    const newVoice = await result.response.json() as VoiceDto
    voice.isSaved = true
    voice.name = newVoice.name
    voice.nameValidationState = { type: 'success' }
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
  }
  else {
    // TODO what happend here?
  }
}

const saveComposition = async (composition: Composition) => {
  if (composition.isSaved) return

  const result = await uiFetchAuthorized(
    toRef(composition, 'isSaving'),
    toRef(composition, 'hasSavingFailed'),
    props.compositionUrl,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        title: composition.title,
        tags: composition.tags,
        isActive: composition.isActive
      })
    }
  )
  if (result.succeeded) {
    composition.isSaved = true
    const compositionListItem = await result.response.json() as CompositionListItem
    composition.title = compositionListItem.title
    composition.titleValidationState = { type: 'success' }
    composition.tags = compositionListItem.tags.flatMap(v => v.value ? [{ key: v.key, value: v.value }] : [])
    composition.voicesUrl = compositionListItem.links.voices
  }
  else if (result.response !== undefined && result.response.status === 400) {
    const errors = await result.response.json() as SaveCompositionServerError[]
    composition.titleValidationState = errors.includes('EmptyTitle') ? { type: 'error', error: 'Bitte geben Sie den Titel des Stücks ein.' } : { type: 'success' }
  }
}

const limitSaveCompositions = pLimit(10)
const limitSaveVoices = pLimit(10)

const saveCompositionAndVoices = async (composition: Composition) => {
  await saveComposition(composition)

  const voicesUrl = composition.voicesUrl
  if (voicesUrl !== undefined) {
    await Promise.all(composition.voices
      .filter(v => v.enabled)
      .map(v => limitSaveVoices(() => saveVoice(voicesUrl, v))))
  }
}

const doImport = async () => {
  if (compositions.value === undefined) return
  compositions.value.forEach(v => v.isEditingTitle = false)
  compositions.value.flatMap(v => v.voices).forEach(v => v.isEditingName = false)
  isSaving.value = true
  try {
    await Promise.all(compositions.value
      .filter(v => v.enabled)
      .map(v => limitSaveCompositions(() => saveCompositionAndVoices(v))))
  }
  finally {
    isSaving.value = false
  }
}

const canImport = computed(() => {
  if (compositions.value === undefined) return false
  
  return compositions.value.some(v => v.enabled && !v.isSaved)
    || compositions.value.flatMap(v => v.voices).some(v => v.enabled && !v.isSaved)
})

type ImportInfo = {
  type: 'success' | 'error'
  message: string
}
const pluralize = (count: number, singular: string, plural: string) => {
  return count == 1 ? `${count} ${singular}` : `${count} ${plural}`
}
const importInfo = computed(() : ImportInfo | undefined => {
  if (compositions.value === undefined || isSaving.value) return undefined

  const savedCompositions = compositions.value.filter(v => v.isSaved).length
  const saveFailedCompositions = compositions.value.filter(v => v.enabled && v.hasSavingFailed).length
  const savedVoices = compositions.value.flatMap(v => v.voices).filter(v => v.isSaved).length
  const saveFailedVoices = compositions.value.flatMap(v => v.voices).filter(v => v.enabled && v.hasSavingFailed).length

  if (saveFailedCompositions > 0 && saveFailedVoices > 0) {
    return { type: 'error', message: `${pluralize(saveFailedCompositions, 'Stück', 'Stücke')} und ${pluralize(saveFailedVoices, 'Stimme', 'Stimmen')} konnten nicht importiert werden.` }
  }
  else if (saveFailedCompositions > 0) {
    return { type: 'error', message: `${pluralize(saveFailedCompositions, 'Stück', 'Stücke')} konnten nicht importiert werden.` }
  }
  else if (saveFailedVoices > 0) {
    return { type: 'error', message: `${pluralize(saveFailedVoices, 'Stimme', 'Stimmen')} konnten nicht importiert werden.` }
  }
  else if (savedCompositions > 0) {
    return { type: 'success', message: `${pluralize(savedCompositions, 'Stück', 'Stücke')} mit ${pluralize(savedVoices, 'Stimme', 'Stimmen')} wurden erfolgreich importiert.` }
  }
  return undefined
})
</script>

<template>
  <div class="p-4">
    <h2 class="text-2xl small-caps">Stücke importieren</h2>

    <FolderInput v-model="files" :disabled="isSaving" class="mt-2" />

    <template v-if="compositions === undefined"></template>
    <div v-else-if="compositions.length === 0" class="mt-4">
      <h3 class="text-lg">Keine PDF-Dateien im ausgewählten Ordner gefunden.</h3>
    </div>
    <div v-else class="flex flex-col gap-2">
      <div v-for="composition in compositions" :key="composition.id" class="border rounded mt-2 p-4">
        <fieldset :disabled="isSaving || composition.isSaved">
          <div v-if="composition.isEditingTitle" class="flex">
            <input class="input-text !rounded-r-none" type="text" required v-model="composition.title" />
            <button class="btn !rounded-l-none !border-l-none" @click="composition.isEditingTitle = false">✔</button>
          </div>
          <div v-else class="flex">
            <LoadButton :loading="composition.isSaving" class="btn-green !rounded-r-none" :class="{ 'btn-solid': composition.enabled }" @click="composition.enabled = !composition.enabled">
              {{ composition.title }}
              <template v-if="composition.isSaved">✔</template>
              <template v-else-if="composition.hasSavingFailed">❌</template>
            </LoadButton>
            <button class="btn !rounded-l-none !border-l-0" @click="composition.isEditingTitle = true"><font-awesome-icon :icon="['fas', 'pen']" /></button>
          </div>
        </fieldset>
        <h4 class="mt-4 text-xl small-caps" :class="{ 'opacity-50': isSaving || !composition.enabled }">Stimmen</h4>
        <div class="ml-4 mt-2 flex flex-wrap items-center gap-2">
          <div v-for="voice in composition.voices" :key="voice.id">
            <fieldset :disabled="!composition.enabled || isSaving || voice.isSaved">
              <div v-if="voice.isEditingName" class="flex">
                <input class="input-text !rounded-r-none" type="text" required v-model="voice.name" />
                <button class="btn !rounded-l-none !border-l-none" @click="voice.isEditingName = false">✔</button>
              </div>
              <div v-else class="flex">
                <LoadButton :loading="voice.isSaving" class="btn-blue !rounded-r-none" :class="{ 'btn-solid': voice.enabled }" @click="voice.enabled = !voice.enabled">
                  {{ voice.name || '<leer>' }}
                  <template v-if="voice.isSaved">✔</template>
                  <template v-else-if="voice.hasSavingFailed">❌</template>
                </LoadButton>
                <button class="btn !rounded-l-none !border-l-0" @click="voice.isEditingName = true"><font-awesome-icon :icon="['fas', 'pen']" /></button>
              </div>
            </fieldset>
          </div>
        </div>
      </div>
    </div>
  </div>

  <Teleport to="#command-bar">
    <button class="btn btn-solid btn-gold !px-8 !py-4" :disabled="isSaving" @click="$emit('cancelImport')">Zurück zur Übersicht</button>
    <LoadButton :loading="isSaving" :disabled="!canImport" class="btn-solid btn-gold !px-8 !py-4" @click="doImport">
      {{ importInfo?.type === 'error' ? 'Import erneut versuchen' : 'Import starten' }}
    </LoadButton>
    <span v-if="importInfo?.type === 'success'" class="text-musi-green">{{ importInfo.message }}</span>
    <span v-else-if="importInfo?.type === 'error'" class="text-musi-red">{{ importInfo.message }}</span>
  </Teleport>
</template>
