<script setup lang="ts">
import { computed, ref } from 'vue'
import { uiFetchAuthorized } from './UIFetch'
import ErrorWithRetry from './ErrorWithRetry.vue'
import LoadingBar from './LoadingBar.vue'
import _ from 'lodash'

const props = defineProps<{
  voiceSettingsUrl: string
}>()

type VoiceSettingsDto = {
  sortOrderPatterns: string[]
}
type VoiceSettingsSaveError =
  { type: 'InvalidPattern', lineIndex: number } |
  { type: 'EmptyPattern', lineIndex: number }
type UIVoiceSettingsSaveError =
  { type: 'InvalidPattern', lineIndex: number, pattern: string } |
  { type: 'EmptyPattern', lineIndex: number, pattern: string }
type EditableVoiceSettings = {
  loadedData: VoiceSettingsDto
  sortOrderPatterns: string
  sortOrderPatternsSaveErrors: UIVoiceSettingsSaveError[]
}

const voiceSettings = ref<EditableVoiceSettings>()
const isLoadingVoiceSettings = ref(false)
const hasLoadingVoiceSettingsFailed = ref(false)
const loadVoiceSettings = async () => {
  const result = await uiFetchAuthorized(isLoadingVoiceSettings, hasLoadingVoiceSettingsFailed, props.voiceSettingsUrl)
  if (result.succeeded) {
    const loadedVoiceSettings = await result.response.json() as VoiceSettingsDto
    voiceSettings.value = {
      loadedData: loadedVoiceSettings,
      sortOrderPatterns: loadedVoiceSettings.sortOrderPatterns.join('\n'),
      sortOrderPatternsSaveErrors: []
    }
  }
}
loadVoiceSettings()

const addMatchAllSortOrderPattern = () => {
  if (voiceSettings.value === undefined) return

  if (voiceSettings.value.sortOrderPatterns.trimEnd().endsWith('\n.*')) return

  voiceSettings.value.sortOrderPatterns += '\n.*'
}

const canSave = computed(() => {
  if (voiceSettings.value === undefined) return false

  return !_.isEqual({ sortOrderPatterns: voiceSettings.value.loadedData.sortOrderPatterns }, { sortOrderPatterns: voiceSettings.value.sortOrderPatterns.split('\n') })
})

const isSaving = ref(false)
const hasSavingFailed = ref(false)
const save = async () => {
  if (voiceSettings.value === undefined) return
  if (!canSave.value) return

  const voiceSettingsToSave = voiceSettings.value
  voiceSettingsToSave.sortOrderPatternsSaveErrors = []
  const result = await uiFetchAuthorized(isSaving, hasSavingFailed, props.voiceSettingsUrl, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      sortOrderPatterns: voiceSettingsToSave.sortOrderPatterns.split('\n')
    })
  })
  if (result.succeeded) {
    const loadedVoiceSettings = await result.response.json() as VoiceSettingsDto
    voiceSettings.value = {
      loadedData: loadedVoiceSettings,
      sortOrderPatterns: loadedVoiceSettings.sortOrderPatterns.join('\n'),
      sortOrderPatternsSaveErrors: []
    }
  }
  else if (result.response !== undefined) {
    const errors = await result.response.json() as VoiceSettingsSaveError[]
    voiceSettings.value.sortOrderPatternsSaveErrors = errors.map(v => {
      switch (v.type) {
        case 'InvalidPattern':
          return { ...v, pattern: voiceSettingsToSave.sortOrderPatterns.split('\n')[v.lineIndex] }
        case 'EmptyPattern':
          return { ...v, pattern: voiceSettingsToSave.sortOrderPatterns.split('\n')[v.lineIndex] }
      }
    })
  }
}

defineExpose({ canSave, save })
</script>

<template>
  <h3 class="mt-2 text-xl small-caps">Stimmeneinstellungen</h3>
  <LoadingBar v-if="isLoadingVoiceSettings" />
  <ErrorWithRetry v-else-if="hasLoadingVoiceSettingsFailed" type="inline" @retry="loadVoiceSettings">Fehler beim Laden der Stimmeneinstellungen.</ErrorWithRetry>
  <div v-else-if="voiceSettings !== undefined" class="flex flex-col gap-2 mt-2">
    <span v-if="hasSavingFailed" class="text-sm text-musi-red">Fehler beim Speichern der Stimmeneinstellungen.</span>
    <label class="input">
      <span class="input-label">Sortierreihenfolge</span>
      <textarea class="input-textarea min-h-96" :disabled="isSaving" v-model="voiceSettings.sortOrderPatterns"></textarea>
    </label>
    <span class="text-sm text-slate-700">Tipp: Um alle Stimmen beim Druck anzuzeigen, füge <a class="px-3 py-2 border rounded font-bold text-musi-blue cursor-pointer" @click="addMatchAllSortOrderPattern">.*</a> am Ende ein.</span>
    <ul v-if="voiceSettings.sortOrderPatternsSaveErrors.length > 0">
      <li v-for="error in voiceSettings.sortOrderPatternsSaveErrors" :key="JSON.stringify(error)">
        <span v-if="error.type === 'InvalidPattern'" class="text-sm text-musi-red">Zeile {{error.lineIndex + 1}}: Ungültiges Muster <span class="px-2 py-1 border rounded text-slate-700">{{ error.pattern }}</span></span>
        <span v-else-if="error.type === 'EmptyPattern'" class="text-sm text-musi-red">Zeile {{error.lineIndex + 1}}: Muster darf nicht leer sein</span>
      </li>
    </ul>
  </div>
</template>