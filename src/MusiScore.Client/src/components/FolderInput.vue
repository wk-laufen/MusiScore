<script setup lang="ts">
const model = defineModel<File[]>()

const selectedFolderChanged = (e: Event) => {
  model.value = [...(e.target as HTMLInputElement)?.files || []]
}
</script>

<template>
  <div>
    <label>
      <input class="input-file hidden" type="file" webkitdirectory multiple @change="selectedFolderChanged" />
      <a class="btn btn-blue">Ordner auswählen</a>
    </label>
  </div>
</template>
