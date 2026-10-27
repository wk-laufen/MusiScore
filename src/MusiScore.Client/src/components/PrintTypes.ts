export type VoiceDto = {
  name: string
  printUrl: string
}

export type CompositionDto = {
  title: string
  voices: VoiceDto[]
}

export type VoiceGroupDto = {
  groupName: string | null
  voices: string[]
}

export type EventDto = {
  /** `null` if there are no events at all and therefore all compositions are shown */
  name: string | null
  compositions: CompositionDto[]
}

export type CompositionList = {
  voices: VoiceGroupDto[]
  /** sorted by name, oldest first */
  events: EventDto[]
}
