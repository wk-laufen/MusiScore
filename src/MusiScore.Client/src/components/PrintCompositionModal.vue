<script setup lang="ts">
import { ref, useTemplateRef, watch } from 'vue'
import LoadButton from './LoadButton.vue'
import LoadingBar from './LoadingBar.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import { type CompositionListItem, type VoicePrintSettings, type VoicePrintResult } from './AdminTypes'
import { uiFetchAuthorized } from './UIFetch'

const props = defineProps<{
  composition: CompositionListItem
}>()

const emit = defineEmits<{
  'close': []
}>()

type VoicePrintSettingsFormValues = {
  name: string
  count: number
  error: string | undefined
}

const popover = useTemplateRef('popover')
const isLoading = ref(false)
const hasLoadingFailed = ref(false)
const isPrinting = ref(false)
const hasPrintingFailed = ref(false)
const voices = ref<VoicePrintSettingsFormValues[]>([])

const loadVoices = async () => {
  const result = await uiFetchAuthorized(isLoading, hasLoadingFailed, props.composition.links.print, { method: 'GET' })
  if (result.succeeded) {
    const settings = await result.response.json() as VoicePrintSettings[]
    voices.value = settings.map(v => ({ name: v.name, count: v.count, error: undefined }))
  }
}
loadVoices()

watch(popover, el => {
  if (el === null) return

  el.addEventListener('toggle', (e: Event) => {
    if ((e as ToggleEvent).newState === 'closed') {
      emit('close')
    }
  })
  el.showPopover()
})

const close = () => {
  popover.value!.hidePopover()
}

const print = async () => {
  voices.value.forEach(v => { v.error = undefined })
  const result = await uiFetchAuthorized(isPrinting, hasPrintingFailed, props.composition.links.print, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(voices.value.map(v => ({ name: v.name, count: v.count }) satisfies VoicePrintSettings))
  })
  if (result.succeeded) {
    const voicePrintResults = await result.response.json() as VoicePrintResult[]
    let hasError = false
    for (const voice of voices.value) {
      const voicePrintResult = voicePrintResults.find(v => v.voiceName === voice.name)
      if (voicePrintResult === undefined || voicePrintResult.result === 'Success')
        voice.error = undefined
      else if (voicePrintResult.result === 'PrintConfigNotFound')
        voice.error = 'Druckkonfiguration nicht gefunden'
      else if (voicePrintResult.result === 'PrintingFailed')
        voice.error = 'Drucken fehlgeschlagen'
      if (voice.error !== undefined) hasError = true
    }
    if (hasError === false) {
      close()
    }
  }
}
</script>

<template>
  <div ref="popover" popover class="m-auto rounded-md border shadow-lg min-w-96 max-w-full max-h-[90vh] backdrop:bg-black/30 backdrop:backdrop-blur-[2px] divide-y flex flex-col">
    <div class="flex items-center justify-between">
      <span class="font-semibold ml-4">{{ composition.title }}</span>
      <button class="cursor-pointer ml-4 p-4" title="Schließen" @click="close">
        <font-awesome-icon :icon="['fas', 'xmark']" />
      </button>
    </div>

    <div class="p-4 overflow-auto">
      <LoadingBar v-if="isLoading" />
      <ErrorWithRetry v-else-if="hasLoadingFailed" type="inline" @retry="loadVoices">Stimmen konnten nicht geladen werden.</ErrorWithRetry>
      <p v-else-if="voices.length === 0" class="text-sm text-slate-500">Keine Stimmen vorhanden.</p>
      <div v-else class="flex flex-col text-sm divide-y">
        <div v-for="voice in voices" :key="voice.name" class="flex justify-between items-center gap-4 py-2">
          <div class="flex flex-col">
            <span>{{ voice.name }}</span>
            <span v-if="voice.error" class="text-musi-red text-xs">{{ voice.error }}</span>
          </div>
          <input class="input-text min-w-16! w-16"
            type="number"
            min="0"
            v-model="voice.count" />
        </div>
      </div>
    </div>

    <div class="flex justify-end gap-2 px-4 py-3">
      <button class="btn btn-blue" @click="close">Abbrechen</button>
      <LoadButton class="btn btn-blue btn-solid"
        :loading="isPrinting"
        :disabled="isLoading || hasLoadingFailed || voices.length === 0"
        @click="print">
        <div class="flex items-center gap-2">
          <span>Drucken</span>
          <font-awesome-icon v-if="hasPrintingFailed" class="text-musi-red" :icon="['fas', 'xmark']" />
        </div>
      </LoadButton>
    </div>
  </div>
</template>
