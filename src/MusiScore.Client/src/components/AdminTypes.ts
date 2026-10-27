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

/** compositions of active events are shown on the print page */
export type Event = {
  id: string
  name: string
  isActive: boolean
  links: {
    self: string
    /** append a composition id to add or remove a single composition */
    compositions: string
  }
}

export type EventSaveError = 'EmptyName' | 'DuplicateName'

/** event names usually start with a date, so the latest events come first */
export const sortEvents = (events: Event[]) => events.sort((a, b) => b.name.localeCompare(a.name))

export type CompositionListItem = {
  id: string
  title: string
  tags: ExistingTag[]
  eventIds: string[]
  links: {
    self: string
    voices: string
    print: string
  }
}

export type PrintConfig = {
  key: string
  name: string
}

export type VoicePrintSettings = {
  name: string
  count: number
}

export type VoicePrintResult = {
  voiceName: string
  result: 'Success' | 'PrintingFailed'
}

export type VoiceDefinition = {
  id: string
  name: string
  memberCount: number
  links: {
    self: string
  }
}

export type VoiceDefinitionWithStats = VoiceDefinition & {
  compositions: string[]
}

export type VoiceDefinitionGroup = {
  id: string
  name: string
  links: {
    self: string
  }
}

export type UserGroupedVoiceDefinitions =
  {
    type: 'UserGroup'
    id: string
    name: string
    voiceDefinitions: VoiceDefinitionWithStats[]
    links: {
      self: string
    }
  }

export type UngroupedVoiceDefinitions =
  {
    type: 'NoGroup'
    voiceDefinitions: VoiceDefinitionWithStats[]
  }

export type GroupedVoiceDefinition = UserGroupedVoiceDefinitions | UngroupedVoiceDefinitions

export type VoiceDefinitionSaveError = 'EmptyName' | 'DuplicateName' | 'UnknownGroup'
export type VoiceDefinitionDeleteError = 'InvalidReplacementVoiceDefinitionId'
export type VoiceDefinitionGroupSaveError = 'EmptyName' | 'DuplicateName'

export type VoiceDefinitionInputs = {
  name: string
  memberCount: number
  sortOrder: number
  /** client-side id of the containing group, `null` for ungrouped voice definitions */
  groupId: string | null
  delete: boolean
}

export type Voice = {
  names: string[]
  printConfig: string
  links: {
    self: string
    sheet: string
  }
}

export type CompositionTemplate = {
  title: string
  tags: ExistingTag[]
  voices: Voice[]
}

export type FullComposition = {
  title: string
  tags: ExistingTag[]
  links: {
    self: string
    voices: string
    print: string
  }
  voices: Voice[]
}

export type SaveCompositionServerError = 'EmptyTitle'
export type VoiceFileServerError = 'EmptyFile' | 'InvalidFile'
export type SaveVoiceServerError = 'EmptyName' | VoiceFileServerError | 'InvalidKey'

export function serializeFile (content: Uint8Array): string
export function serializeFile (content: undefined): undefined
export function serializeFile (content: Uint8Array | undefined) {
  if (content === undefined) return undefined

  const encodedContent = chunk(content, 0x10FFF) // see https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/fromCodePoint
    .map(chunk => String.fromCodePoint(...chunk))
    .reduce((a, b) => a + b)
  return btoa(encodedContent)
}
