<script setup lang="ts">
import { ref } from 'vue'
import LoadingBar from './LoadingBar.vue'
import ErrorWithRetry from './ErrorWithRetry.vue'
import uiFetch from './UIFetch'
import type { ActiveCompositionDto } from './PrintTypes'

const isLoading = ref(false)
const hasLoadingFailed = ref(false)
const compositionList = ref<ActiveCompositionDto[]>()
const loadCompositions = async () => {
  const result = await uiFetch(isLoading, hasLoadingFailed, '/api/print/compositions')
  if (result.succeeded) {
    compositionList.value = (await result.response.json() as ActiveCompositionDto[])
    compositionList.value.sort((a, b) => a.title.localeCompare(b.title))
  }
}
loadCompositions()

const selectedComposition = ref<ActiveCompositionDto>()
</script>

<template>
  <h1 class="text-3xl p-8 bg-musi-gold text-white small-caps">
    <font-awesome-icon class="mr-2" :icon="['fas', 'music']" />
    <span>MusiScore</span>
  </h1>
  <div class="grow overflow-y-auto m-4">
    <LoadingBar v-if="isLoading"></LoadingBar>
    <ErrorWithRetry v-else-if="hasLoadingFailed" @retry="loadCompositions">Fehler beim Laden.</ErrorWithRetry>
    <div v-else-if="compositionList !== undefined && selectedComposition === undefined" class="flex flex-col items-stretch m-4">
      <InfoNotification v-if="compositionList.length === 0">Keine Stücke vorhanden.</InfoNotification>
      <div class="flex flex-wrap items-stretch justify-center gap-2 m-4">
        <div v-for="composition in compositionList" :key="JSON.stringify(composition)"
          @click="() => selectedComposition = composition"
          class="flex items-stretch border rounded font-semibold text-blue-700 border-blue-500 divide-x divide-blue-500 cursor-pointer">
          <span class="grow flex items-center justify-center text-center !p-8 w-60">{{ composition.title }}</span>
        </div>
      </div>
    </div>
    <div v-else class="flex flex-col items-stretch m-4">
      <!-- voices -->
    </div>
  </div>
</template>
