<script lang="ts" setup>
import { computed, ref, watch } from 'vue'
import { Pdf, type PDFFile, type PdfModification } from './Pdf'
import _ from 'lodash'
import PdfPreview from './PdfPreview.vue'

const props = defineProps<{
  originalFile: Uint8Array
}>()

const emit = defineEmits<{
  extractPages: [doc: Uint8Array]
}>()

const fileModifications = defineModel<({ id: string; isDraft: boolean } & PdfModification)[]>('file-modifications', { default: [] })

const voiceFileWithModifications = ref<PDFFile>()
watch([() => props.originalFile, fileModifications], async ([newOriginalFile, newFileModifications]) => {
  if (newOriginalFile === undefined) {
    voiceFileWithModifications.value = undefined
    return
  }
  voiceFileWithModifications.value = await Pdf.applyModifications(newOriginalFile, newFileModifications)
}, { deep: true, immediate: true })

const selectedFilePages = ref([] as readonly number[])
const selectAllPages = () => {
  if (voiceFileWithModifications.value === undefined) {
    selectedFilePages.value = []
    return
  }
  selectedFilePages.value = _.range(0, voiceFileWithModifications.value.pageCount)
}

watch(() => voiceFileWithModifications.value?.pageCount, pageCount => {
  if (pageCount === undefined) {
    selectedFilePages.value = []
    return
  }
  selectedFilePages.value = selectedFilePages.value.filter(v => v < pageCount)
})

const lastFileModification = computed(() => fileModifications.value.at(-1))

watch(selectedFilePages, selectedPages => {
  if (lastFileModification.value?.isDraft) {
    lastFileModification.value.pages = selectedPages
  }
})

let nextModificationId = 1
const addVoiceFileModification = (modification: { isDraft: boolean } & PdfModification) => {
  fileModifications.value.push({ id: `${nextModificationId++}`, ...modification })
}

const extractPagesToNewVoice = async () => {
  if (voiceFileWithModifications.value === undefined) return

  addVoiceFileModification({ type: 'remove', pages: selectedFilePages.value, isDraft: false})

  const doc = await Pdf.extractPages(voiceFileWithModifications.value.data, selectedFilePages.value)
  emit('extractPages', doc)
}

const pagesToString = (pages: readonly number[]) => {
  const pageNumbers = _.orderBy(pages.map(v => v + 1))
  if (pageNumbers.length === 0) return `Keine Seiten`
  if (pageNumbers.length === 1) return `Seite ${pageNumbers[0]}`
  const ranges : { start: number, end: number }[] = []
  let currentRange : typeof ranges[0] | undefined = undefined
  for (const pageNumber of pageNumbers) {
    if (currentRange == undefined) {
      currentRange = { start: pageNumber, end: pageNumber }
    }
    else if (currentRange.end == pageNumber - 1) {
      currentRange.end = pageNumber
    }
    else {
      ranges.push(currentRange)
      currentRange = { start: pageNumber, end: pageNumber }
    }
  }
  if (currentRange !== undefined) {
    ranges.push(currentRange)
  }
  const rangesAsString = ranges.flatMap(v => {
    if (v.start == v.end) {
      return [`${v.start}`]
    }
    else if (v.start + 1 == v.end) {
      return [`${v.start}`, `${v.end}`]
    }
    else {
      return [`${v.start}-${v.end}`]
    }
  })
  const lastPage = rangesAsString.pop()
  return `Seiten ${rangesAsString.join(', ')} und ${lastPage}`
}
</script>

