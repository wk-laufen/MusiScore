import { chunk } from "lodash-es"

export type CompositionListItem = {
  title: string
  isActive: boolean
  links: {
    self: string
    voices: string
  }
}

export type PrintSetting = {
  key: string
  name: string
}

export type Voice = {
  name: string
  file: string // base 64 encoded
  printSetting: string
  links: {
    self: string
  }
}

export type FullComposition = {
  title: string
  isActive: boolean
  links: {
    self: string
    voices?: string
  }
  voices: Voice[]
}

export type CompositionData = {
  title: string
  isActive: boolean
  voices: VoiceData[]
}

export type VoiceData = {
  title: string
  file: ArrayBuffer
  printSetting: string
}

export type SaveCompositionServerError = 'EmptyTitle'
export type VoiceFileServerError = 'EmptyFile' | 'InvalidFile'
export type SaveVoiceServerError = 'EmptyName' | VoiceFileServerError | 'UnknownPrintSetting'

export const deserializeFile = (text: string | undefined) => {
  if (text === undefined) return undefined
  return Uint8Array.from(atob(text), m => m.codePointAt(0) as number)
}

export function serializeFile (content: ArrayBuffer): string
export function serializeFile (content: undefined): undefined
export function serializeFile (content: ArrayBuffer | undefined) {
  if (content === undefined) return undefined

  const encodedContent = chunk(new Uint8Array(content), 0x10FFF) // see https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/fromCodePoint
    .map(chunk => String.fromCodePoint(...chunk))
    .reduce((a, b) => a + b)
  return btoa(encodedContent)
}
