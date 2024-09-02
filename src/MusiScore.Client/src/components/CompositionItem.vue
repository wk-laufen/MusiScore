<script setup lang="ts">
import { ref } from 'vue'
import LoadingBar from './LoadingBar.vue'
import { type CompositionListItem } from './AdminTypes'
import { uiFetchAuthorized } from './UIFetch'

const emit = defineEmits<{
  'toggleActivate': []
  'edit': []
  'deleted': []
}>()

const props = defineProps<{
  composition: CompositionListItem
}>()

const isMarkedForDeletion = ref(false)
const isDeleting = ref(false)
const hasDeletingFailed = ref(false)
const deleteComposition = async () => {
  const response = await uiFetchAuthorized(isDeleting, hasDeletingFailed, props.composition.links.self, { method: 'DELETE' })
  if (response.succeeded) {
    emit('deleted')
  }
}
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
      <LoadingBar v-if="isDeleting" type="minimal" class="p-4 grow" />
      <button v-else-if="hasDeletingFailed"
        class="p-4 grow bg-blue-500"
        title="Löschen erneut versuchen"
        @click="deleteComposition">
        <font-awesome-icon :icon="['fas', 'trash']" class="text-musi-red" />
      </button>
      <button v-else-if="isMarkedForDeletion"
        class="p-4 grow bg-blue-500"
        title="Wirklich löschen"
        @click="deleteComposition">
        <font-awesome-icon :icon="['fas', 'trash']" class="text-white" />
      </button>
      <button v-else class="p-4 grow" title="Löschen" @click="isMarkedForDeletion = true">
        <font-awesome-icon :icon="['fas', 'trash']" />
      </button>
    </div>
  </div>
</template>
