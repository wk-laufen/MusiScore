<script setup lang="ts">
import { ref } from 'vue'
import LoadingBar from './LoadingBar.vue'
import { type CompositionListItem } from './AdminTypes'
import { uiFetchAuthorized } from './UIFetch'
import { sortBy } from 'lodash-es';

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
  <div class="flex items-stretch border rounded-sm text-blue-700 border-blue-500 divide-x divide-blue-500">
    <div class="grow flex flex-col items-center justify-center text-center p-8! w-60">
      <span class="font-semibold">{{ composition.title }}</span>
      <span v-for="tag in sortBy(composition.tags.filter(v => v.value !== null && v.settings.overviewDisplayFormat !== null), [v => v.settings.overviewDisplayFormat?.order])"
        :key="tag.key"
        class="text-stone-800 text-sm">
        {{ tag.settings.overviewDisplayFormat?.format.replace('%s', tag.value!) }}
      </span>
    </div>
    <button class="p-4 cursor-pointer"
      :title="composition.isActive ? 'Markierung entfernen' : 'Als aktuelles Stück markieren'"
      @click="$emit('toggleActivate')">
      <font-awesome-icon v-if="composition.isActive" :icon="['fas', 'star']" />
      <font-awesome-icon v-else :icon="['far', 'star']" />
    </button>
    <div class="flex flex-col justify-items-stretch divide-y divide-blue-500">
      <button class="p-4 grow cursor-pointer" title="Bearbeiten" @click="$emit('edit')">
        <font-awesome-icon :icon="['fas', 'pen']" />
      </button>
      <LoadingBar v-if="isDeleting" type="minimal" class="p-4 grow" />
      <button v-else-if="hasDeletingFailed"
        class="p-4 grow bg-blue-500 cursor-pointer"
        title="Löschen erneut versuchen"
        @click="deleteComposition">
        <font-awesome-icon :icon="['fas', 'trash-can']" class="text-musi-red" />
      </button>
      <button v-else-if="isMarkedForDeletion"
        class="p-4 grow bg-blue-500 cursor-pointer"
        title="Wirklich löschen"
        @click="deleteComposition">
        <font-awesome-icon :icon="['fas', 'trash-can']" class="text-white" />
      </button>
      <button v-else class="p-4 grow cursor-pointer" title="Löschen" @click="isMarkedForDeletion = true">
        <font-awesome-icon :icon="['fas', 'trash-can']" />
      </button>
    </div>
  </div>
</template>
