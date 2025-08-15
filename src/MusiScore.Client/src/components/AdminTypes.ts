import { chunk } from "lodash-es"

export type ExistingTag = {
  key: string
  title: string
  settings: {
    valueType: "text" | "multi-line-text"
    overviewDisplayFormat: null | {
      order: 1
      format: string
    }
  }
  value: string | null
  otherValues: string[]
}

export type CompositionListItem = {
  title: string
  tags: ExistingTag[]
  isActive: boolean
  links: {
    self: string
    voices: string
  }
}

export type PrintConfig = {
  key: string
  name: string
}

export type VoiceDefinition = {
  name: string
  allowPublicPrint: boolean
}

export type VoiceDefinitionWithStats = {
  name: string
  allowPublicPrint: boolean
  compositions: string[]
  links: {
    self: string
  }
}

export type Voice = {
  name: string
  printConfig: string
  links: {
    self: string
    sheet: string
  }
}

export type CompositionTemplate = {
  title: string
  tags: ExistingTag[]
  isActive: boolean
  voices: Voice[]
}

export type FullComposition = {
  title: string
  tags: ExistingTag[]
  isActive: boolean
  links: {
    self: string
    voices?: string
  }
  voices: Voice[]
}

export type SaveCompositionServerError = 'EmptyTitle'
export type VoiceFileServerError = 'EmptyFile' | 'InvalidFile'
export type SaveVoiceServerError = 'EmptyName' | VoiceFileServerError | 'UnknownPrintConfig'

export function serializeFile (content: Uint8Array): string
export function serializeFile (content: undefined): undefined
export function serializeFile (content: Uint8Array | undefined) {
  if (content === undefined) return undefined

  const encodedContent = chunk(content, 0x10FFF) // see https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/fromCodePoint
    .map(chunk => String.fromCodePoint(...chunk))
    .reduce((a, b) => a + b)
  return btoa(encodedContent)
}
