export type VoiceDto = {
  name: string
  globalSortOrder: number
  printUrl: string
}

export type ActiveCompositionDto = {
  title: string
  voices: VoiceDto[]
}
