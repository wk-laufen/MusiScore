<script setup lang="ts">
import { ref, watch } from 'vue'
import { type PDFDocumentProxy, getDocument, GlobalWorkerOptions } from 'pdfjs-dist'
import { range } from 'lodash-es'
import PdfPage from './PdfPage.vue'
import pdfjsWorkerUrl from 'pdfjs-dist/build/pdf.worker.mjs?url'
import _ from 'lodash'

GlobalWorkerOptions.workerSrc = pdfjsWorkerUrl

const selectedPages = defineModel<readonly number[]>('selectedPages', { default: [] })

const props = withDefaults(
  defineProps<{
    file?: Uint8Array
    isRotating: boolean
  }>(),
  { isRotating: false }
)

const pdfDoc = ref<PDFDocumentProxy>()
const isLoadingDocument = ref(false)
watch(() => props.file, async (file, _, onCleanup) => {
  if (file === undefined) {
    pdfDoc.value = undefined
    return
  }
  
  try {
    isLoadingDocument.value = true
    const loadDocTask = getDocument(file.slice())
    onCleanup(() => loadDocTask.destroy())
    try {
      pdfDoc.value = await loadDocTask.promise
    }
    catch (e) {
      if (e instanceof Error && e.message === 'Worker was destroyed') {
        console.log('PDF document loading cancelled')
      }
      else {
        console.error('PDF document loading failed', e)
        // TODO notify user
      }
    }
  }
  finally {
    isLoadingDocument.value = false
  }
}, { immediate: true })

const switchPageSelection = (pageNumber: number) => {
  if (selectedPages.value.includes(pageNumber)) {
    selectedPages.value = _.without(selectedPages.value, pageNumber)
  }
  else {
    selectedPages.value = _.orderBy(selectedPages.value.concat(pageNumber))
  }
}
</script>

<template>
  <div v-if="pdfDoc !== undefined" class="flex gap-2 flex-wrap items-center">
    <PdfPage v-for="pageNumber in range(1, pdfDoc.numPages + 1)" :key="pageNumber"
      :pdf-doc="pdfDoc"
      :page-number="pageNumber"
      :is-loading-document="isLoadingDocument"
      :is-selected="selectedPages.includes(pageNumber - 1)"
      :is-rotating="isRotating"
      @switch-page-selection="switchPageSelection(pageNumber - 1)" />
  </div>
</template>