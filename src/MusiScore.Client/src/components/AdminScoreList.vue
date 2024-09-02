<script setup lang="ts">
import { computed, ref } from 'vue'
import LoadingBar from './LoadingBar.vue'
import LoadButton from './LoadButton.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import LoginForm from './LoginForm.vue'
import CompositionList from './CompositionList.vue'
import CompositionForm from './CompositionForm.vue'
import ImportCompositionForm from './ImportCompositionForm.vue'
import { uiFetchAuthorized } from './UIFetch'
import type { CompositionListItem } from './AdminTypes'
import { useAPIKeyStore } from '@/stores/api-key'

type CompositionData = {
  compositions: CompositionListItem[]
  links: {
    printSettings: string
    inferPrintSetting: string
    testPrintSetting: string
    composition: string
    export: string
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

const isImportingCompositions = ref(false)

const isExportingCompositions = ref(false)
const hasExportingCompositionsFailed = ref(false)
const exportCompositions = async () => {
  const result = await uiFetchAuthorized(isExportingCompositions, hasExportingCompositionsFailed, '/api/admin/compositions/export')
  if (result.succeeded) {
    const blob = await result.response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'Notenarchiv.zip'
    a.click()
  }
}

const isInListView = computed(() => editComposition.value === undefined && !isImportingCompositions.value)

type EditComposition = {
  type: 'create' | 'edit'
  printSettingsUrl: string
  testPrintSettingUrl: string
  compositionUrl: string
}
const editComposition = ref<EditComposition>()

const createComposition = () => {
  if (!compositionList.value) return

  editComposition.value = {
    type: 'create',
    printSettingsUrl: compositionList.value.links.printSettings,
    testPrintSettingUrl: compositionList.value.links.testPrintSetting,
    compositionUrl: compositionList.value.links.composition
  }
}

const startEditComposition = (composition: CompositionListItem) => {
  if (!compositionList.value) return

  editComposition.value = {
    type: 'edit',
    printSettingsUrl: compositionList.value.links.printSettings,
    testPrintSettingUrl: compositionList.value.links.testPrintSetting,
    compositionUrl: composition.links.self
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
    <CompositionForm v-else-if="editComposition"
      :type="editComposition.type"
      :print-settings-url="editComposition.printSettingsUrl"
      :testPrintSettingUrl="editComposition.testPrintSettingUrl"
      :composition-url="editComposition.compositionUrl"
      @cancel-edit="cancelEdit" />
    <ImportCompositionForm v-else-if="compositionList !== undefined && isImportingCompositions"
      :composition-url="compositionList.links.composition"
      :inferPrintSettingUrl="compositionList.links.inferPrintSetting"
      @cancel-import="cancelImport" />
  </div>
  <div v-if="!showLogin" id="command-bar" class="basis-auto grow-0 shrink-0 border-t flex items-center p-4 gap-4">
    <button class="btn btn-solid btn-gold !px-8 !py-4">Einstellungen</button>
    <button v-if="compositionList !== undefined && isInListView" class="btn btn-solid btn-gold !px-8 !py-4" @click="isImportingCompositions = true">Importieren</button>
    <LoadButton v-if="compositionList !== undefined && compositionList?.compositions.length > 0 && isInListView"
      :loading="isExportingCompositions"
      :disabled="false"
      class="btn-solid btn-gold !px-8 !py-4"
      @click="exportCompositions">
      <template v-if="hasExportingCompositionsFailed">Exportieren fehlgeschlagen. Erneut versuchen.</template>
      <template v-else>Exportieren</template>
    </LoadButton>
    <button v-if="compositionList !== undefined && isInListView" class="btn btn-solid btn-gold !px-8 !py-4" @click="createComposition">Neues Stück hinzufügen</button>
  </div>
</template>
