import { type Ref } from 'vue'
import { useAPIKeyStore } from '@/stores/api-key'

type FetchResult = { succeeded: true, response: Response } | { succeeded: false, response?: Response }

export const uiFetch = async (isLoadingRef: Ref<boolean>, hasFailedRef: Ref<boolean>, fetchUrl: string, fetchParams?: RequestInit) : Promise<FetchResult> => {
  isLoadingRef.value = true
  hasFailedRef.value = false

  try {
    const response = await fetch(fetchUrl, fetchParams)
    if (!response.ok) {
      throw response
    }
    return { succeeded: true, response }
  }
  catch (e) {
    hasFailedRef.value = true
    return { succeeded: false, response: e instanceof Response ? e : undefined }
  }
  finally {
    isLoadingRef.value = false
  }
}

export const uiFetchAuthorized = async (isLoadingRef: Ref<boolean>, hasFailedRef: Ref<boolean>, fetchUrl: string, fetchParams?: RequestInit) : Promise<FetchResult> => {
  const apiKeyStore = useAPIKeyStore()
  const headers = apiKeyStore.apiKey !== undefined ? { Authorization: `APIKey ${apiKeyStore.apiKey}` } : undefined
  return await uiFetch(isLoadingRef, hasFailedRef, fetchUrl, { ...fetchParams, headers: { ...fetchParams?.headers, ...headers } })
}
