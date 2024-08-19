<script setup lang="ts">
defineEmits(['retry'])

const props = withDefaults(
  defineProps<{
    retryTitle?: string
    type?: 'inline' | 'banner'
  }>(), {
    type: 'banner'
  }
)
const containerClasses = props.type === 'banner'
  ? [ "flex", "justify-center", "items-center", "gap-2", "bg-musi-red", "p-4", "rounded", "text-white" ]
  : [ "inline-flex", "justify-center", "items-center", "gap-2", "rounded", "text-musi-red" ]

const buttonClasses = props.type === 'banner'
  ? [ "btn", "text-white", "bg-white", "has-hover:hover:bg-white", "has-hover:hover:text-musi-red" ]
  : [ "btn", "text-musi-red", "bg-white", "has-hover:hover:bg-musi-red", "has-hover:hover:text-white" ]
</script>

<template>
  <div :class="containerClasses">
    <div class="font-semibold"><slot /></div>
    <button :class="buttonClasses" @click="$emit('retry')">{{ retryTitle ?? 'Erneut versuchen' }}</button>
  </div>
</template>
