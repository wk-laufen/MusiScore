<script setup lang="ts">
defineEmits(['retry'])

withDefaults(
  defineProps<{
    retryTitle?: string
    type?: 'inline' | 'banner'
  }>(), {
    type: 'banner'
  }
)
</script>

<template>
  <div v-if="type === 'banner'" class="flex justify-center items-center gap-2 bg-musi-red p-4 rounded text-white">
    <div class="font-semibold"><slot /></div>
    <button class="btn text-white bg-white has-hover:hover:bg-white has-hover:hover:text-musi-red" @click="$emit('retry')">{{ retryTitle ?? 'Erneut versuchen' }}</button>
  </div>
  <div v-if="type === 'inline'" class="flex justify-center items-center gap-2 rounded text-musi-red">
    <div class="font-semibold"><slot /></div>
    <button class="btn text-white bg-white has-hover:hover:bg-white has-hover:hover:text-musi-red" @click="$emit('retry')">{{ retryTitle ?? 'Erneut versuchen' }}</button>
  </div>
</template>
