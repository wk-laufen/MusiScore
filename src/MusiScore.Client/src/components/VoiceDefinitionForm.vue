<script setup lang="ts">
import { computed, ref, toRef } from 'vue'
import { uiFetchAuthorized } from './UIFetch'
import ErrorWithRetry from './ErrorWithRetry.vue'
import LoadingBar from './LoadingBar.vue'
import VoiceDefinitionGroupItem from './VoiceDefinitionGroupItem.vue'
import _ from 'lodash'
import type {
  GroupedVoiceDefinition,
  UngroupedVoiceDefinitions,
  UserGroupedVoiceDefinitions,
  VoiceDefinition,
  VoiceDefinitionGroup,
  VoiceDefinitionDeleteError,
  VoiceDefinitionGroupSaveError,
  VoiceDefinitionInputs,
  VoiceDefinitionSaveError,
  VoiceDefinitionWithStats
} from './AdminTypes'
import draggable from 'vuedraggable'
import { EditableVoiceDefinitionGroup, type EditableVoiceDefinition, type EditableVoiceDefinitionNoGroup, type EditableVoiceDefinitionUserGroup, type VoiceDefinitionGroupInputs } from './VoiceDefinitionTypes'

const props = defineProps<{
  voiceDefinitionsUrl: string
  voiceDefinitionGroupsUrl: string
}>()

let nextId = 1
const newId = (prefix: string) => `${prefix}${nextId++}`

const userGroups = ref<EditableVoiceDefinitionUserGroup[]>()
const noGroups = ref<EditableVoiceDefinitionNoGroup[]>()

/** all groups in display order, the ungrouped voice definitions last */
const groups = computed(() =>
  userGroups.value !== undefined && noGroups.value !== undefined
    ? [...userGroups.value, ...noGroups.value] as EditableVoiceDefinitionGroup[]
    : undefined
)

/** every voice definition across all groups, so a deleted one can hand its voices to any other */
const allVoiceDefinitions = computed(() => groups.value?.flatMap(v => v.voiceDefinitions) ?? [])

const isLoading = ref(false)
const hasLoadingFailed = ref(false)

// the sort order of a loaded item is derived from its position, see `updateSortOrder`
const loadVoiceDefinition = (v: VoiceDefinitionWithStats, groupId: string | null) : EditableVoiceDefinition => ({
  loadedData: undefined,
  links: { ...v.links },
  id: newId('v'),
  serverId: v.id,
  isNew: false,
  name: v.name,
  memberCount: v.memberCount,
  sortOrder: 0,
  groupId,
  compositions: v.compositions,
  replacementId: '',
  replacementIdValidationState: { type: 'notValidated' },
  delete: false,
  isSaving: false,
  hasSavingFailed: false,
  saveErrors: [],
})

const loadUserGroup = (group: UserGroupedVoiceDefinitions) : EditableVoiceDefinitionUserGroup => {
  const id = newId('g')
  return {
    type: 'UserGroup',
    loadedData: undefined,
    links: { ...group.links },
    id,
    serverId: group.id,
    isNew: false,
    name: group.name,
    sortOrder: 0,
    delete: false,
    isSaving: false,
    hasSavingFailed: false,
    saveErrors: [],
    voiceDefinitions: group.voiceDefinitions.map(v => loadVoiceDefinition(v, id))
  }
}

const loadNoGroup = (group: UngroupedVoiceDefinitions) : EditableVoiceDefinitionNoGroup => ({
  type: 'NoGroup',
  voiceDefinitions: group.voiceDefinitions.map(v => loadVoiceDefinition(v, null))
})

const load = async () => {
  const result = await uiFetchAuthorized(isLoading, hasLoadingFailed, props.voiceDefinitionGroupsUrl)
  if (!result.succeeded) return

  const loaded = await result.response.json() as GroupedVoiceDefinition[]
  userGroups.value = loaded.filter(v => v.type === 'UserGroup').map(loadUserGroup)
  noGroups.value = loaded.filter(v => v.type === 'NoGroup').map(loadNoGroup)
  updateSortOrder()
  // whatever we ended up with is the state as it was loaded, so nothing counts as changed yet
  for (const group of userGroups.value) {
    group.loadedData = getGroupInputs(group)
  }
  for (const group of groups.value ?? []) {
    for (const voiceDefinition of group.voiceDefinitions) {
      voiceDefinition.loadedData = getVoiceDefinitionInputs(voiceDefinition)
    }
  }
}
load()

