<script setup lang="ts">
import { ref } from 'vue'
import LoadingBar from './LoadingBar.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import CompositionList from './CompositionList.vue'
import CompositionForm from './CompositionForm.vue'
import uiFetch from './UIFetch'
import type { CompositionListItem } from './AdminTypes'

type CompositionData = {
  compositions: CompositionListItem[]
  links: {
    printSettings: string
    composition: string
    export: string
  }
}

const isLoading = ref(false)
const hasLoadingFailed = ref(false)
const compositionList = ref<CompositionData>()
const loadCompositions = async () => {
  const result = await uiFetch(isLoading, hasLoadingFailed, '/api/admin/compositions')
  if (result.succeeded) {
    compositionList.value = (await result.response.json() as CompositionData)
    compositionList.value.compositions.sort((a, b) => a.title.localeCompare(b.title))
  }
}
loadCompositions()

const isExportingCompositions = ref(false)
const hasExportingCompositionsFailed = ref(false)
const exportCompositions = async () => {
  const result = await uiFetch(isExportingCompositions, hasExportingCompositionsFailed, '/api/admin/compositions/export')
  if (result.succeeded) {
    const blob = await result.response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'Notenarchiv.zip'
    a.click()
  }
}

type EditComposition = {
  type: 'create' | 'edit'
  printSettingsUrl: string
}
const editComposition = ref<EditComposition>()

const createComposition = () => {
  if (!compositionList.value) return

  editComposition.value = {
    type: 'create',
    printSettingsUrl: compositionList.value.links.printSettings
  }
}

const compositionSaved = async (oldComposition: CompositionListItem | undefined, newComposition: CompositionListItem) => {
  if (!compositionList.value || !editComposition.value) return

  editComposition.value = undefined
  const compositions = compositionList.value.compositions.filter(v => v !== oldComposition)
  compositions.push(newComposition)
  compositions.sort((a, b) => a.title.localeCompare(b.title))
  compositionList.value.compositions = compositions
}
</script>

<template>
  <h1 class="text-3xl p-8 bg-musi-gold text-white small-caps">
    <font-awesome-icon class="mr-2" :icon="['fas', 'music']" />
    <span>MusiScore - Administration</span>
  </h1>
  <div class="grow overflow-y-auto m-4">
    <LoadingBar v-if="isLoading"></LoadingBar>
    <ErrorWithRetry v-else-if="hasLoadingFailed" @retry="loadCompositions">Fehler beim Laden.</ErrorWithRetry>
    <CompositionList v-else-if="compositionList !== undefined" :compositions="compositionList?.compositions" />
    <CompositionForm v-else-if="editComposition" :type="editComposition.type" :print-settings-url="editComposition.printSettingsUrl" @saved="compositionSaved" />
  </div>
  <div id="command-bar" class="basis-auto grow-0 shrink-0 flex justify-end m-4 gap-4">
    <button v-if="compositionList !== undefined && compositionList.compositions.length > 0" class="btn btn-solid btn-gold !px-8 !py-4" @click="exportCompositions">Exportieren</button>
    <button v-if="editComposition === undefined" class="btn btn-solid btn-gold !px-8 !py-4" @click="createComposition">Neues Stück hinzufügen</button>
  </div>
</template>
