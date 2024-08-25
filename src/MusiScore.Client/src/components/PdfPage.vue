<script setup lang="ts">
import { computed, ref, toRaw, watch } from 'vue'
import { RenderingCancelledException, type PDFDocumentProxy } from 'pdfjs-dist'
import LoadingBar from './LoadingBar.vue'

const props = defineProps<{
  pdfDoc: PDFDocumentProxy
  pageNumber: number
  isLoadingDocument: boolean
}>()

const container = ref<HTMLCanvasElement>()
const isLoadingPage = ref(false)
watch([() => props.pdfDoc, () => props.pageNumber, container], async ([pdfDoc, pageNumber, c], _, onCleanup) => {
  if (c === undefined) return

  try {
    isLoadingPage.value = true
    const p = await toRaw(pdfDoc).getPage(pageNumber)
    const drawingContext = c.getContext('2d')
    if (drawingContext === null) return

    const viewport = p.getViewport({ scale: 1 })

    const outputScale = window.devicePixelRatio || 1
    c.width = Math.floor(viewport.width * outputScale)
    c.height = Math.floor(viewport.height * outputScale)

    const renderTask = toRaw(p).render({ canvasContext: drawingContext, viewport })
    onCleanup(() => renderTask.cancel())
    try {
      await renderTask.promise
    }
    catch (e) {
      if (e instanceof RenderingCancelledException) {
        console.log('Page rendering cancelled')
      }
      else {
        console.error(e)
        // TODO notify user
      }
    }
  }
  finally {
    isLoadingPage.value = false
  }
})

const isLoading = computed(() => props.isLoadingDocument || isLoadingPage.value)
</script>

<template>
  <div class="grid">
    <canvas ref="container" class="row-span-full col-span-full border"></canvas>
    <div v-if="isLoading" class="row-span-full col-span-full bg-white opacity-50"></div>
    <LoadingBar v-if="isLoading" type="minimal" class="row-span-full col-span-full place-self-center"></LoadingBar>
  </div>
</template>
