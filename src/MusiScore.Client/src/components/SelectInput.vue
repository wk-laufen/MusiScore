<script setup lang="ts">
import type { ValidationState } from './Validation'

const model = defineModel()

defineProps<{
  title: string,
  validationState: ValidationState,
  options: { key: string, value: string }[]
}>()
</script>

<template>
  <div>
    <label class="input">
      <span class="input-label">{{ title }}</span>
      <select class="input-select" v-model="model">
        <option disabled value="">Bitte auswählen</option>
        <option v-for="option in options" :key="option.key" :value="option.key">{{ option.value }}</option>
      </select>
      <span v-if="validationState.type === 'error'" class="text-sm text-musi-red">{{ validationState.error }}</span>
    </label>
  </div>
</template>
