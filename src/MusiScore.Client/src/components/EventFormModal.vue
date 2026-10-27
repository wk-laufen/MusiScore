<script setup lang="ts">
import { computed, ref, useTemplateRef, watch } from 'vue'
import LoadButton from './LoadButton.vue'
import TextInput from './TextInput.vue'
import { type CompositionListItem, type Event, type EventSaveError } from './AdminTypes'
import type { ValidationState } from './Validation'
import { uiFetchAuthorized } from './UIFetch'

const props = defineProps<{
  /** `undefined` to create a new event */
  event: Event | undefined
  compositions: CompositionListItem[]
  eventsUrl: string
}>()

const emit = defineEmits<{
  'close': []
  'saved': [event: Event, compositionIds: string[]]
  'deleted': [event: Event]
}>()

const name = ref(props.event?.name ?? '')
const nameValidationState = ref<ValidationState>({ type: 'notValidated' })
const isActive = ref(props.event?.isActive ?? false)
const compositionIds = ref(props.event === undefined
  ? []
  : props.compositions.filter(v => v.eventIds.includes(props.event!.id)).map(v => v.id))

const filterText = ref('')
const filteredCompositions = computed(() =>
  props.compositions.filter(v => v.title.toLocaleLowerCase().includes(filterText.value.toLocaleLowerCase()))
)

const popover = useTemplateRef('popover')
watch(popover, el => {
  if (el === null) return

  el.addEventListener('toggle', (e: globalThis.Event) => {
    if ((e as ToggleEvent).newState === 'closed') {
      emit('close')
    }
  })
  el.showPopover()
})

const close = () => {
  popover.value!.hidePopover()
}

const isSaving = ref(false)
const hasSavingFailed = ref(false)
const save = async () => {
  nameValidationState.value = { type: 'notValidated' }
  const result = await uiFetchAuthorized(isSaving, hasSavingFailed, props.event?.links.self ?? props.eventsUrl, {
    method: props.event === undefined ? 'POST' : 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: name.value, isActive: isActive.value, compositionIds: compositionIds.value })
  })
  if (result.succeeded) {
    emit('saved', await result.response.json() as Event, compositionIds.value)
    close()
  }
  else if (result.response?.status === 400) {
    const errors = await result.response.json() as EventSaveError[]
    if (errors.includes('DuplicateName')) {
      nameValidationState.value = { type: 'error', error: 'Eine Veranstaltung mit diesem Namen existiert bereits.' }
    }
    else if (errors.includes('EmptyName')) {
      nameValidationState.value = { type: 'error', error: 'Bitte geben Sie den Namen der Veranstaltung ein.' }
    }
  }
}

const isMarkedForDeletion = ref(false)
const isDeleting = ref(false)
const hasDeletingFailed = ref(false)
const deleteEvent = async () => {
  const event = props.event
  if (event === undefined) return

  const result = await uiFetchAuthorized(isDeleting, hasDeletingFailed, event.links.self, { method: 'DELETE' })
  if (result.succeeded) {
    emit('deleted', event)
    close()
  }
}
</script>

<template>
  <div ref="popover" popover class="m-auto rounded-md border shadow-lg min-w-96 max-w-full max-h-[90vh] backdrop:bg-black/30 backdrop:backdrop-blur-[2px] divide-y flex flex-col">
    <div class="flex items-center justify-between">
      <span class="font-semibold ml-4">{{ event === undefined ? 'Neue Veranstaltung' : 'Veranstaltung bearbeiten' }}</span>
      <button class="cursor-pointer ml-4 p-4" title="Schließen" @click="close">
        <font-awesome-icon :icon="['fas', 'xmark']" />
      </button>
    </div>

    <div class="p-4 flex flex-col gap-4 min-h-0">
      <TextInput title="Name" :placeholder="`z.B. ${new Date().getFullYear()} - Jahreskonzert`" :validation-state="nameValidationState" v-model="name" @keyup.enter="save" />
      <label class="flex items-center gap-2 select-none">
        <input type="checkbox" class="h-4 w-4" v-model="isActive" />
        Aktiv - die Stücke werden auf der Druckseite angezeigt
      </label>
      <div class="flex flex-col gap-2 min-h-0">
        <span class="input-label">Stücke ({{ compositionIds.length }} ausgewählt)</span>
        <input class="input-text" type="search" placeholder="Filter" v-model="filterText" />
        <div class="flex flex-col overflow-auto max-h-80 text-sm">
          <label v-for="composition in filteredCompositions" :key="composition.id" class="flex items-center gap-2 py-1 select-none">
            <input type="checkbox" class="h-4 w-4" :value="composition.id" v-model="compositionIds" />
            {{ composition.title }}
          </label>
          <span v-if="filteredCompositions.length === 0" class="text-slate-500">Keine Stücke gefunden.</span>
        </div>
      </div>
    </div>

    <div class="flex items-center gap-2 px-4 py-3">
      <template v-if="event !== undefined">
        <LoadButton v-if="isMarkedForDeletion" class="btn-red btn-solid" :loading="isDeleting" @click="deleteEvent">
          {{ hasDeletingFailed ? 'Löschen erneut versuchen' : 'Wirklich löschen' }}
        </LoadButton>
        <button v-else class="btn btn-red" title="Die Stücke bleiben erhalten" @click="isMarkedForDeletion = true">Löschen</button>
      </template>
      <span class="grow"></span>
      <button class="btn btn-blue" @click="close">Abbrechen</button>
      <LoadButton class="btn-blue btn-solid" :loading="isSaving" @click="save">
        <div class="flex items-center gap-2">
          <span>Speichern</span>
          <font-awesome-icon v-if="hasSavingFailed && nameValidationState.type !== 'error'" class="text-musi-red" :icon="['fas', 'xmark']" />
        </div>
      </LoadButton>
    </div>
  </div>
</template>
