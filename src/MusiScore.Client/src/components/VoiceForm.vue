<script setup lang="ts">
import { computed, useId } from 'vue'
import type { VoiceDefinition } from './AdminTypes'

const props = defineProps<{
  voices: VoiceDefinition[]
  label?: string
}>()

const inputId = useId()

const suggestionsElementId = useId()

const voiceName = defineModel({ default: '' })
const voiceNameType = computed(() => props.voices.some(v => v.name === voiceName.value) ? 'existing' : 'new')
</script>
<template>
  <div class="input">
    <label v-if="label !== undefined" :for="inputId" class="input-label">{{ label }}</label>
    <input :id="inputId" type="text" v-model="voiceName" required :list="suggestionsElementId" class="input-text" :class="{ 'bg-yellow-500/50': voiceNameType === 'new', 'bg-green-500/50': voiceNameType === 'existing' }" />
    <datalist :id="suggestionsElementId">
      <option v-for="voice in voices" :key="voice.name" :value="voice.name">{{ voice.name }}</option>
    </datalist>
  </div>
</template>