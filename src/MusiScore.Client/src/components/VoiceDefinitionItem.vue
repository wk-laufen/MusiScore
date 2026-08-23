<script setup lang="ts">
import { computed, watch } from 'vue'
import SelectInput from './SelectInput.vue'
import type { EditableVoiceDefinition } from './VoiceDefinitionTypes'
import { joinStrings } from './UI'

const props = defineProps<{
  voiceDefinition: EditableVoiceDefinition
  /** every voice definition across all groups, so a deleted one can hand its voices to any other */
  allVoiceDefinitions: EditableVoiceDefinition[]
}>()

defineEmits<{
  'delete': []
}>()

const name = defineModel<string>('name', { required: true })
const memberCount = defineModel<number>('memberCount', { required: true })
const replacementId = defineModel<string>('replacementId', { required: true })

const usageTitle = computed(() =>
  `Verwendet in ${joinStrings(props.voiceDefinition.compositions.map(v => `"${v}"`))}`)

/** a voice definition can be replaced by any other one that already exists on the server and stays */
const replacementOptions = computed(() =>
  props.allVoiceDefinitions
    .filter(v => v !== props.voiceDefinition && !v.delete && v.serverId !== null)
    .map(v => ({ key: v.serverId as string, value: v.name })))

// the selected replacement can disappear, e.g. when it is marked for deletion itself
watch(replacementOptions, options => {
  if (replacementId.value !== '' && !options.some(v => v.key === replacementId.value)) {
    replacementId.value = ''
  }
}, { immediate: true })
</script>

<template>
  <div class="flex flex-col gap-2 rounded p-1">
    <div class="flex items-center gap-2">
      <button class="btn" :class="{ 'btn-solid btn-red': voiceDefinition.delete }"
        title="Stimme löschen"
        @click="$emit('delete')">
        <font-awesome-icon :icon="['fas', 'trash-can']" />
      </button>
      <div class="w-6 text-center" :class="voiceDefinition.delete ? 'opacity-50' : 'voice-handle cursor-grab'">
        <font-awesome-icon :icon="['fas', 'up-down-left-right']" />
      </div>
      <div class="flex items-center gap-2" :class="{ 'opacity-50': voiceDefinition.delete }">
        <input type="number" min="0" v-model="memberCount" class="input-text min-w-20! w-20" :disabled="voiceDefinition.delete || voiceDefinition.isSaving" />
        <font-awesome-icon :icon="['fas', 'xmark']" />
        <input type="text" v-model="name" required placeholder="Name" :disabled="voiceDefinition.delete || voiceDefinition.isSaving" class="input-text" />
      </div>
      <font-awesome-icon v-if="voiceDefinition.compositions.length > 0" :icon="['fas', 'info-circle']"
        class="text-musi-blue" :title="usageTitle" />
      <span v-if="voiceDefinition.saveErrors.length > 0" class="text-sm text-musi-red">{{ voiceDefinition.saveErrors.join(" ") }}</span>
      <span v-else-if="voiceDefinition.hasSavingFailed" class="text-sm text-musi-red">Fehler beim Speichern.</span>
    </div>
    <SelectInput v-if="voiceDefinition.delete && voiceDefinition.compositions.length > 0"
      title="Ersetzen durch"
      :options="replacementOptions"
      :validation-state="voiceDefinition.replacementIdValidationState"
      v-model="replacementId" />
  </div>
</template>
