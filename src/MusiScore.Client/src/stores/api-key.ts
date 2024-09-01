import { defineStore } from 'pinia'

export const useAPIKeyStore = defineStore('apiKey', {
  state: () : { apiKey: string | undefined } => ({ apiKey: localStorage.getItem('api-key') || undefined }),
  actions: {
    update(value: string, remember: boolean) {
      this.apiKey = value
      if (remember) {
        localStorage.setItem('api-key', this.apiKey)
      }
      else {
        localStorage.removeItem('api-key')
      }
    },
  },
})
