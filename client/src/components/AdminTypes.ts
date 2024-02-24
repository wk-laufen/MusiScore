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
export type SaveVoiceServerError = 'EmptyName' | 'EmptyFile' | 'InvalidFile' | 'UnknownPrintSetting'
