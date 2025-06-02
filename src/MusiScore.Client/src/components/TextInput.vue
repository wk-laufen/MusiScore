<script setup lang="ts">
import { useId } from 'vue'
import type { ValidationState } from './Validation'

const model = defineModel()

const suggestionsElementId = useId()

withDefaults(defineProps<{
  title?: string
  validationState: ValidationState
  disabled?: boolean
  placeholder?: string
  required?: boolean
  suggestions?: string[]
}>(), { disabled: false, required: true, suggestions: () => [] })
</script>

<template>
  <div>
    <label class="input">
      <span v-if="title !== undefined" class="input-label">{{ title }}</span>
      <div v-else-if="$slots.title" class="input-label"><slot name="title"></slot></div>
      <input class="input-text" type="text" :required="required" :disabled="disabled" :placeholder="placeholder" :list="suggestionsElementId" v-model="model" />
      <datalist :id="suggestionsElementId">
        <option v-for="text in suggestions" :key="text" :value="text"></option>
      </datalist>
      <span v-if="validationState.type === 'error'" class="text-sm text-musi-red">{{ validationState.error }}</span>
    </label>
  </div>
</template>
