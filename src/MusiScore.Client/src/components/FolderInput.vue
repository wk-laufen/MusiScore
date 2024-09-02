<script setup lang="ts">
const model = defineModel<File[]>()

defineProps<{
  disabled: boolean
}>()

const selectedFolderChanged = (e: Event) => {
  model.value = [...(e.target as HTMLInputElement)?.files || []]
}
</script>

<template>
  <div>
    <label>
      <input class="input-file hidden" type="file" :disabled="disabled" webkitdirectory multiple @change="selectedFolderChanged" />
      <button :disabled="disabled" class="btn btn-blue">Ordner auswählen</button>
    </label>
  </div>
</template>