/**
 * Voice definitions are sorted by group first, so their sort order (which is local to their group) as
 * well as the group they belong to are derived from where they are placed in the UI.
 */
const updateSortOrder = () => {
  if (userGroups.value === undefined || groups.value === undefined) return

  let groupSortOrder = 1
  for (const group of userGroups.value) {
    if (group.delete) continue
    group.sortOrder = groupSortOrder
    groupSortOrder++
  }

  for (const group of groups.value) {
    let voiceDefinitionSortOrder = 1
    for (const voiceDefinition of group.voiceDefinitions) {
      voiceDefinition.groupId = EditableVoiceDefinitionGroup.getClientId(group)
      if (voiceDefinition.delete) continue
      voiceDefinition.sortOrder = voiceDefinitionSortOrder
      voiceDefinitionSortOrder++
    }
  }
}

const addGroup = () => {
  if (userGroups.value === undefined) return

  userGroups.value.push({
    type: 'UserGroup',
    loadedData: undefined,
    links: { self: props.voiceDefinitionGroupsUrl },
    id: newId('g'),
    serverId: null,
    isNew: true,
    name: '',
    sortOrder: (_.maxBy(userGroups.value, v => v.sortOrder)?.sortOrder || 0) + 1,
    delete: false,
    isSaving: false,
    hasSavingFailed: false,
    saveErrors: [],
    voiceDefinitions: []
  })
}

const toggleDeleteGroup = (group: EditableVoiceDefinitionUserGroup) => {
  if (userGroups.value === undefined) return

  if (group.isNew) {
    const index = userGroups.value.indexOf(group)
    userGroups.value.splice(index, 1)
  }
  else {
    group.delete = !group.delete
  }
  updateSortOrder()
}

const addVoiceDefinition = (group: EditableVoiceDefinitionGroup) => {
  group.voiceDefinitions.push({
    loadedData: undefined,
    links: { self: props.voiceDefinitionsUrl },
    id: newId('v'),
    serverId: null,
    isNew: true,
    name: '',
    memberCount: 1,
    sortOrder: 0,
    groupId: EditableVoiceDefinitionGroup.getClientId(group),
    compositions: [],
    replacementId: '',
    replacementIdValidationState: { type: 'notValidated' },
    delete: false,
    isSaving: false,
    hasSavingFailed: false,
    saveErrors: [],
  })
  updateSortOrder()
}

const toggleDeleteVoiceDefinition = (group: EditableVoiceDefinitionGroup, voiceDefinition: EditableVoiceDefinition) => {
  if (voiceDefinition.isNew) {
    const index = group.voiceDefinitions.indexOf(voiceDefinition)
    group.voiceDefinitions.splice(index, 1)
  }
  else {
    voiceDefinition.delete = !voiceDefinition.delete
  }
  updateSortOrder()
}

const getGroupInputs = (group: EditableVoiceDefinitionUserGroup) : VoiceDefinitionGroupInputs =>
  ({ name: group.name, sortOrder: group.sortOrder, delete: group.delete })

const hasGroupChanged = (group: EditableVoiceDefinitionUserGroup) =>
  !_.isEqual(group.loadedData, getGroupInputs(group))

const getVoiceDefinitionInputs = (voiceDefinition: EditableVoiceDefinition) : VoiceDefinitionInputs =>
  ({
    name: voiceDefinition.name,
    memberCount: voiceDefinition.memberCount,
    sortOrder: voiceDefinition.sortOrder,
    groupId: voiceDefinition.groupId,
    delete: voiceDefinition.delete
  })

const hasVoiceDefinitionChanged = (voiceDefinition: EditableVoiceDefinition) =>
  !_.isEqual(voiceDefinition.loadedData, getVoiceDefinitionInputs(voiceDefinition))

const handleGroupSaveErrors = (group: EditableVoiceDefinitionUserGroup, errors: VoiceDefinitionGroupSaveError[]) => {
  group.saveErrors = [
    ...(errors.includes('EmptyName') ? ['Bitte geben Sie einen Namen ein.'] : []),
    ...(errors.includes('DuplicateName') ? ['Gruppe existiert bereits.'] : []),
  ]
}

