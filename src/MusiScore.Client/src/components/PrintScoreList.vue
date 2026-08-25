<script setup lang="ts">
import { computed, ref } from 'vue'
import LoadingBar from './LoadingBar.vue'
import InfoNotification from './InfoNotification.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import LoadButton from './LoadButton.vue'
import HorizontalDivider from './HorizontalDivider.vue'
import { uiFetch } from './UIFetch'
import type { CompositionDto, CompositionList } from './PrintTypes'

const isLoading = ref(false)
const hasLoadingFailed = ref(false)
const compositionList = ref<CompositionList>()
const loadCompositions = async () => {
  const result = await uiFetch(isLoading, hasLoadingFailed, '/api/print/compositions')
  if (result.succeeded) {
    compositionList.value = (await result.response.json() as CompositionList)
    compositionList.value.compositions.sort((a, b) => a.title.localeCompare(b.title))
  }
}
loadCompositions()

const voiceGroups = computed(() => {
  if (compositionList.value === undefined) return undefined

  const compositions = compositionList.value.compositions

  const compositionHasVoice = (composition: CompositionDto, voiceName: string) =>
    composition.voices.some(v => v.name === voiceName)
  const isActiveVoice = (voiceName: string) =>
    compositions.some(c => compositionHasVoice(c, voiceName))

  return compositionList.value.voices.flatMap(voiceGroup => {
    const activeVoices = voiceGroup.voices.filter(v => isActiveVoice(v))
    if (activeVoices.length === 0) return []
    return [ { groupName: voiceGroup.groupName, voices: activeVoices } ]
  })
})

const voiceGroupTabs = computed(() =>
  [
    { key: null, value: 'Alle' },
    ...voiceGroups.value?.map(v => ({ key: v.groupName, value: v.groupName })) ?? []
  ]
)

const selectedComposition = ref<CompositionDto | null>(null)
const isCompositionSelectable = (composition: CompositionDto) => {
  if (selectedVoiceName.value === null) return true
  return composition.voices.findIndex(v => v.name === selectedVoiceName.value) !== -1
}

const trySelectComposition = (composition: CompositionDto) => {
  if (selectedComposition.value === composition) selectedComposition.value = null
  else if (isCompositionSelectable(composition)) { selectedComposition.value = composition }
}

const selectedVoiceName = ref<string | null>(null)
const isVoiceNameSelectable = (voiceName: string) => {
  if (selectedComposition.value === null) return true
  return selectedComposition.value.voices.findIndex(v => v.name === voiceName) !== -1
}

const trySelectVoiceName = (voiceName: string) => {
  if (selectedVoiceName.value === voiceName) selectedVoiceName.value = null
  else if (isVoiceNameSelectable(voiceName)) { selectedVoiceName.value = voiceName }
}

const selectedVoiceGroupName = ref<string | null>(null)

const visibleVoiceGroups = computed(() =>
  selectedVoiceGroupName.value === null
    ? voiceGroups.value
    : voiceGroups.value?.filter(v => v.groupName === selectedVoiceGroupName.value)
)

const selectVoiceGroup = (groupName: string | null) => {
  selectedVoiceGroupName.value = selectedVoiceGroupName.value === groupName ? null : groupName
}

const tryGetPrintUrl = () => {
  if (selectedComposition.value === null || selectedVoiceName.value === null) return undefined
  return selectedComposition.value.voices.find(v => v.name === selectedVoiceName.value)?.printUrl
}
const canPrint = () => {
  return tryGetPrintUrl() !== undefined
}
const isPrinting = ref(false)
const hasPrintingFailed = ref(false)
const tryPrint = async () => {
  const printUrl = tryGetPrintUrl()
  if (printUrl === undefined) return
  await uiFetch(isPrinting, hasPrintingFailed, printUrl, { method: 'POST' })
}
</script>

<template>
  <h1 class="text-3xl p-8 bg-musi-gold text-white small-caps">
    <font-awesome-icon class="mr-2" :icon="['fas', 'music']" />
    <span>MusiScore</span>
  </h1>
  <div class="grow overflow-y-auto m-4">
    <LoadingBar v-if="isLoading"></LoadingBar>
    <ErrorWithRetry v-else-if="hasLoadingFailed" @retry="loadCompositions">Fehler beim Laden.</ErrorWithRetry>
    <div v-else-if="compositionList !== undefined" class="flex flex-col items-stretch md:m-4">
      <InfoNotification v-if="compositionList.compositions.length === 0">Keine Stücke vorhanden.</InfoNotification>
      <template v-else>
        <HorizontalDivider>Stück</HorizontalDivider>
        <div class="flex flex-wrap items-stretch justify-center gap-2 m-4">
          <div v-for="composition in compositionList.compositions" :key="composition.title"
            @click="() => trySelectComposition(composition)"
            :class="{ 'bg-blue-500 text-white': selectedComposition === composition, 'opacity-50 cursor-not-allowed!': !isCompositionSelectable(composition) }"
            class="flex items-stretch border rounded-sm font-semibold text-blue-700 border-blue-500 divide-x divide-blue-500 cursor-pointer">
            <span class="grow flex items-center justify-center text-center p-4 md:p-8 md:w-60">{{ composition.title }}</span>
          </div>
        </div>
        <HorizontalDivider>Stimme</HorizontalDivider>
        <ul class="nav-container justify-center">
          <li v-for="voiceGroup in voiceGroupTabs" :key="voiceGroup.key || ''">
            <a class="nav-item" :class="{ active: selectedVoiceGroupName === voiceGroup.key }"
              @click="selectVoiceGroup(voiceGroup.key)">{{ voiceGroup.value }}</a>
          </li>
        </ul>
        <div v-for="voiceGroup in visibleVoiceGroups" :key="voiceGroup.groupName" class="flex flex-wrap items-stretch justify-center gap-2 m-4">
          <div v-for="voiceName in voiceGroup.voices" :key="voiceName"
            @click="() => trySelectVoiceName(voiceName)"
            :class="{ 'bg-blue-500 text-white': selectedVoiceName === voiceName, 'opacity-10 cursor-not-allowed!': !isVoiceNameSelectable(voiceName) }"
            class="flex items-stretch border rounded-sm font-semibold text-blue-700 border-blue-500 divide-x divide-blue-500 cursor-pointer">
            <span class="grow flex items-center justify-center text-center p-4 md:p-8 md:w-60">{{ voiceName }}</span>
          </div>
        </div>
      </template>
    </div>
  </div>
  <div v-if="compositionList !== undefined && compositionList.compositions.length > 0"
    class="basis-auto grow-0 shrink-0 border-t flex justify-center p-4">
    <LoadButton v-if="isPrinting"
      :loading="true"
      class="btn-solid btn-gold w-60 h-20 md:h-24">&nbsp;</LoadButton>
    <button v-else @click="() => tryPrint()"
      :class="{ 'opacity-50 cursor-not-allowed!': !canPrint(), 'animate-wiggle': hasPrintingFailed }"
      class="btn btn-solid btn-gold w-full sm:w-60 h-20 md:h-24">
      <span v-if="hasPrintingFailed">Fehler beim Drucken.<br />Nochmal versuchen</span>
      <span v-else>Drucken</span>
    </button>
  </div>
</template>
