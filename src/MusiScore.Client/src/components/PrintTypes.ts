export type VoiceDto = {
  name: string
  printUrl: string
}

export type CompositionDto = {
  title: string
  voices: VoiceDto[]
}

export type VoiceGroupDto = {
  groupName: string
  voices: string[]
}

export type CompositionList = {
  voices: VoiceGroupDto[]
  compositions: CompositionDto[]
}
