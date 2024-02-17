<script setup lang="ts">
import type { ValidationState } from './Validation'

const model = defineModel<File>()

const selectedFileChanged = (e: Event) => {
  model.value = (e.target as HTMLInputElement)?.files?.[0]
}

defineProps<{
  title: string,
  validationState: ValidationState
}>()
</script>

<template>
  <div>
    <label class="input">
      <span class="input-label">{{ title }}</span>
      <input class="input-file hidden" type="file" accept=".pdf" required @change="selectedFileChanged" />
      <div class="flex gap-4 items-center">
        <div class="btn btn-blue">Auswählen</div>
      </div>
      <span v-if="validationState.type === 'error'" class="text-sm text-musi-red">{{ validationState.error }}</span>
    </label>
  </div>
</template>
