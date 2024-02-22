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
    voice?: string
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

export type SaveCompositionServerError = { errorCode: "EmptyTitle" }
export type SaveVoiceServerError =
  | { errorCode: "EmptyName" }
  | { errorCode: "EmptyFile" }
  | { errorCode: "InvalidFile" }
  | { errorCode: "UnknownPrintSetting" }
export type SaveVoiceServerErrors = SaveVoiceServerError[]
