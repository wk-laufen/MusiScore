<script setup lang="ts">
import type { ValidationState } from './Validation'

const model = defineModel()

defineProps<{
  title: string,
  validationState: ValidationState
}>()
</script>

<template>
  <div>
    <label class="input">
      <span class="input-label">{{ title }}</span>
      <input class="input-text" type="text" required v-model="model" />
      <span v-if="validationState.type === 'error'" class="text-sm text-musi-red">{{ validationState.error }}</span>
    </label>
  </div>
</template>
