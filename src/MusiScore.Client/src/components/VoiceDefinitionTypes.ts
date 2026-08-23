import type { UserGroupedVoiceDefinitions, VoiceDefinitionInputs, VoiceDefinitionWithStats } from "./AdminTypes"

export type EditableVoiceDefinition = VoiceDefinitionInputs & {
  loadedData: VoiceDefinitionInputs | undefined
  links: VoiceDefinitionWithStats['links']
  id: string
  isNew: boolean
  compositions: string[]
  isSaving: boolean
  hasSavingFailed: boolean
  saveErrors: string[]
}

export type VoiceDefinitionGroupInputs = {
  name: string
  sortOrder: number
  delete: boolean
}

export type EditableVoiceDefinitionUserGroup =
  { type: 'UserGroup' } & VoiceDefinitionGroupInputs & {
    loadedData: VoiceDefinitionGroupInputs | undefined
    links: UserGroupedVoiceDefinitions['links']
    /** client-side id, stable across saves */
    id: string
    /** id assigned by the server, `null` as long as the group hasn't been created yet */
    serverId: string | null
    isNew: boolean
    isSaving: boolean
    hasSavingFailed: boolean
    saveErrors: string[]
    voiceDefinitions: EditableVoiceDefinition[]
  }

export type EditableVoiceDefinitionNoGroup = {
  type: 'NoGroup'
  voiceDefinitions: EditableVoiceDefinition[]
}

export type EditableVoiceDefinitionGroup =
  EditableVoiceDefinitionUserGroup |
  EditableVoiceDefinitionNoGroup

export namespace EditableVoiceDefinitionGroup {
  export const getClientId = (group: EditableVoiceDefinitionGroup) => {
    switch (group.type) {
        case 'UserGroup': return group.id
        case 'NoGroup': return null
      }
  }
  export const getServerId = (group: EditableVoiceDefinitionGroup) => {
    switch (group.type) {
        case 'UserGroup': return group.serverId
        case 'NoGroup': return null
      }
  }
}
