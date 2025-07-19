<script setup lang="ts">
import { watch } from 'vue'
import TextInput from './TextInput.vue'
import ActivatableTextInput from './ActivatableTextInput.vue'
import LoadingBar from './LoadingBar.vue'
import type { EditablePrintConfig } from './PrintConfigTypes'

const props = defineProps<{
  printConfig: EditablePrintConfig
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
</script>

<template>
  <div class="grid border rounded-sm p-4" :class="{ 'border-musi-red': printConfig.hasSavingFailed }">
    <fieldset :disabled="printConfig.isSaving" class="col-span-full row-span-full" :class="{ 'opacity-50': printConfig.isSaving }">
      <div class="flex gap-2">
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
      </div>
      <TextInput title="Anzeigename" :validation-state="printConfig.nameValidationState" v-model="name" class="mt-4" />
      <TextInput :required="false" :validation-state="printConfig.cupsCommandLineArgsValidationState" v-model="cupsCommandLineArgs" class="mt-4">
        <template v-slot:title>
          CUPS Kommandozeilenargumente (<a href="https://www.cups.org/doc/options.html" target="_blank" class="text-musi-blue">?</a>)
        </template>
      </TextInput>
      <label class="flex gap-2 mt-4">
        <input type="checkbox" v-model="reorderPagesAsBooklet" />
        <span class="text-sm">Seiten für Broschürendruck anordnen</span>
      </label>
    </fieldset>
    <LoadingBar v-if="printConfig.isSaving" type="minimal" class="col-span-full row-span-full" />
  </div>
</template>