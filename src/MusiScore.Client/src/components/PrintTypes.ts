export type VoiceDto = {
  name: string
  printUrl: string
}

export type ActiveCompositionDto = {
  title: string
  voices: VoiceDto[]
}
