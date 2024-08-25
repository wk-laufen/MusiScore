<script setup lang="ts">
import { ref, toRaw, watch } from 'vue'
import { RenderingCancelledException, type PDFDocumentProxy } from 'pdfjs-dist'

const props = defineProps<{
  pdfDoc: PDFDocumentProxy
  pageNumber: number,
}>()

const container = ref<HTMLCanvasElement>()
watch([() => props.pdfDoc, () => props.pageNumber, container], async ([pdfDoc, pageNumber, c], _, onCleanup) => {
  if (c === undefined) return

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
})
</script>

<template>
  <canvas ref="container" class="border"></canvas>
</template>
