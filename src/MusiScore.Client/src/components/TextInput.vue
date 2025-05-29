<script setup lang="ts">
import type { ValidationState } from './Validation'

const model = defineModel()

withDefaults(defineProps<{
  title?: string,
  validationState: ValidationState,
  disabled?: boolean,
  placeholder?: string,
  required?: boolean
}>(), { disabled: false, required: true })
</script>

<template>
  <div>
    <label class="input">
      <span v-if="title !== undefined" class="input-label">{{ title }}</span>
      <div v-else-if="$slots.title" class="input-label"><slot name="title"></slot></div>
      <input class="input-text" type="text" :required="required" :disabled="disabled" :placeholder="placeholder" v-model="model" />
      <span v-if="validationState.type === 'error'" class="text-sm text-musi-red">{{ validationState.error }}</span>
    </label>
  </div>
</template>
