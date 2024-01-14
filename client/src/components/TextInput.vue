<script setup lang="ts">
const model = defineModel()

type ValidationState =
  | { type: "notValidated" }
  | { type: "error", error: string }
  | { type: "success", value: string }

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
      <span v-if="validationState.type === 'error'" class="text-sm text-pink-600">{{ validationState.error }}</span>
    </label>
  </div>
</template>
