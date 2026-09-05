<script setup lang="ts">
import { useId } from 'vue'
import type { VoiceDefinition } from './AdminTypes'

const props = defineProps<{
  voices: VoiceDefinition[]
  label?: string
}>()

const inputId = useId()
const suggestionsElementId = useId()

/** a voice can cover several voice definitions, e.g. one sheet for "Horn 1" and "Horn 2" */
const voiceNames = defineModel<string[]>({ default: () => [''] })

const setName = (index: number, name: string) => {
  voiceNames.value = voiceNames.value.map((v, i) => i === index ? name : v)
}
const addName = () => {
  voiceNames.value = [...voiceNames.value, '']
}
const removeName = (index: number) => {
  voiceNames.value = voiceNames.value.filter((_, i) => i !== index)
}

const isExistingVoice = (name: string) => props.voices.some(v => v.name === name)
</script>
<template>
  <div class="input">
    <label v-if="label !== undefined" :for="inputId" class="input-label">{{ label }}</label>
    <div class="flex flex-col gap-2">
      <div v-for="(voiceName, index) in voiceNames" :key="index" class="flex items-center gap-2">
        <input :id="index === 0 ? inputId : undefined" type="text"
          :value="voiceName" @input="setName(index, ($event.target as HTMLInputElement).value)"
          required :list="suggestionsElementId" class="input-text grow"
          :class="{ 'bg-yellow-500/50': !isExistingVoice(voiceName), 'bg-green-500/50': isExistingVoice(voiceName) }" />
        <button v-if="voiceNames.length > 1" class="btn btn-red" title="Stimmenname entfernen" @click="removeName(index)">
          <font-awesome-icon :icon="['fas', 'trash-can']" />
        </button>
      </div>
    </div>
    <button class="btn btn-green self-start mt-2" title="Weiteren Stimmennamen hinzufügen" @click="addName">
      <font-awesome-icon :icon="['fas', 'plus']" />
    </button>
    <datalist :id="suggestionsElementId">
      <option v-for="voice in voices" :key="voice.name" :value="voice.name">{{ voice.name }}</option>
    </datalist>
  </div>
</template>