const saveNewGroup = async (group: EditableVoiceDefinitionUserGroup) => {
  const data = { name: group.name, sortOrder: group.sortOrder }
  const result = await uiFetchAuthorized(
    toRef(group, 'isSaving'),
    toRef(group, 'hasSavingFailed'),
    group.links.self,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    }
  )
  if (result.succeeded) {
    const response = await result.response.json() as VoiceDefinitionGroup
    group.isNew = false
    group.serverId = response.id
    group.name = response.name
    group.links = { ...response.links }
    group.loadedData = { name: response.name, sortOrder: group.sortOrder, delete: false }
  }
  else if (result.response !== undefined) {
    handleGroupSaveErrors(group, await result.response.json() as VoiceDefinitionGroupSaveError[])
  }
}

const updateGroup = async (group: EditableVoiceDefinitionUserGroup) => {
  const data = { name: group.name, sortOrder: group.sortOrder }
  const result = await uiFetchAuthorized(
    toRef(group, 'isSaving'),
    toRef(group, 'hasSavingFailed'),
    group.links.self,
    {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    }
  )
  if (result.succeeded) {
    group.loadedData = getGroupInputs(group)
  }
  else if (result.response !== undefined) {
    handleGroupSaveErrors(group, await result.response.json() as VoiceDefinitionGroupSaveError[])
  }
}

const deleteGroup = async (group: EditableVoiceDefinitionUserGroup) => {
  if (userGroups.value === undefined) return

  const result = await uiFetchAuthorized(
    toRef(group, 'isSaving'),
    toRef(group, 'hasSavingFailed'),
    group.links.self,
    { method: 'DELETE' }
  )
  if (result.succeeded) {
    userGroups.value.splice(userGroups.value.indexOf(group), 1)
  }
}

const saveGroup = async (group: EditableVoiceDefinitionUserGroup) => {
  group.saveErrors = []
  if (group.isNew) {
    await saveNewGroup(group)
  }
  else if (group.delete) {
    await deleteGroup(group)
  }
  else {
    await updateGroup(group)
  }
}

const handleVoiceDefinitionSaveErrors = (voiceDefinition: EditableVoiceDefinition, errors: VoiceDefinitionSaveError[]) => {
  voiceDefinition.saveErrors = [
    ...(errors.includes('EmptyName') ? ['Bitte geben Sie einen Namen ein.'] : []),
    ...(errors.includes('DuplicateName') ? ['Stimme existiert bereits.'] : []),
    ...(errors.includes('UnknownGroup') ? ['Gruppe existiert nicht.'] : []),
  ]
}

const saveNewVoiceDefinition = async (voiceDefinition: EditableVoiceDefinition, groupServerId: string | null) => {
  const data = {
    name: voiceDefinition.name,
    sortOrder: voiceDefinition.sortOrder,
    groupId: groupServerId,
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
    const response = await result.response.json() as VoiceDefinition
    voiceDefinition.isNew = false
    voiceDefinition.serverId = response.id
    voiceDefinition.name = response.name
    voiceDefinition.links = { ...response.links }
    voiceDefinition.loadedData = getVoiceDefinitionInputs(voiceDefinition)
  }
  else if (result.response !== undefined && result.response.status === 400) {
    handleVoiceDefinitionSaveErrors(voiceDefinition, await result.response.json() as VoiceDefinitionSaveError[])
  }
}

const updateVoiceDefinition = async (voiceDefinition: EditableVoiceDefinition, groupServerId: string | null) => {
  const data = {
    name: voiceDefinition.name,
    sortOrder: voiceDefinition.sortOrder,
    group: { id: groupServerId },
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
    voiceDefinition.loadedData = getVoiceDefinitionInputs(voiceDefinition)
  }
  else if (result.response !== undefined && result.response.status === 400) {
    handleVoiceDefinitionSaveErrors(voiceDefinition, await result.response.json() as VoiceDefinitionSaveError[])
  }
}

