<script setup lang="ts">
import { computed, ref } from 'vue'
import { type CompositionListItem } from './AdminTypes'
import InfoNotification from './InfoNotification.vue'
import HorizontalDivider from './HorizontalDivider.vue'
import CompositionItem from './CompositionItem.vue'
import { uiFetchAuthorized } from './UIFetch'
import _ from 'lodash'

const props = defineProps<{
  compositions: CompositionListItem[]
}>()

defineEmits<{
  edit: [composition: CompositionListItem]
  deleted: [composition: CompositionListItem]
}>()

const filterText = ref("")
const showActiveCompositionsOnly = ref(false)

const filteredCompositions = computed(() =>
  props.compositions.filter(v => v.title.toLocaleLowerCase().includes(filterText.value.toLocaleLowerCase()) && (!showActiveCompositionsOnly.value || v.isActive))
)
const filteredCompositionsByFirstChar = computed(() => {
  return _(filteredCompositions.value)
    .groupBy(v => {
      if (v.title.length === 0) return '<leer>'
      if (v.title[0] >= '0' && v.title[0] <= '9') return '0-9'
      return v.title[0].toLocaleUpperCase()
    })
    .value()
})

const isTogglingActivate = ref(false)
const hasTogglingActivateFailed = ref(false)
const toggleActivate = async (composition: CompositionListItem) => {
  const currentValue = composition.isActive
  const newValue = !currentValue
  composition.isActive = newValue

  const result = await uiFetchAuthorized(isTogglingActivate, hasTogglingActivateFailed, composition.links.self, {
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
  <div v-if="props.compositions.length > 0" class="flex items-center gap-2 m-4">
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
    <template v-else v-for="(compositions, firstChar) in filteredCompositionsByFirstChar" :key="firstChar">
      <HorizontalDivider>{{ firstChar }}</HorizontalDivider>
      <div class="flex flex-wrap items-stretch gap-2 m-4">
        <CompositionItem v-for="composition in compositions" :key="composition.links.self" :composition="composition"
          @toggle-activate="toggleActivate(composition)"
          @edit="$emit('edit', composition)"
          @deleted="$emit('deleted', composition)" />
      </div>
    </template>
  </div>
</template>
