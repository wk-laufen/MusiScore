<script setup lang="ts">
import type { ValidationState } from './Validation'

const isActive = defineModel<boolean>('isActive')
const text = defineModel<string>('text')

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
    <label class="input w-min">
      <span v-if="title !== undefined" class="input-label">{{ title }}</span>
      <div v-else-if="$slots.title" class="input-label"><slot name="title"></slot></div>
      <div class="flex gap-2">
        <input type="checkbox" v-model="isActive" />
        <input class="input-text min-w-[16.6rem]!" type="text" :required="required" :disabled="disabled" :placeholder="placeholder" v-model="text" />
      </div>
      <span v-if="validationState.type === 'error'" class="text-sm text-musi-red">{{ validationState.error }}</span>
    </label>
  </div>
</template>
