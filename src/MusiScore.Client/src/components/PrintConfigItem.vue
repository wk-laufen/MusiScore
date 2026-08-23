<script setup lang="ts">
import { computed, watch } from 'vue'
import TextInput from './TextInput.vue'
import ActivatableTextInput from './ActivatableTextInput.vue'
import LoadingBar from './LoadingBar.vue'
import type { EditablePrintConfig } from './PrintConfigTypes'
import type { PrintConfig } from './AdminTypes'
import SelectInput from './SelectInput.vue'
import { joinStrings } from './UI'

const props = defineProps<{
  printConfig: EditablePrintConfig
  replacementConfigs: PrintConfig[]
}>()

const emit = defineEmits<{
  (e: 'delete'): void
}>()

const name = defineModel<string>('name', { required: true })
const idIsReadOnly = defineModel<boolean>('idIsReadOnly', { required: true })
const id = defineModel<string>('id', { required: true })
watch([name, idIsReadOnly], ([newName, newIdIsReadOnly]) => {
  if (props.printConfig.isNew && newIdIsReadOnly) {
    id.value = newName
      .toLowerCase()
      .replace('ä', 'ae').replace('ö', 'oe').replace('ü', 'ue').replace('ß', 'ss')
      .replace(/[^a-zA-Z0-9]+/g, '-')
  }
})

const cupsCommandLineArgs = defineModel<string>('cupsCommandLineArgs', { required: true })
const reorderPagesAsBooklet = defineModel<boolean>('reorderPagesAsBooklet', { required: true })
const replacementConfigId = defineModel<string>('replacementConfigId', { required: true })

const isUsed = computed(() => props.printConfig.compositions.length > 0)

const usageTitle = computed(() =>
  `Verwendet in ${joinStrings(props.printConfig.compositions.map(composition => `"${composition.title}" (${joinStrings(composition.voices.map(v => `"${v}"`))})`))}`)
</script>

<template>
  <div class="grid border rounded-sm p-4" :class="{ 'border-musi-red': printConfig.hasSavingFailed }">
    <div class="col-span-full row-span-full w-72 flex flex-col gap-4" :class="{ 'opacity-50': printConfig.isSaving }">
      <fieldset :disabled="printConfig.isSaving || printConfig.delete" class="flex flex-col gap-4" :class="{ 'opacity-50': !printConfig.isSaving && printConfig.delete}">
        <ActivatableTextInput v-if="printConfig.isNew"
          title="Schlüssel"
          :placeholder="idIsReadOnly ? '<automatisch generieren>' : ''"
          :validation-state="printConfig.keyValidationState"
          :disabled="printConfig.keyIsReadOnly"
          v-model:text="id"
          v-model:is-active="idIsReadOnly" />
        <TextInput v-else
          title="Schlüssel"
          :validation-state="printConfig.keyValidationState"
          :disabled="printConfig.keyIsReadOnly"
          v-model="id" />
        <TextInput title="Anzeigename" :validation-state="printConfig.nameValidationState" v-model="name" />
        <TextInput :required="false" :validation-state="printConfig.cupsCommandLineArgsValidationState" v-model="cupsCommandLineArgs">
          <template v-slot:title>
            CUPS Kommandozeilenargumente (<a href="https://www.cups.org/doc/options.html" target="_blank" class="text-musi-blue">?</a>)
          </template>
        </TextInput>
        <label class="flex gap-2">
          <input type="checkbox" v-model="reorderPagesAsBooklet" />
          <span class="text-sm">Seiten für Broschürendruck anordnen</span>
        </label>
      </fieldset>
      <fieldset :disabled="printConfig.isSaving" class="flex flex-col gap-4">
        <button class="self-stretch btn btn-red !flex items-center justify-center gap-2" :class="{ 'btn-solid': printConfig.delete }" @click="emit('delete')">
          <span>Druckeinstellung löschen</span>
          <font-awesome-icon v-if="isUsed" :icon="['fas', 'info-circle']" :title="usageTitle" />
        </button>
        <SelectInput v-if="printConfig.delete && isUsed" title="Ersetzen durch" :options="replacementConfigs.map(v => ({ key: v.key, value: v.name}))" :validation-state="printConfig.replacementConfigIdValidationState" v-model="replacementConfigId" />
      </fieldset>
    </div>
    <LoadingBar v-if="printConfig.isSaving" type="minimal" class="col-span-full row-span-full" />
  </div>
</template>