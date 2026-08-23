<script setup lang="ts">
import draggable from 'vuedraggable'
import type { EditableVoiceDefinition, EditableVoiceDefinitionGroup } from './VoiceDefinitionTypes'
import { joinStrings } from './UI'
import { computed } from 'vue';

const props = defineProps<{
  group: EditableVoiceDefinitionGroup
}>()

const name = defineModel<string>('name')

defineEmits<{
  'delete': []
  'deleteVoiceDefinition': [voiceDefinition: EditableVoiceDefinition]
  'addVoiceDefinition': []
  'reorder': []
}>()

const deleteGroup = computed(() => props.group.type === 'UserGroup' && props.group.delete)

const usageTitle = (voiceDefinition: EditableVoiceDefinition) =>
  voiceDefinition.compositions.length > 0
    ? `Verwendet in ${joinStrings(voiceDefinition.compositions.map(v => `"${v}"`))}`
    : 'Nicht verwendet'

const deleteGroupTitle = () =>
  props.group.voiceDefinitions.length > 0
    ? 'Gruppe enthält Stimmen. Bitte verschieben Sie diese zuerst in eine andere Gruppe.'
    : 'Gruppe löschen'
</script>

<template>
  <div class="border rounded p-2 flex flex-col gap-2" :class="{ 'opacity-50': deleteGroup }">
    <div class="flex items-center gap-2">
      <template v-if="group.type === 'UserGroup'">
        <button class="btn" :class="{ 'btn-solid btn-red': deleteGroup }"
          :disabled="group.voiceDefinitions.length > 0"
          :title="deleteGroupTitle()"
          @click="$emit('delete')">
          <font-awesome-icon :icon="['fas', 'trash-can']" />
        </button>
        <div class="w-6 text-center" :class="{ 'group-handle cursor-grab': !deleteGroup }">
          <font-awesome-icon :icon="['fas', 'up-down']" />
        </div>
        <input type="text" v-model="name" required placeholder="Gruppenname" :disabled="group.delete || group.isSaving" class="input-text" />
        <span v-if="group.saveErrors.length > 0" class="text-sm text-musi-red">{{ group.saveErrors.join(" ") }}</span>
        <span v-else-if="group.hasSavingFailed" class="text-sm text-musi-red">Fehler beim Speichern.</span>
      </template>
      <span v-else class="small-caps text-lg">Sonstige</span>
    </div>
    <draggable :list="group.voiceDefinitions" item-key="id" animation="150" filter="input" :preventOnFilter="false"
      tag="ul" handle=".voice-handle" :group="{ name: 'voice-definitions' }"
      class="flex flex-col gap-2"
      @change="$emit('reorder')">
      <template #item="{ element: voiceDefinition } : { element: EditableVoiceDefinition }">
        <li>
          <div class="flex items-center gap-2 rounded p-1" :class="{ 'opacity-50': voiceDefinition.delete }">
            <button class="btn" :class="{ 'btn-solid btn-red': voiceDefinition.delete }"
              :disabled="voiceDefinition.compositions.length > 0"
              :title="usageTitle(voiceDefinition)"
              @click="$emit('deleteVoiceDefinition', voiceDefinition)">
              <font-awesome-icon :icon="['fas', 'trash-can']" />
            </button>
            <div class="w-6 text-center" :class="{ 'voice-handle cursor-grab': !voiceDefinition.delete }">
              <font-awesome-icon :icon="['fas', 'up-down-left-right']" />
            </div>
            <div class="flex items-center gap-2">
              <input type="number" min="0" v-model="voiceDefinition.memberCount" class="input-text min-w-20! w-20" :disabled="voiceDefinition.delete || voiceDefinition.isSaving" />
              <font-awesome-icon :icon="['fas', 'xmark']" />
              <input type="text" v-model="voiceDefinition.name" required placeholder="Name" :disabled="voiceDefinition.delete || voiceDefinition.isSaving" class="input-text" />
            </div>
            <span v-if="voiceDefinition.saveErrors.length > 0" class="text-sm text-musi-red">{{ voiceDefinition.saveErrors.join(" ") }}</span>
            <span v-else-if="voiceDefinition.hasSavingFailed" class="text-sm text-musi-red">Fehler beim Speichern.</span>
          </div>
        </li>
      </template>
      <template #footer>
        <li>
          <button class="btn btn-green btn-solid h-full px-4!" title="Neue Stimme" @click="$emit('addVoiceDefinition')">
            <font-awesome-icon :icon="['fas', 'plus']" />
          </button>
        </li>
      </template>
    </draggable>
  </div>
</template>
