<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { type CompositionListItem, type Event } from './AdminTypes'
import InfoNotification from './InfoNotification.vue'
import HorizontalDivider from './HorizontalDivider.vue'
import CompositionItem from './CompositionItem.vue'
import EventFormModal from './EventFormModal.vue'
import { uiFetchAuthorized } from './UIFetch'
import _ from 'lodash'

const props = defineProps<{
  compositions: CompositionListItem[]
  events: Event[]
  eventsUrl: string
  voiceDefinitionGroupsUrl: string
}>()

const emit = defineEmits<{
  edit: [composition: CompositionListItem]
  deleted: [composition: CompositionListItem]
  eventSaved: [event: Event, compositionIds: string[]]
  eventDeleted: [event: Event]
}>()

const filterText = ref("")
const selectedEventId = ref("")
// always keep an existing event selected, e.g. initially or after the selected one was deleted
watch(() => props.events, events => {
  if (!events.some(v => v.id === selectedEventId.value)) {
    selectedEventId.value = events[0]?.id ?? ""
  }
}, { immediate: true })
const selectedEvent = computed(() => props.events.find(v => v.id === selectedEventId.value))
const showSelectedEventCompositionsOnly = ref(true)

const filteredCompositions = computed(() =>
  props.compositions.filter(v =>
    v.title.toLocaleLowerCase().includes(filterText.value.toLocaleLowerCase()) &&
    (selectedEvent.value === undefined || !showSelectedEventCompositionsOnly.value || v.eventIds.includes(selectedEvent.value.id))
  )
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

const isTogglingEvent = ref(false)
const hasTogglingEventFailed = ref(false)
const toggleEvent = async (composition: CompositionListItem, event: Event) => {
  const currentEventIds = composition.eventIds
  const isAdding = !currentEventIds.includes(event.id)
  composition.eventIds = isAdding ? [ ...currentEventIds, event.id ] : currentEventIds.filter(v => v !== event.id)

  const result = await uiFetchAuthorized(isTogglingEvent, hasTogglingEventFailed, `${event.links.compositions}/${composition.id}`, {
    method: isAdding ? 'PUT' : 'DELETE'
  })
  if (!result.succeeded) {
    composition.eventIds = currentEventIds
  }
}

type EventForm = { type: 'create' } | { type: 'edit', event: Event }
const eventForm = ref<EventForm>()

const eventSaved = (event: Event, compositionIds: string[]) => {
  emit('eventSaved', event, compositionIds)
  selectedEventId.value = event.id
}

const eventDeleted = (event: Event) => {
  emit('eventDeleted', event)
}
</script>

<template>
  <div v-if="props.compositions.length > 0" class="flex flex-col items-start gap-2 m-4">
    <div class="flex items-center gap-2">
      <select class="input-select" v-model="selectedEventId">
        <option v-if="events.length === 0" value="" disabled>-- Keine Veranstaltungen --</option>
        <option v-for="event in events" :key="event.id" :value="event.id">{{ event.name }}{{ event.isActive ? ' (aktiv)' : '' }}</option>
      </select>
      <button class="p-2 cursor-pointer text-blue-700" title="Neue Veranstaltung" @click="eventForm = { type: 'create' }">
        <font-awesome-icon :icon="['fas', 'plus']" />
      </button>
      <template v-if="selectedEvent !== undefined">
        <button class="p-2 cursor-pointer text-blue-700" title="Veranstaltung bearbeiten" @click="eventForm = { type: 'edit', event: selectedEvent }">
          <font-awesome-icon :icon="['fas', 'pen']" />
        </button>
        <div class="ml-2">
          <input id="show-selected-event-compositions-only"
            class="h-4 w-4 mt-1 mr-2"
            type="checkbox"
            v-model="showSelectedEventCompositionsOnly" />
          <label for="show-selected-event-compositions-only" class="select-none">Nur Stücke dieser Veranstaltung anzeigen</label>
        </div>
      </template>
    </div>
    <div>
      <input class="input-text" type="search" placeholder="Filter" v-model="filterText" />
    </div>
  </div>
  <div class="flex flex-col items-stretch m-4">
    <InfoNotification v-if="filteredCompositions.length === 0">Keine Stücke vorhanden.</InfoNotification>
    <template v-else v-for="(compositions, firstChar) in filteredCompositionsByFirstChar" :key="firstChar">
      <HorizontalDivider>{{ firstChar }}</HorizontalDivider>
      <div class="flex flex-wrap items-stretch gap-2 m-4">
        <CompositionItem v-for="composition in compositions" :key="composition.links.self" :composition="composition"
          :event="selectedEvent"
          :voice-definition-groups-url="voiceDefinitionGroupsUrl"
          @toggle-event="selectedEvent !== undefined && toggleEvent(composition, selectedEvent)"
          @edit="$emit('edit', composition)"
          @deleted="$emit('deleted', composition)" />
      </div>
    </template>
  </div>
  <EventFormModal v-if="eventForm !== undefined"
    :event="eventForm.type === 'edit' ? eventForm.event : undefined"
    :compositions="compositions"
    :events-url="eventsUrl"
    @saved="eventSaved"
    @deleted="eventDeleted"
    @close="eventForm = undefined" />
</template>
