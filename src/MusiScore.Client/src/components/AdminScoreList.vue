<script setup lang="ts">
import { computed, ref } from 'vue'
import LoadingBar from './LoadingBar.vue'
import LoadButton from './LoadButton.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import LoginForm from './LoginForm.vue'
import CompositionList from './CompositionList.vue'
import CompositionForm from './CompositionForm.vue'
import ImportCompositionForm from './ImportCompositionForm.vue'
import SettingsForm from './SettingsForm.vue'
import { uiFetchAuthorized } from './UIFetch'
import type { CompositionListItem } from './AdminTypes'
import { useAPIKeyStore } from '@/stores/api-key'

type CompositionData = {
  compositions: CompositionListItem[]
  links: {
    printConfigs: string
    inferPrintConfig: string
    testPrintConfig: string
    composition: string
    compositionTemplate: string
    export: string
    voiceDefinitions: string
  }
}

const showLogin = ref(false)
const apiKeyStore = useAPIKeyStore()
const login = async (apiKey: string, remember: boolean) => {
  apiKeyStore.update(apiKey, remember)
  showLogin.value = false
  await loadCompositions()
}

const isLoading = ref(false)
const hasLoadingFailed = ref(false)
const compositionList = ref<CompositionData>()
const loadCompositions = async () => {
  compositionList.value = undefined
  const result = await uiFetchAuthorized(isLoading, hasLoadingFailed, '/api/admin/compositions')
  if (result.succeeded) {
    compositionList.value = (await result.response.json() as CompositionData)
    compositionList.value.compositions.sort((a, b) => a.title.localeCompare(b.title))
  }
  else if (result.response?.status === 401) {
    showLogin.value = true
  }
}
loadCompositions()

const isEditingSettings = ref(false)

const isImportingCompositions = ref(false)

const isExportingCompositions = ref(false)
const hasExportingCompositionsFailed = ref(false)
const exportCompositions = async () => {
  if (compositionList.value === undefined) return

  const result = await uiFetchAuthorized(isExportingCompositions, hasExportingCompositionsFailed, compositionList.value.links.export)
  if (result.succeeded) {
    const blob = await result.response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'Notenarchiv.zip'
    a.click()
  }
}

const isInListView = computed(() => editComposition.value === undefined && !isImportingCompositions.value && !isEditingSettings.value)

type EditComposition = {
  type: 'create' | 'edit'
  compositionUrl: string
  templateUrl: string
}
const editComposition = ref<EditComposition>()

const createComposition = () => {
  if (!compositionList.value) return

  editComposition.value = {
    type: 'create',
    compositionUrl: compositionList.value.links.composition,
    templateUrl: compositionList.value.links.compositionTemplate
  }
}

const startEditComposition = (composition: CompositionListItem) => {
  if (!compositionList.value) return

  editComposition.value = {
    type: 'edit',
    compositionUrl: composition.links.self,
    templateUrl: compositionList.value.links.compositionTemplate
  }
}

const cancelEdit = async () => {
  editComposition.value = undefined
  await loadCompositions()
}

const cancelImport = async () => {
  isImportingCompositions.value = false
  await loadCompositions()
}

const cancelEditSettings = async () => {
  isEditingSettings.value = false
  await loadCompositions()
}

const compositionDeleted = (composition: CompositionListItem) => {
  if (compositionList.value === undefined) return

  compositionList.value.compositions = compositionList.value.compositions.filter(v => v !== composition)
}
</script>

<template>
  <h1 class="text-3xl p-8 bg-musi-gold text-white small-caps">
    <font-awesome-icon class="mr-2" :icon="['fas', 'music']" />
    <span>MusiScore - Administration</span>
  </h1>
  <div class="grow overflow-y-auto m-4">
    <LoginForm v-if="showLogin" :api-key="apiKeyStore.apiKey" @login="login" />
    <LoadingBar v-else-if="isLoading"></LoadingBar>
    <ErrorWithRetry v-else-if="hasLoadingFailed" @retry="loadCompositions">Fehler beim Laden.</ErrorWithRetry>
    <CompositionList v-else-if="compositionList !== undefined && isInListView" :compositions="compositionList.compositions"
      @edit="startEditComposition"
      @deleted="compositionDeleted" />
    <CompositionForm v-else-if="compositionList !== undefined && editComposition"
      :type="editComposition.type"
      :printConfigsUrl="compositionList.links.printConfigs"
      :testPrintConfigUrl="compositionList.links.testPrintConfig"
      :composition-url="editComposition.compositionUrl"
      :composition-template-url="editComposition.templateUrl"
      @cancel-edit="cancelEdit" />
    <ImportCompositionForm v-else-if="compositionList !== undefined && isImportingCompositions"
      :composition-url="compositionList.links.composition"
      :inferPrintConfigUrl="compositionList.links.inferPrintConfig"
      :voiceDefinitionsUrl="compositionList.links.voiceDefinitions"
      @cancel-import="cancelImport" />
    <SettingsForm v-else-if="compositionList !== undefined && isEditingSettings"
      :printConfigsUrl="compositionList.links.printConfigs"
      :voiceDefinitionsUrl="compositionList.links.voiceDefinitions"
      @cancel-edit-settings="cancelEditSettings" />
  </div>
  <div v-if="!showLogin" id="command-bar" class="basis-auto grow-0 shrink-0 border-t flex items-center p-4 gap-4">
    <button v-if="compositionList !== undefined && isInListView" class="btn btn-solid btn-gold px-8! py-4!" @click="isEditingSettings = true">Einstellungen</button>
    <button v-if="compositionList !== undefined && isInListView" class="btn btn-solid btn-gold px-8! py-4!" @click="isImportingCompositions = true">Importieren</button>
    <LoadButton v-if="compositionList !== undefined && compositionList?.compositions.length > 0 && isInListView"
      :loading="isExportingCompositions"
      :disabled="false"
      class="btn-solid btn-gold px-8! py-4!"
      @click="exportCompositions">
      <template v-if="hasExportingCompositionsFailed">Exportieren fehlgeschlagen. Erneut versuchen.</template>
      <template v-else>Exportieren</template>
    </LoadButton>
    <button v-if="compositionList !== undefined && isInListView" class="btn btn-solid btn-gold px-8! py-4!" @click="createComposition">Neues Stück hinzufügen</button>
  </div>
</template>
