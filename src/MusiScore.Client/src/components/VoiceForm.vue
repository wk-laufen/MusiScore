<script setup lang="ts">
import { ref, useId, watch } from 'vue'
import type { VoiceDefinition } from './AdminTypes'

const props = defineProps<{
  voices: VoiceDefinition[]
}>()

const suggestionsElementId = useId()

export type SelectedVoice = { type: 'existing', value: VoiceDefinition['name'] } | { type: 'new', value: string }
const newVoiceName = ref('')
const selectedVoice = defineModel<SelectedVoice>({ default: { type: 'existing', value: '' }})
watch(newVoiceName, voiceName => {
  const voiceDefinition = props.voices.find(v => v.name === voiceName)
  if (voiceDefinition !== undefined) {
    selectedVoice.value = { type: 'existing', value: voiceDefinition.name }
  }
  else {
    selectedVoice.value = { type: 'new', value: voiceName }
  }
})
watch(selectedVoice, voice => {
  const voiceName : string = voice.type === 'new' ? voice.value : (props.voices.find(v => v.name === voice.value)?.name || '')
  newVoiceName.value = voiceName
}, { immediate: true })
</script>
<template>
  <input type="text" v-model="newVoiceName" required :list="suggestionsElementId" class="input-text" :class="{ 'bg-yellow-500/50': selectedVoice.type === 'new', 'bg-green-500/50': selectedVoice.type === 'existing' }" />
  <datalist :id="suggestionsElementId">
    <option v-for="voice in voices" :key="voice.name" :value="voice.name">{{ voice.name }}</option>
  </datalist>
</template>