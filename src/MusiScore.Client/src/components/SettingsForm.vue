<script setup lang="ts">
import { computed, ref } from 'vue'
import PrintSettingsForm from './PrintConfigsForm.vue'
import LoadButton from './LoadButton.vue'
import VoiceDefinitionForm from './VoiceDefinitionForm.vue'

defineProps<{
  printConfigsUrl: string
  voiceDefinitionsUrl: string
}>()

defineEmits<{
  'cancelEditSettings': []
}>()

const printSettingsFormRef = ref<InstanceType<typeof PrintSettingsForm>>()
const voiceDefinitionFormRef = ref<InstanceType<typeof VoiceDefinitionForm>>()

const canSave = computed(() => {
  return (printSettingsFormRef.value?.canSave || false) || (voiceDefinitionFormRef.value?.canSave || false)
})

const isSaving = ref(false)
const saveSettings = async () => {
  isSaving.value = true
  try {
    await Promise.all([
      printSettingsFormRef.value?.save() || Promise.resolve(),
      voiceDefinitionFormRef.value?.save() || Promise.resolve()
    ])
  }
  finally {
    isSaving.value = false
  }
}
</script>

<template>
  <div class="p-4">
    <h2 class="text-2xl small-caps">Einstellungen</h2>
    <PrintSettingsForm ref="printSettingsFormRef" :printConfigsUrl="printConfigsUrl" />
    <VoiceDefinitionForm ref="voiceDefinitionFormRef" :voiceDefinitionsUrl="voiceDefinitionsUrl" />
  </div>

  <Teleport to="#command-bar">
    <button class="btn btn-solid btn-gold px-8! py-4!" :disabled="isSaving" @click="$emit('cancelEditSettings')">Zurück zur Übersicht</button>
    <LoadButton :loading="isSaving" :disabled="!canSave" class="btn-solid btn-gold px-8! py-4!" @click="saveSettings">Einstellungen speichern</LoadButton>
  </Teleport>
</template>