<script setup lang="ts">
import { computed, ref } from 'vue'
import LoadingBar from './LoadingBar.vue'
import InfoNotification from './InfoNotification.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import LoadButton from './LoadButton.vue'
import HorizontalDivider from './HorizontalDivider.vue'
import { uiFetch } from './UIFetch'
import type { ActiveCompositionDto } from './PrintTypes'
import _ from 'lodash'

const isLoading = ref(false)
const hasLoadingFailed = ref(false)
const compositionList = ref<ActiveCompositionDto[]>()
const loadCompositions = async () => {
  const result = await uiFetch(isLoading, hasLoadingFailed, '/api/print/compositions')
  if (result.succeeded) {
    compositionList.value = (await result.response.json() as ActiveCompositionDto[])
    compositionList.value.sort((a, b) => a.title.localeCompare(b.title))
  }
}
loadCompositions()

const voiceNames = computed(() =>
{
  if (compositionList.value === undefined) return undefined

  return _(compositionList.value)
    .flatMap(v => v.voices)
    .uniqBy(v => v.name)
    .sortBy(v => v.globalSortOrder, v => v.name)
    .map(v => v.name)
    .value()
})

const selectedComposition = ref<ActiveCompositionDto>()
const isCompositionSelectable = (composition: ActiveCompositionDto) => {
  if (selectedVoiceName.value === undefined) return true
  return composition.voices.findIndex(v => v.name === selectedVoiceName.value) !== -1
}

const trySelectComposition = (composition: ActiveCompositionDto) => {
  if (selectedComposition.value === composition) selectedComposition.value = undefined
  else if (isCompositionSelectable(composition)) { selectedComposition.value = composition }
}

const selectedVoiceName = ref<string>()
const isVoiceNameSelectable = (voiceName: string) => {
  if (selectedComposition.value === undefined) return true
  return selectedComposition.value.voices.findIndex(v => v.name === voiceName) !== -1
}

const trySelectVoiceName = (voiceName: string) => {
  if (selectedVoiceName.value === voiceName) selectedVoiceName.value = undefined
  else if (isVoiceNameSelectable(voiceName)) { selectedVoiceName.value = voiceName }
}

const tryGetPrintUrl = () => {
  if (selectedComposition.value === undefined || selectedVoiceName.value === undefined) return undefined
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
      <InfoNotification v-if="compositionList.length === 0">Keine Stücke vorhanden.</InfoNotification>
      <template v-else>
        <HorizontalDivider>Stimme</HorizontalDivider>
        <div class="flex flex-wrap items-stretch justify-center gap-2 m-4">
          <div v-for="voiceName in voiceNames" :key="voiceName"
            @click="() => trySelectVoiceName(voiceName)"
            :class="{ 'bg-blue-500 text-white': selectedVoiceName === voiceName, 'opacity-50 cursor-not-allowed!': !isVoiceNameSelectable(voiceName) }"
            class="flex items-stretch border rounded-sm font-semibold text-blue-700 border-blue-500 divide-x divide-blue-500 cursor-pointer">
            <span class="grow flex items-center justify-center text-center p-4 md:p-8 md:w-60">{{ voiceName }}</span>
          </div>
        </div>
        <HorizontalDivider>Stück</HorizontalDivider>
        <div class="flex flex-wrap items-stretch justify-center gap-2 m-4">
          <div v-for="composition in compositionList" :key="composition.title"
            @click="() => trySelectComposition(composition)"
            :class="{ 'bg-blue-500 text-white': selectedComposition === composition, 'opacity-50 cursor-not-allowed!': !isCompositionSelectable(composition) }"
            class="flex items-stretch border rounded-sm font-semibold text-blue-700 border-blue-500 divide-x divide-blue-500 cursor-pointer">
            <span class="grow flex items-center justify-center text-center p-4 md:p-8 md:w-60">{{ composition.title }}</span>
          </div>
        </div>
        <div class="flex justify-center my-8">
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
    </div>
  </div>
</template>
