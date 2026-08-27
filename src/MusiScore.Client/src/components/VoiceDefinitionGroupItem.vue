<script setup lang="ts">
import draggable from 'vuedraggable'
import type { EditableVoiceDefinition, EditableVoiceDefinitionGroup } from './VoiceDefinitionTypes'
import VoiceDefinitionItem from './VoiceDefinitionItem.vue'
import { computed } from 'vue';
import { ungroupedName } from './VoiceGrouping';

const props = defineProps<{
  group: EditableVoiceDefinitionGroup
  /** every voice definition across all groups, so a deleted one can hand its voices to any other */
  allVoiceDefinitions: EditableVoiceDefinition[]
}>()

const name = defineModel<string>('name')

defineEmits<{
  'delete': []
  'deleteVoiceDefinition': [voiceDefinition: EditableVoiceDefinition]
  'addVoiceDefinition': []
  'reorder': []
}>()

const deleteGroup = computed(() => props.group.type === 'UserGroup' && props.group.delete)

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
      <span v-else class="small-caps text-lg">{{ ungroupedName }}</span>
    </div>
    <draggable :list="group.voiceDefinitions" item-key="id" animation="150" filter="input" :preventOnFilter="false"
      tag="ul" handle=".voice-handle" :group="{ name: 'voice-definitions' }"
      class="flex flex-col gap-2"
      @change="$emit('reorder')">
      <template #item="{ element: voiceDefinition } : { element: EditableVoiceDefinition }">
        <li>
          <VoiceDefinitionItem
            :voice-definition="voiceDefinition"
            :all-voice-definitions="allVoiceDefinitions"
            v-model:name="voiceDefinition.name"
            v-model:member-count="voiceDefinition.memberCount"
            v-model:replacement-id="voiceDefinition.replacementId"
            @delete="$emit('deleteVoiceDefinition', voiceDefinition)" />
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
