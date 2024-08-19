<script setup lang="ts">
</script>

<template>
  <div class="flex items-center justify-center gap-2">
    <font-awesome-icon :icon="['fas', 'info-circle']" size="lg" class="text-blue-700" />
    <span class="text-lg text-blue-700">
      <slot />
    </span>
  </div>
</template>
