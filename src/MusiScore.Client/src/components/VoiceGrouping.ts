import type { GroupedVoiceDefinition, VoiceDefinitionWithStats } from './AdminTypes'

export const ungroupedName = 'Sonstige'

export type VoiceGroup<T> = {
  name: string
  voiceDefinitions: VoiceDefinitionWithStats[]
  voices: T[]
}

/** Buckets voices into the group of their voice definition, empty groups omitted. */
export const groupVoices = <T>(
  voices: T[],
  definitionGroups: GroupedVoiceDefinition[],
  voiceNames: (voice: T) => string[]
) : VoiceGroup<T>[] => {
  // a voice covering names from several groups shows up in each of them
  const belongsTo = (voice: T, group: GroupedVoiceDefinition) =>
    group.voiceDefinitions.some(v => voiceNames(voice).includes(v.name))

  return definitionGroups.map(group => {
    switch (group.type) {
      case 'UserGroup':
        return {
          name: group.name,
          voiceDefinitions: group.voiceDefinitions,
          voices: voices.filter(voice => belongsTo(voice, group))
        }
      case 'NoGroup':
        return {
          name: ungroupedName,
          voiceDefinitions: group.voiceDefinitions,
          voices: [
            ...voices.filter(voice => belongsTo(voice, group)),
            // a voice that is still unnamed matches no definition and would drop out of the list otherwise
            ...voices.filter(voice => !definitionGroups.some(v => belongsTo(voice, v)))
          ]
        }
    }
  }).filter(v => v.voices.length > 0)
}

/** The name of a group that defines a single voice of the same name adds nothing. */
export const showGroupTitle = <T>(group: VoiceGroup<T>) =>
  group.voiceDefinitions.length > 1 ||
  (group.voiceDefinitions.length === 1 && group.voiceDefinitions[0].name !== group.name)
