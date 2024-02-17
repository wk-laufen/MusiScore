<script setup lang="ts">
import { ref, toRaw, watch } from 'vue'
import { type PDFDocumentProxy, type PDFPageProxy } from 'pdfjs-dist'

const props = defineProps<{
    pdfDoc: PDFDocumentProxy
    pageNumber: number,
}>()

const page = ref<PDFPageProxy>()
const loadPage = async () =>
{
    page.value = await toRaw(props.pdfDoc).getPage(props.pageNumber);
}
loadPage()
const container = ref<HTMLCanvasElement>()
watch([page, container], async ([p,c]) =>
{
    if (p === undefined || c === undefined) return
    const drawingContext = c.getContext('2d')
    if (drawingContext === null) return

    const viewport = p.getViewport({ scale: 0.5 })

    const outputScale = window.devicePixelRatio || 1
    c.width = Math.floor(viewport.width * outputScale)
    c.height = Math.floor(viewport.height * outputScale)

    await toRaw(p).render({ canvasContext: drawingContext, viewport }).promise
})
</script>

<template>
    <canvas ref="container" class="border"></canvas>
</template>
