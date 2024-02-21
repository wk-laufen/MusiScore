<script setup lang="ts">
import { ref, watch } from 'vue'
import { type PDFDocumentProxy, getDocument, GlobalWorkerOptions } from 'pdfjs-dist'
import { range } from 'lodash-es'
import PdfPage from './PdfPage.vue'
import pdfjsWorkerUrl from 'pdfjs-dist/build/pdf.worker.mjs?url'

GlobalWorkerOptions.workerSrc = pdfjsWorkerUrl

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

  pdfDoc.value = await getDocument(file).promise
}
watch(() => props.file, loadPDFDocument)

</script>

<template>
  <div v-if="pdfDoc !== undefined">
    <PdfPage v-for="pageNumber in range(1, pdfDoc.numPages)" :key="pageNumber" :pdf-doc="pdfDoc" :page-number="pageNumber" />
  </div>
</template>