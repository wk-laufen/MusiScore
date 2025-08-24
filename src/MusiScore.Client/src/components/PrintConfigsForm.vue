<script setup lang="ts">
import { computed, ref, toRef } from 'vue'
import type { EditablePrintConfig, PrintConfigDeleteError, PrintConfigDto, PrintConfigInputs, PrintConfigSaveError } from './PrintConfigTypes'
import { uiFetchAuthorized } from './UIFetch'
import _ from 'lodash'
import PrintConfigItem from './PrintConfigItem.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import LoadingBar from './LoadingBar.vue'
import draggable from 'vuedraggable'

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
      loadedData: { key: v.key, name: v.name, cupsCommandLineArgs: v.cupsCommandLineArgs, reorderPagesAsBooklet: v.reorderPagesAsBooklet, sortOrder: v.sortOrder, delete: false },
      ...v,
      id: `${nextPrintConfigId++}`,
      isNew: false,
      keyValidationState: { type: 'success' },
      keyIsReadOnly: true,
      nameValidationState: { type: 'success' },
      cupsCommandLineArgsValidationState: { type: 'success' },
      delete: false,
      replacementConfigId: "",
      replacementConfigIdValidationState: { type: 'notValidated' },
      isSaving: false,
      hasSavingFailed: false
    }))
  }
}
loadPrintConfigs()

const updateSortOrder = () => {
  if (printConfigs.value === undefined) return

  let sortOrder = 1
  for (const printConfig of printConfigs.value) {
    if (printConfig.delete) continue
    printConfig.sortOrder = sortOrder
    sortOrder++
  }
}

const toggleDeletePrintConfig = (printConfig: EditablePrintConfig) => {
  if (printConfigs.value === undefined) return

  if (printConfig.isNew) {
    printConfigs.value.splice(printConfigs.value.indexOf(printConfig), 1)
    updateSortOrder()
  }
  else {
    printConfig.delete = !printConfig.delete
    updateSortOrder()
  }
}

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
    delete: false,
    replacementConfigId: "",
    replacementConfigIdValidationState: { type: 'notValidated' },
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
      reorderPagesAsBooklet: v.reorderPagesAsBooklet,
      sortOrder: v.sortOrder,
      delete: v.delete
    }
    return !_.isEqual(v.loadedData, currentData)
  })
})

const handleSaveErrors = (printConfig: EditablePrintConfig, errors: PrintConfigSaveError[]) => {
  printConfig.keyValidationState = errors.includes('InvalidKey') ? { type: 'error', error: 'Ungültiger Schlüssel. Bitte verwenden Sie nur Buchstaben, Ziffern und \'-\' bzw. \'_\'.' } : { type: 'success' }
  printConfig.nameValidationState = errors.includes('EmptyName') ? { type: 'error', error: 'Bitte geben Sie einen Namen ein.' } : { type: 'success' }
}

const createPrintConfig = async (printConfig: EditablePrintConfig) => {
  const data = {
    key: printConfig.key,
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
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    }
  )
  if (result.succeeded) {
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
      reorderPagesAsBooklet: response.reorderPagesAsBooklet,
      sortOrder: response.sortOrder,
      delete: false
    }
  }
  else if (result.response !== undefined) {
    const errors = await result.response.json() as PrintConfigSaveError[]
    handleSaveErrors(printConfig, errors)
  }
}

const deletePrintConfig = async (printConfig: EditablePrintConfig) => {
  if (printConfigs.value === undefined) return

  const result = await uiFetchAuthorized(
    toRef(printConfig, 'isSaving'),
    toRef(printConfig, 'hasSavingFailed'),
    printConfig.links.self,
    {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        replacementConfigId: printConfig.replacementConfigId
      })
    }
  )
  if (result.succeeded) {
    printConfigs.value.splice(printConfigs.value.indexOf(printConfig), 1)
  }
  else if (result.response !== undefined) {
    const errors = await result.response.json() as PrintConfigDeleteError[]
    printConfig.replacementConfigIdValidationState = errors.includes('InvalidReplacementConfigId') ? { type: 'error', error: 'Bitte wählen Sie eine Einstellung, die stattdessen verwendet werden soll'} : { 'type': 'success' }
  }
}

const updatePrintConfig = async (printConfig: EditablePrintConfig) => {
  const data = {
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
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    }
  )
  if (result.succeeded) {
    printConfig.loadedData = {
      key: printConfig.key,
      name: printConfig.name,
      cupsCommandLineArgs: printConfig.cupsCommandLineArgs,
      reorderPagesAsBooklet: printConfig.reorderPagesAsBooklet,
      sortOrder: printConfig.sortOrder,
      delete: false
    }
  }
  else if (result.response !== undefined) {
    const errors = await result.response.json() as PrintConfigSaveError[]
    handleSaveErrors(printConfig, errors)
  }
}

const savePrintConfig = async (printConfig: EditablePrintConfig) => {
  if (printConfig.isNew) {
    await createPrintConfig(printConfig)
  }
  else if (printConfig.delete) {
    await deletePrintConfig(printConfig)
  }
  else {
    await updatePrintConfig(printConfig)
  }
}

const canSave = computed(() => modifiedPrintConfigs.value.length > 0)
const save = async () => {
  if (!canSave.value) return

  await Promise.all(modifiedPrintConfigs.value.map(v => savePrintConfig(v)))
}

defineExpose({ canSave, save })
</script>

<template>
  <h3 class="mt-2 text-xl small-caps">Druckeinstellungen</h3>
  <LoadingBar v-if="isLoadingPrintConfigs" />
  <ErrorWithRetry v-else-if="hasLoadingPrintConfigsFailed" type="inline" @retry="loadPrintConfigs">Fehler beim Laden der Druckeinstellungen.</ErrorWithRetry>
  <div v-else-if="printConfigs !== undefined">
    <draggable v-model="printConfigs" item-key="id" animation="150" filter="input[type=text]" :preventOnFilter="false" class="mt-4 flex flex-wrap items-start gap-4" @end="updateSortOrder">
      <template #item="{ element: printConfig } : { element: EditablePrintConfig }">
        <PrintConfigItem
          :printConfig="printConfig"
          :replacement-configs="printConfigs.filter(v => v !== printConfig && !v.delete)"
          v-model:name="printConfig.name"
          v-model:id="printConfig.key"
          v-model:id-is-read-only="printConfig.keyIsReadOnly"
          v-model:reorder-pages-as-booklet="printConfig.reorderPagesAsBooklet"
          v-model:cups-command-line-args="printConfig.cupsCommandLineArgs"
          v-model:replacement-config-id="printConfig.replacementConfigId"
          @delete="toggleDeletePrintConfig(printConfig)" />
      </template>
      <template #footer>
        <button class="btn btn-green btn-solid self-center px-8! py-4!" @click="addPrintConfig">Neue Druckeinstellung</button>
      </template>
    </draggable>
  </div>
</template>