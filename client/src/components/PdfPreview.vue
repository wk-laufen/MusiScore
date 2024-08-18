<script setup lang="ts">
import { nextTick, ref, watch, watchEffect } from 'vue'
import { type PDFDocumentProxy, getDocument, GlobalWorkerOptions } from 'pdfjs-dist'
import { range } from 'lodash-es'
import PdfPage from './PdfPage.vue'
import pdfjsWorkerUrl from 'pdfjs-dist/build/pdf.worker.mjs?url'
import { Pdf, type PdfModification } from './Pdf'

GlobalWorkerOptions.workerSrc = pdfjsWorkerUrl

const props = defineProps<{
  file?: Uint8Array
}>()

const pdfDoc = ref<PDFDocumentProxy>()
const loadPDFDocument = async (file?: Uint8Array) =>
{
  // clear preview first because pageNumber key is reused for a different page
  pdfDoc.value = undefined
  if (file === undefined) return

  await nextTick()
  pdfDoc.value = await getDocument(file.slice()).promise
}
watch(() => props.file, loadPDFDocument, { immediate: true })

const modifications = ref([] as PdfModification[])
// modifications.value.push({ type: 'zoom', pages: [1, 3], bounds: { x: 37, y: 81, width: 568-37, height: 729-81}})
// modifications.value.push({ type: 'rotate', pages: [1, 3], degrees: 1 })

const modifiedPdfDoc = ref<PDFDocumentProxy>()
watchEffect(async () => {
  if (props.file === undefined) return undefined

  const modifiedDocContent = await Pdf.applyModifications(props.file, modifications.value)
  modifiedPdfDoc.value = await getDocument(modifiedDocContent).promise
})
</script>

<template>
  <div v-if="modifiedPdfDoc !== undefined" class="flex gap-2 flex-wrap items-center">
    <PdfPage v-for="pageNumber in range(1, modifiedPdfDoc.numPages + 1)" :key="pageNumber" :pdf-doc="modifiedPdfDoc" :page-number="pageNumber" />
  </div>
</template>