<template>
  <div class="flex flex-row flex-wrap gap-2">
    <button class="btn btn-blue" @click="selectAllPages()" :disabled="selectedFilePages.length === voiceFileWithModifications?.pageCount">Alle Seiten auswählen</button>
    <button class="btn btn-blue" @click="selectedFilePages = []" :disabled="selectedFilePages.length === 0">Seitenauswahl aufheben</button>
  </div>
  <div class="mt-2 flex flex-row flex-wrap gap-2">
    <button class="btn btn-green" @click="addVoiceFileModification({ type: 'scaleToA4', pages: selectedFilePages, isDraft: false })" :disabled="selectedFilePages.length === 0">Seitenformat auf A4 ändern</button>
    <button class="btn btn-green" @click="addVoiceFileModification({ type: 'zoom', pages: selectedFilePages, relativeBounds: { x: 0.01, y: 0.01, width: 0.98, height: 0.98 }, isDraft: true })" :disabled="selectedFilePages.length === 0">Zoomen</button>
    <button class="btn btn-green" @click="addVoiceFileModification({ type: 'remove', pages: selectedFilePages, isDraft: false})" :disabled="selectedFilePages.length === 0">Seiten entfernen</button>
    <button class="btn btn-green" @click="addVoiceFileModification({ type: 'rotatePage', pages: selectedFilePages, isDraft: false })" :disabled="selectedFilePages.length === 0">Seiten um 90° drehen</button>
    <button class="btn btn-green" @click="addVoiceFileModification({ type: 'rotateContent', pages: selectedFilePages, degrees: 0, isDraft: true })" :disabled="selectedFilePages.length === 0">Seiteninhalt drehen</button>
    <button class="btn btn-green" @click="addVoiceFileModification({ type: 'cutPageLeftRight', pages: selectedFilePages, isDraft: false })" :disabled="selectedFilePages.length === 0">Seiten in linke und rechte Hälfte teilen</button>
    <button class="btn btn-green" @click="addVoiceFileModification({ type: 'orderPages', pages: selectedFilePages, isDraft: false })" :disabled="selectedFilePages.length < 1">Seiten nach Markierungsreihenfolge sortieren</button>
    <button class="btn btn-green" @click="extractPagesToNewVoice()" :disabled="selectedFilePages.length === 0">Seiten in neue Stimme ausschneiden</button>
  </div>
  <ol class="mt-2 list-decimal list-inside">
    <li v-for="modification in fileModifications" :key="modification.id">
      <template v-if="modification.type === 'scaleToA4'">Seitenformat auf A4 ändern</template>
      <template v-else-if="modification.type === 'zoom'">
        <div class="inline-flex flex-row items-center gap-2">
          <span>{{ pagesToString(modification.pages) }} zoomen</span>
          <template v-if="modification.isDraft">
            <div class="grid grid-cols-3 grid-rows-4 gap-1">
              <a title="Ausschnitt nach oben bewegen" class="col-start-2 row-span-2 btn btn-blue" @click="modification.relativeBounds.y += 0.02"><font-awesome-icon :icon="['fas', 'arrow-up']" /></a>
              <a title="Ausschnitt nach unten bewegen" class="col-start-2 row-span-2 btn btn-blue" @click="modification.relativeBounds.y -= 0.02"><font-awesome-icon :icon="['fas', 'arrow-down']" /></a>
              <a title="Ausschnitt nach links bewegen" class="col-start-1 row-start-2 row-span-2 btn btn-blue" @click="modification.relativeBounds.x -= 0.02"><font-awesome-icon :icon="['fas', 'arrow-left']" /></a>
              <a title="Ausschnitt nach rechts bewegen" class="col-start-3 row-start-2 row-span-2 btn btn-blue" @click="modification.relativeBounds.x += 0.02"><font-awesome-icon :icon="['fas', 'arrow-right']" /></a>
            </div>
            <a title="Ausschnitt verkleinern" class="btn btn-blue" @click="modification.relativeBounds.x -= 0.01; modification.relativeBounds.y -= 0.01; modification.relativeBounds.width += 0.02; modification.relativeBounds.height += 0.02"><font-awesome-icon :icon="['fas', 'magnifying-glass-minus']" /></a>
            <a title="Ausschnitt vergrößern" class="btn btn-blue" @click="modification.relativeBounds.x += 0.01; modification.relativeBounds.y += 0.01; modification.relativeBounds.width -= 0.02; modification.relativeBounds.height -= 0.02"><font-awesome-icon :icon="['fas', 'magnifying-glass-plus']" /></a>
          </template>
        </div>
      </template>
      <template v-else-if="modification.type === 'remove'">
        <span>{{ pagesToString(modification.pages) }} entfernen</span>
      </template>
      <template v-else-if="modification.type === 'rotatePage'">
        <span>Inhalt von {{ pagesToString(modification.pages) }} um 90° drehen</span>
      </template>
      <template v-else-if="modification.type === 'rotateContent'">
        <span>{{ pagesToString(modification.pages) }} um
          <input v-if="modification.isDraft" class="input-text w-20!" type="number" step="0.1" v-model="modification.degrees">
          <template v-else>{{ modification.degrees }}</template>
          Grad drehen</span>
      </template>
      <template v-else-if="modification.type === 'cutPageLeftRight'">
        <span>{{ pagesToString(modification.pages) }} in linke und rechte Hälfte teilen</span>
      </template>
      <template v-else-if="modification.type === 'orderPages'">
        <span>{{ pagesToString(modification.pages) }} sortieren</span>
      </template>
      <a v-if="modification.isDraft" title="Änderung akzeptieren" class="ml-2 btn btn-green py-1! px-2!" @click="modification.isDraft = false">✔</a>
      <a v-if="modification === lastFileModification" title="Änderung verwerfen" class="ml-2 btn btn-red py-1! px-2!" @click="fileModifications.pop()">❌</a>
    </li>
  </ol>
  <PdfPreview
    :file="voiceFileWithModifications?.data"
    v-model:selected-pages="selectedFilePages"
    :is-rotating="lastFileModification?.type === 'rotateContent' && lastFileModification.isDraft"
    class="mt-6" />
</template>