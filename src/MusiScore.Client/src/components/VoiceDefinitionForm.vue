<script setup lang="ts">
import { computed, ref, toRef } from 'vue'
import { uiFetchAuthorized } from './UIFetch'
import ErrorWithRetry from './ErrorWithRetry.vue'
import LoadingBar from './LoadingBar.vue'
import _ from 'lodash'
import type { VoiceDefinitionWithStats } from './AdminTypes'
import draggable from 'vuedraggable'
import { joinStrings } from './UI'

const props = defineProps<{
  voiceDefinitionsUrl: string
}>()

type VoiceDefinitionSaveError = 'EmptyName' | 'DuplicateName'
type EditableVoiceDefinition = {
  loadedData: Pick<VoiceDefinitionWithStats, 'name' | 'memberCount'> & { sortOrder: number, delete: boolean }
  links: VoiceDefinitionWithStats['links']
  id: number
  isNew: boolean
  name: string
  memberCount: number
  sortOrder: number
  compositions: string[]
  delete: boolean
  isSaving: boolean
  hasSavingFailed: boolean
  saveErrors: string[]
}

let nextVoiceDefinitionId = 1
const voiceDefinitions = ref<EditableVoiceDefinition[]>()
const isLoadingVoiceDefinitions = ref(false)
const hasLoadingVoiceDefinitionsFailed = ref(false)
const loadVoiceDefinitions = async () => {
  const result = await uiFetchAuthorized(isLoadingVoiceDefinitions, hasLoadingVoiceDefinitionsFailed, props.voiceDefinitionsUrl)
  if (result.succeeded) {
    const loadedVoiceDefinitions = await result.response.json() as VoiceDefinitionWithStats[]
    voiceDefinitions.value = loadedVoiceDefinitions.map((v, i) => ({
      loadedData: { name: v.name, memberCount: v.memberCount, sortOrder: i + 1, delete: false },
      links: { ...v.links },
      id: nextVoiceDefinitionId++,
      isNew: false,
      name: v.name,
      memberCount: v.memberCount,
      sortOrder: i + 1,
      compositions: v.compositions,
      delete: false,
      isSaving: false,
      hasSavingFailed: false,
      saveErrors: [],
    }))
  }
}
loadVoiceDefinitions()

const toggleDeleteVoiceDefinition = (voiceDefinition: EditableVoiceDefinition) => {
  if (voiceDefinitions.value === undefined) return

  if (voiceDefinition.isNew) {
    voiceDefinitions.value.splice(voiceDefinitions.value.indexOf(voiceDefinition), 1)
    updateSortOrder()
  }
  else {
    voiceDefinition.delete = !voiceDefinition.delete
    updateSortOrder()
  }
}

const updateSortOrder = () => {
  if (voiceDefinitions.value === undefined) return

  let sortOrder = 1
  for (const voiceDefinition of voiceDefinitions.value) {
    if (voiceDefinition.delete) continue
    voiceDefinition.sortOrder = sortOrder
    sortOrder++
  }
}

const addVoiceDefinition = () => {
  if (voiceDefinitions.value === undefined) return

  const sortOrder = (_.maxBy(voiceDefinitions.value, v => v.sortOrder)?.sortOrder || 0) + 1
  voiceDefinitions.value.push({
    loadedData: { name: '', memberCount: 1, sortOrder: sortOrder, delete: false },
    links: { self: props.voiceDefinitionsUrl },
    id: nextVoiceDefinitionId++,
    isNew: true,
    name: '',
    memberCount: 1,
    sortOrder: sortOrder,
    compositions: [],
    delete: false,
    isSaving: false,
    hasSavingFailed: false,
    saveErrors: [],
  })
}

const handleSaveErrors = (voiceDefinition: EditableVoiceDefinition, errors: VoiceDefinitionSaveError[]) => {
  voiceDefinition.saveErrors = [
    ...(errors.includes('EmptyName') ? ['Bitte geben Sie einen Namen ein.'] : []),
    ...(errors.includes('DuplicateName') ? ['Stimme existiert bereits.'] : []),
  ]
}

const createVoiceDefinition = async (voiceDefinition: EditableVoiceDefinition) => {
  const data = {
    name: voiceDefinition.name,
    sortOrder: voiceDefinition.sortOrder,
    memberCount: voiceDefinition.memberCount,
  }
  const result = await uiFetchAuthorized(
    toRef(voiceDefinition, 'isSaving'),
    toRef(voiceDefinition, 'hasSavingFailed'),
    voiceDefinition.links.self,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    }
  )
  if (result.succeeded) {
    const response = await result.response.json() as VoiceDefinitionWithStats
    voiceDefinition.isNew = false
    voiceDefinition.name = response.name
    voiceDefinition.links = { ...response.links }
    voiceDefinition.loadedData = {
      name: response.name,
      memberCount: response.memberCount,
      sortOrder: voiceDefinition.sortOrder,
      delete: false
    }
  }
  else if (result.response !== undefined) {
    const errors = await result.response.json() as VoiceDefinitionSaveError[]
    handleSaveErrors(voiceDefinition, errors)
  }
}

