<script setup lang="ts">
const props = defineProps<{
  type?: 'minimal' | 'full'
}>()
const text = props.type === 'minimal' ? 'M' : 'MusiScore'
</script>

<template>
  <div class="flex items-center justify-center gap-4">
    <span v-for="(c, index) in text" :key="c" class="small-caps animate-ping" :style="{ 'animation-delay': (index * 50) + 'ms' }">{{ c }}</span>
  </div>
</template>
