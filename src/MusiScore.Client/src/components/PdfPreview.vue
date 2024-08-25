<script setup lang="ts">
import { ref, watch } from 'vue'
import { type PDFDocumentProxy, getDocument, GlobalWorkerOptions } from 'pdfjs-dist'
import { range } from 'lodash-es'
import PdfPage from './PdfPage.vue'
import pdfjsWorkerUrl from 'pdfjs-dist/build/pdf.worker.mjs?url'

GlobalWorkerOptions.workerSrc = pdfjsWorkerUrl

const props = defineProps<{
  file?: Uint8Array
}>()

const pdfDoc = ref<PDFDocumentProxy>()
watch(() => props.file, async (file, _, onCleanup) => {
  if (file === undefined) {
    pdfDoc.value = undefined
    return
  }
  
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
}, { immediate: true })

}
</script>

<template>
  <div v-if="pdfDoc !== undefined" class="flex gap-2 flex-wrap items-center">
    <PdfPage v-for="pageNumber in range(1, pdfDoc.numPages + 1)" :key="pageNumber" :pdf-doc="pdfDoc" :page-number="pageNumber" />
  </div>
</template>