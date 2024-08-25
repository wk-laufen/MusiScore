<script setup lang="ts">
import { computed, ref, toRaw, watch } from 'vue'
import { RenderingCancelledException, type PDFDocumentProxy } from 'pdfjs-dist'
import LoadingBar from './LoadingBar.vue'
import _ from 'lodash'

const props = withDefaults(
  defineProps<{
    pdfDoc: PDFDocumentProxy
    pageNumber: number
    isLoadingDocument: boolean
    isSelected: boolean
    isRotating: boolean
  }>(),
  { isSelected: false, isRotating: false }
)

defineEmits<{
  'switchPageSelection': []
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
  <div class="grid" @click="!isLoading && $emit('switchPageSelection')">
    <canvas ref="container" class="row-span-full col-span-full border" :class="{'border-musi-blue': isSelected}"></canvas>
    <div v-if="isLoading" class="row-span-full col-span-full bg-white opacity-50"></div>
    <LoadingBar v-if="isLoading" type="minimal" class="row-span-full col-span-full place-self-center"></LoadingBar>
    <div v-if="isSelected && isRotating" class="row-span-full col-span-full grid justify-items-stretch items-center">
      <div v-for="i in _.range(0, 100)" :key="i" class="h-px bg-musi-red"></div>
    </div>
  </div>
</template>
