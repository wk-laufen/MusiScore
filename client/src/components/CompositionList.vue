<script setup lang="ts">
import { computed, ref } from 'vue'
import { type CompositionListItem } from './AdminTypes'
import { groupBy } from 'lodash-es'
import InfoNotification from './InfoNotification.vue'
import HorizontalDivider from './HorizontalDivider.vue'
import CompositionItem from './CompositionItem.vue'
import uiFetch from './UIFetch'

const props = defineProps<{
  compositions: CompositionListItem[]
}>()

defineEmits<{
  deleted: [composition: CompositionListItem]
}>()

const filterText = ref("")
const showActiveCompositionsOnly = ref(false)

const filteredCompositions = computed(() =>
  props.compositions.filter(v => v.title.toLocaleLowerCase().includes(filterText.value.toLocaleLowerCase()) && (!showActiveCompositionsOnly.value || v.isActive))
)
const filteredCompositionsByFirstChar = computed(() => {
  return groupBy(filteredCompositions.value, (v: CompositionListItem) => v.title.length > 0 ? v.title[0].toLocaleUpperCase() : '<leer>')
})

const isTogglingActivate = ref(false)
const hasTogglingActivateFailed = ref(false)
const toggleActivate = async (composition: CompositionListItem) => {
  const currentValue = composition.isActive
  const newValue = !currentValue
  composition.isActive = newValue

  const result = await uiFetch(isTogglingActivate,hasTogglingActivateFailed, composition.links.self, {
    method: 'PATCH',
    body: JSON.stringify({ isActive: newValue }),
    headers: {
      'Content-Type': 'application/json'
    }
  })
  if (!result.succeeded) {
    composition.isActive = currentValue
  }
}

</script>

<template>
  <div v-if="filteredCompositions.length > 0" class="flex items-center gap-2 m-4">
    <div>
      <input class="input-text" type="search" placeholder="Filter" v-model="filterText" />
    </div>
    <div>
      <input id="show-active-compositions-only"
        class="h-4 w-4 mt-1 mr-2"
        type="checkbox"
        v-model="showActiveCompositionsOnly" />
      <label for="show-active-compositions-only" class="select-none">Nur aktuelle Stücke anzeigen</label>
    </div>
  </div>
  <div class="flex flex-col items-stretch m-4">
    <InfoNotification v-if="filteredCompositions.length === 0">Keine Stücke vorhanden.</InfoNotification>
    <template v-else v-for="(compositions, firstChar) in filteredCompositionsByFirstChar" :key="JSON.stringify(compositions)">
      <HorizontalDivider>{{ firstChar }}</HorizontalDivider>
      <div class="flex flex-wrap items-stretch gap-2 m-4">
        <CompositionItem v-for="composition in compositions" :key="composition.links.self" :composition="composition"
          @toggle-activate="toggleActivate(composition)"
          @deleted="$emit('deleted', composition)" />
      </div>
    </template>
  </div>
</template>
