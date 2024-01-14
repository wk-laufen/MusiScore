export type CompositionListItem = {
  title: string
  isActive: boolean
  links: {
    self: string
    voiceList: string
    voice: string
  }
}

export type Voice = {
  title: string
  file: Uint8Array
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
    voice: string
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
  file: Uint8Array
  printSetting: string
}
