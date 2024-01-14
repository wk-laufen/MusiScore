<script setup lang="ts">
import { ref } from 'vue'
import { type CompositionListItem } from './AdminTypes'

defineEmits(['toggleActivate', 'edit', 'delete'])

defineProps<{
  composition: CompositionListItem
}>()

const isMarkedForDeletion = ref(false)
const isDeleting = ref(false)
const hasDeletingFailed = ref(false)
</script>

<template>
  <div class="flex items-stretch border rounded font-semibold text-blue-700 border-blue-500 divide-x divide-blue-500">
    <span class="grow flex items-center justify-center text-center !p-8 w-60">{{ composition.title }}</span>
    <button class="p-4"
      :title="composition.isActive ? 'Markierung entfernen' : 'Als aktuelles Stück markieren'"
      @click="$emit('toggleActivate')">
      <font-awesome-icon v-if="composition.isActive" :icon="['fas', 'star']" />
      <font-awesome-icon v-else :icon="['far', 'star']" />
    </button>
    <div class="flex flex-col justify-items-stretch divide-y divide-blue-500">
      <button class="p-4 grow" title="Bearbeiten" @click="$emit('edit')">
        <font-awesome-icon :icon="['fas', 'pen']" />
      </button>
      <button v-if="isMarkedForDeletion"
        class="p-4 grow bg-blue-500"
        title="Wirklich löschen"
        @click="$emit('delete')">
        <font-awesome-icon :icon="['fas', 'trash']" class="text-white" />
      </button>
      <div v-else-if="isDeleting" class="px-2 py-3 grow">
        <div class="spinner spinner-blue"></div>
      </div>
      <button v-else-if="hasDeletingFailed"
        class="p-4 grow bg-blue-500"
        title="Löschen erneut versuchen"
        @click="$emit('delete')">
        <font-awesome-icon :icon="['fas', 'trash']" class="text-musi-red" />
      </button>
      <button v-else class="p-4 grow" title="Löschen" @click="isMarkedForDeletion = true">
        <font-awesome-icon :icon="['fas', 'trash']" />
      </button>
    </div>
  </div>
</template>
./AdminTypes