<script setup lang="ts">
import { ref } from 'vue'
import PrintSettingsForm from './PrintSettingsForm.vue'
import LoadButton from './LoadButton.vue'

defineProps<{
  printConfigsUrl: string
}>()

defineEmits<{
  'cancelEditSettings': []
}>()

const printSettingsFormRef = ref<typeof PrintSettingsForm>()
const isSaving = ref(false)
const saveSettings = async () => {
  isSaving.value = true
  try {
    await printSettingsFormRef.value?.save()
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
  </div>

  <Teleport to="#command-bar">
    <button class="btn btn-solid btn-gold !px-8 !py-4" :disabled="isSaving" @click="$emit('cancelEditSettings')">Zurück zur Übersicht</button>
    <LoadButton :loading="isSaving" :disabled="printSettingsFormRef === undefined || !printSettingsFormRef.canSave" class="btn-solid btn-gold !px-8 !py-4" @click="saveSettings">Einstellungen speichern</LoadButton>
  </Teleport>
</template>