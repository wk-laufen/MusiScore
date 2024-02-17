<script setup lang="ts">
import { ref, watch } from 'vue'
import { type PDFDocumentProxy, getDocument, GlobalWorkerOptions } from 'pdfjs-dist'
import { range } from 'lodash-es'
import PdfPage from './PdfPage.vue'

const props = defineProps<{
  file?: ArrayBuffer
}>()

const pdfDoc = ref<PDFDocumentProxy>()
const loadPDFDocument = async (file?: ArrayBuffer) =>
{
  if (file === undefined) {
    pdfDoc.value = undefined
    return
  }

  GlobalWorkerOptions.workerSrc = 'https://cdn.jsdelivr.net/npm/pdfjs-dist@4.0.379/build/pdf.worker.mjs' // TODO use local path
  pdfDoc.value = await getDocument(file).promise
}
watch(() => props.file, loadPDFDocument)

</script>

<template>
  <div v-if="pdfDoc !== undefined">
    <PdfPage v-for="pageNumber in range(1, pdfDoc.numPages)" :key="pageNumber" :pdf-doc="pdfDoc" :page-number="pageNumber" />
  </div>
</template>