const deleteVoiceDefinition = async (voiceDefinition: EditableVoiceDefinition) => {
  if (voiceDefinitions.value === undefined) return

  const result = await uiFetchAuthorized(
    toRef(voiceDefinition, 'isSaving'),
    toRef(voiceDefinition, 'hasSavingFailed'),
    voiceDefinition.links.self,
    {
      method: 'DELETE',
    }
  )
  if (result.succeeded) {
    voiceDefinitions.value.splice(voiceDefinitions.value.indexOf(voiceDefinition), 1)
  }
}

const updateVoiceDefinition = async (voiceDefinition: EditableVoiceDefinition) => {
  const data = {
    name: voiceDefinition.name,
    sortOrder: voiceDefinition.sortOrder,
    memberCount: voiceDefinition.memberCount,
  }
  const result = await uiFetchAuthorized(
    toRef(voiceDefinition, 'isSaving'),
    toRef(voiceDefinition, 'hasSavingFailed'),
    voiceDefinition.links.self,
    {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    }
  )
  if (result.succeeded) {
    voiceDefinition.loadedData = {
      name: voiceDefinition.name,
      memberCount: voiceDefinition.memberCount,
      sortOrder: voiceDefinition.sortOrder,
      delete: false
    }
  }
  else if (result.response !== undefined) {
    const errors = await result.response.json() as VoiceDefinitionSaveError[]
    handleSaveErrors(voiceDefinition, errors)
  }
}

const saveVoiceDefinition = async (voiceDefinition: EditableVoiceDefinition) => {
  voiceDefinition.saveErrors = []
  if (voiceDefinition.isNew) {
    await createVoiceDefinition(voiceDefinition)
  }
  else if (voiceDefinition.delete) {
    await deleteVoiceDefinition(voiceDefinition)
  }
  else {
    await updateVoiceDefinition(voiceDefinition)
  }
}

const hasChanged = (voiceDefinition: EditableVoiceDefinition) => {
  let data : EditableVoiceDefinition['loadedData'] = {
      name: voiceDefinition.name,
      memberCount: voiceDefinition.memberCount,
      sortOrder: voiceDefinition.sortOrder,
      delete: voiceDefinition.delete
    }
    return !_.isEqual(voiceDefinition.loadedData, data)
}

const canSave = computed(() => {
  if (voiceDefinitions.value === undefined) return false
  return voiceDefinitions.value.some(hasChanged)
})

const save = async () => {
  if (voiceDefinitions.value === undefined) return
  if (!canSave.value) return

  await Promise.all(voiceDefinitions.value.filter(hasChanged).map(v => saveVoiceDefinition(v)))
}

defineExpose({ canSave, save })
</script>

<template>
  <h3 class="mt-2 text-xl small-caps">Stimmen</h3>
  <LoadingBar v-if="isLoadingVoiceDefinitions" />
  <ErrorWithRetry v-else-if="hasLoadingVoiceDefinitionsFailed" type="inline" @retry="loadVoiceDefinitions">Fehler beim Laden der Stimmen.</ErrorWithRetry>
  <div v-else-if="voiceDefinitions !== undefined" class="flex flex-col gap-2 mt-2">
    <draggable v-model="voiceDefinitions" item-key="id" animation="150" filter="input[type=text]" :preventOnFilter="false" tag="ul" handle=".handle" class="flex flex-col gap-2" @end="updateSortOrder">
      <template #item="{ element: voiceDefinition } : { element: EditableVoiceDefinition }">
        <li>
          <div class="flex items-center gap-4">
            <button class="btn" :class="{ 'btn-solid btn-red': voiceDefinition.delete }"
              :disabled="voiceDefinition.compositions.length > 0"
              :title="voiceDefinition.compositions.length > 0 ? `Verwendet in ${joinStrings(voiceDefinition.compositions.map((v: string) => `\x22${v}\x22`))}` : 'Nicht verwendet'"
              @click="toggleDeleteVoiceDefinition(voiceDefinition)">
              <font-awesome-icon :icon="['fas', 'trash-can']" />
            </button>
            <div class="handle cursor-grab">
              <font-awesome-icon :icon="['fas', 'up-down']" />
            </div>
            <div class="flex items-center gap-2">
              <input type="number" min="0" v-model="voiceDefinition.memberCount" class="input-text min-w-20! w-20" />
              <font-awesome-icon :icon="['fas', 'xmark']" />
              <input type="text" v-model="voiceDefinition.name" required placeholder="Name" :disabled="voiceDefinition.delete || voiceDefinition.isSaving" class="input-text" />
            </div>
            <span v-if="voiceDefinition.saveErrors.length > 0" class="text-sm text-musi-red">{{ voiceDefinition.saveErrors.join(" ") }}</span>
            <span v-else-if="voiceDefinition.hasSavingFailed" class="text-sm text-musi-red">Fehler beim Speichern.</span>
          </div>
        </li>
      </template>
    </draggable>
    <button class="btn btn-green btn-solid self-start px-8! py-4!" @click="addVoiceDefinition">Neue Stimme</button>
  </div>
</template>