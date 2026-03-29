<script setup lang="ts">
</script>

<template>
  <div class="flex items-center justify-center gap-2">
    <font-awesome-icon :icon="['fas', 'info-circle']" size="lg" class="text-musi-blue" />
    <span class="text-lg text-musi-blue">
      <slot />
    </span>
  </div>
</template>