const deleteVoiceDefinition = async (group: EditableVoiceDefinitionGroup, voiceDefinition: EditableVoiceDefinition) => {
  const result = await uiFetchAuthorized(
    toRef(voiceDefinition, 'isSaving'),
    toRef(voiceDefinition, 'hasSavingFailed'),
    voiceDefinition.links.self,
    {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        replacementVoiceDefinitionId: voiceDefinition.replacementId || null
      })
    }
  )
  if (result.succeeded) {
    group.voiceDefinitions.splice(group.voiceDefinitions.indexOf(voiceDefinition), 1)
  }
  else if (result.response !== undefined && result.response.status === 400) {
    const errors = await result.response.json() as VoiceDefinitionDeleteError[]
    voiceDefinition.replacementIdValidationState = errors.includes('InvalidReplacementVoiceDefinitionId')
      ? { type: 'error', error: 'Bitte wählen Sie eine Stimme, die stattdessen verwendet werden soll.' }
      : { type: 'success' }
  }
}

const saveVoiceDefinition = async (group: EditableVoiceDefinitionGroup, voiceDefinition: EditableVoiceDefinition) => {
  voiceDefinition.saveErrors = []
  voiceDefinition.replacementIdValidationState = { type: 'notValidated' }
  if (voiceDefinition.isNew) {
    await saveNewVoiceDefinition(voiceDefinition, EditableVoiceDefinitionGroup.getServerId(group))
  }
  else if (voiceDefinition.delete) {
    await deleteVoiceDefinition(group, voiceDefinition)
  }
  else {
    await updateVoiceDefinition(voiceDefinition, EditableVoiceDefinitionGroup.getServerId(group))
  }
}

const canSave = computed(() => {
  if (userGroups.value === undefined || groups.value === undefined) return false

  return userGroups.value.some(hasGroupChanged) ||
    groups.value.some(group => group.voiceDefinitions.some(hasVoiceDefinitionChanged))
})

const save = async () => {
  if (userGroups.value === undefined || groups.value === undefined) return
  if (!canSave.value) return

  // groups are saved first so that voice definitions can reference the id of a newly created group
  await Promise.all(userGroups.value.filter(hasGroupChanged).map(saveGroup))

  await Promise.all(
    groups.value
      // voice definitions of a group that couldn't be saved would end up in the wrong group
      .filter(group => group.type === 'NoGroup' || (group.saveErrors.length === 0 && !group.hasSavingFailed))
      .flatMap(group =>
        group.voiceDefinitions
          .filter(hasVoiceDefinitionChanged)
          .map(voiceDefinition => saveVoiceDefinition(group, voiceDefinition))
      )
  )
}

defineExpose({ canSave, save })
</script>

<template>
  <h3 class="mt-2 text-xl small-caps">Stimmen</h3>
  <LoadingBar v-if="isLoading" />
  <ErrorWithRetry v-else-if="hasLoadingFailed" type="inline" @retry="load">Fehler beim Laden der Stimmen.</ErrorWithRetry>
  <div v-else-if="groups !== undefined" class="flex flex-col gap-2 mt-2">
    <draggable v-model="userGroups" item-key="id" animation="150" filter="input" :preventOnFilter="false" tag="ul" handle=".group-handle" class="flex flex-col gap-2" @change="updateSortOrder">
      <template #item="{ element: group } : { element: EditableVoiceDefinitionUserGroup }">
        <li>
          <VoiceDefinitionGroupItem
            :group="group"
            :all-voice-definitions="allVoiceDefinitions"
            v-model:name="group.name"
            @delete="toggleDeleteGroup(group)"
            @add-voice-definition="addVoiceDefinition(group)"
            @delete-voice-definition="toggleDeleteVoiceDefinition(group, $event)"
            @reorder="updateSortOrder" />
        </li>
      </template>
    </draggable>
    <VoiceDefinitionGroupItem v-for="group in noGroups" :key="group.type"
      :group="group"
      :all-voice-definitions="allVoiceDefinitions"
      is-ungrouped
      @add-voice-definition="addVoiceDefinition(group)"
      @delete-voice-definition="toggleDeleteVoiceDefinition(group, $event)"
      @reorder="updateSortOrder" />
    <button class="!flex items-center gap-2 btn btn-green btn-solid self-start px-8! py-4!" @click="addGroup">
      <font-awesome-icon :icon="['fas', 'plus']" />
      <span>Neue Stimmgruppe</span>
    </button>
  </div>
</template>
