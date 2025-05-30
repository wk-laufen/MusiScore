<script setup lang="ts">
import { computed, ref, toRef } from 'vue'
import type { EditablePrintConfig, PrintConfigDto, PrintConfigInputs, PrintConfigSaveError } from './PrintConfigTypes'
import { uiFetchAuthorized } from './UIFetch'
import _ from 'lodash'
import PrintConfigItem from './PrintConfigItem.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import LoadingBar from './LoadingBar.vue'

const props = defineProps<{
  printConfigsUrl: string
}>()

let nextPrintConfigId = 1
const printConfigs = ref<EditablePrintConfig[]>()
const isLoadingPrintConfigs = ref(false)
const hasLoadingPrintConfigsFailed = ref(false)
const loadPrintConfigs = async () => {
  const result = await uiFetchAuthorized(isLoadingPrintConfigs, hasLoadingPrintConfigsFailed, props.printConfigsUrl)
  if (result.succeeded) {
    const loadedPrintConfigs = await result.response.json() as PrintConfigDto[]
    printConfigs.value = loadedPrintConfigs.map((v) : EditablePrintConfig => ({
      loadedData: { key: v.key, name: v.name, cupsCommandLineArgs: v.cupsCommandLineArgs, reorderPagesAsBooklet: v.reorderPagesAsBooklet },
      ...v,
      id: `${nextPrintConfigId++}`,
      isNew: false,
      keyValidationState: { type: 'success' },
      keyIsReadOnly: true,
      nameValidationState: { type: 'success' },
      sortOrder: v.sortOrder,
      cupsCommandLineArgsValidationState: { type: 'success' },
      isSaving: false,
      hasSavingFailed: false
    }))
  }
}
loadPrintConfigs()

const addPrintConfig = () => {
  if (printConfigs.value === undefined) return

  printConfigs.value.push({
    loadedData: undefined,
    id: `${nextPrintConfigId++}`,
    isNew: true,
    key: '',
    keyValidationState: { type: 'notValidated' },
    keyIsReadOnly: true,
    name: '',
    nameValidationState: { type: 'notValidated' },
    sortOrder: printConfigs.value.length === 0 ? 1 : Math.max(...printConfigs.value.map(v => v.sortOrder)) + 1,
    cupsCommandLineArgs: '',
    cupsCommandLineArgsValidationState: { type: 'notValidated' },
    reorderPagesAsBooklet: false,
    isSaving: false,
    hasSavingFailed: false,
    links: {
      self: props.printConfigsUrl
    }
  })
}

const modifiedPrintConfigs = computed(() => {
  if (printConfigs.value === undefined) return []

  return printConfigs.value.filter(v => {
    const currentData : PrintConfigInputs = {
      key: v.key,
      name: v.name,
      cupsCommandLineArgs: v.cupsCommandLineArgs,
      reorderPagesAsBooklet: v.reorderPagesAsBooklet
    }
    return !_.isEqual(v.loadedData, currentData)
  })
})

const savePrintConfig = async (printConfig: EditablePrintConfig) => {
  const httpMethod = printConfig.isNew ? 'POST' : 'PATCH'
  const data = {
    ...(printConfig.isNew ? { key: printConfig.key } : {}),
    name: printConfig.name,
    sortOrder: printConfig.sortOrder,
    cupsCommandLineArgs: printConfig.cupsCommandLineArgs,
    reorderPagesAsBooklet: printConfig.reorderPagesAsBooklet,
  }
  const result = await uiFetchAuthorized(
    toRef(printConfig, 'isSaving'),
    toRef(printConfig, 'hasSavingFailed'),
    printConfig.links.self,
    {
      method: httpMethod,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    }
  )
  if (result.succeeded) {
    if (printConfig.isNew) {
      const response = await result.response.json() as PrintConfigDto
      printConfig.isNew = false
      printConfig.key = response.key
      printConfig.keyIsReadOnly = true
      printConfig.name = response.name
      printConfig.links = { ...response.links }
      printConfig.loadedData = {
        key: response.key,
        name: response.name,
        cupsCommandLineArgs: response.cupsCommandLineArgs,
        reorderPagesAsBooklet: response.reorderPagesAsBooklet
      }
    }
    else {
      printConfig.loadedData = {
        key: printConfig.key,
        name: printConfig.name,
        cupsCommandLineArgs: printConfig.cupsCommandLineArgs,
        reorderPagesAsBooklet: printConfig.reorderPagesAsBooklet
      }
    }
  }
  else if (result.response !== undefined) {
    const errors = await result.response.json() as PrintConfigSaveError[]
    printConfig.keyValidationState = errors.includes('InvalidKey') ? { type: 'error', error: 'Ungültiger Schlüssel. Bitte verwenden Sie nur Buchstaben, Ziffern und \'-\' bzw. \'_\'.' } : { type: 'success' }
    printConfig.nameValidationState = errors.includes('EmptyName') ? { type: 'error', error: 'Bitte geben Sie einen Namen ein.' } : { type: 'success' }
  }
}

const canSave = computed(() => modifiedPrintConfigs.value.length > 0)
const save = async () => {
  modifiedPrintConfigs.value.map(v => savePrintConfig(v))
}

defineExpose({ canSave, save })
</script>

<template>
  <h3 class="mt-2 text-xl small-caps">Druckeinstellungen</h3>
  <LoadingBar v-if="isLoadingPrintConfigs" />
  <ErrorWithRetry v-else-if="hasLoadingPrintConfigsFailed" type="inline" @retry="loadPrintConfigs">Fehler beim Laden der Druckeinstellungen.</ErrorWithRetry>
  <div v-else-if="printConfigs !== undefined" class="mt-4 flex flex-wrap items-start gap-4">
    <PrintConfigItem v-for="printConfig in printConfigs" :key="printConfig.id"
      :printConfig="printConfig"
      v-model:name="printConfig.name"
      v-model:id="printConfig.key"
      v-model:id-is-read-only="printConfig.keyIsReadOnly"
      v-model:reorder-pages-as-booklet="printConfig.reorderPagesAsBooklet"
      v-model:cups-command-line-args="printConfig.cupsCommandLineArgs" />
    <button class="btn btn-green btn-solid self-center !px-8 !py-4" @click="addPrintConfig">Neue Druckeinstellung</button>
  </div>
</template>