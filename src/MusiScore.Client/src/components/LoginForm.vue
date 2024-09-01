<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{
  apiKey?: string
}>()

defineEmits<{
  login: [apiKey: string, remember: boolean]
}>()

const apiKey = ref(props.apiKey || '')
const rememberAPIKey = ref(false)

const errorMessage = props.apiKey !== undefined ? 'Login fehlgeschlagen' : undefined
</script>

<template>
  <div class="flex flex-col items-center gap-2">
    <div class="flex items-center justify-center gap-4">
      <input type="text" placeholder="Passwort" v-model="apiKey" class="input-text !w-96 h-12" />
      <a class="btn btn-blue" @click="$emit('login', apiKey, rememberAPIKey)">Login</a>
    </div>
    <label class="flex gap-2">
      <input type="checkbox" v-model="rememberAPIKey" />
      <span>Passwort merken</span>
    </label>
    <span v-if="errorMessage" class="text-musi-red">{{ errorMessage }}</span>
  </div>
</template>