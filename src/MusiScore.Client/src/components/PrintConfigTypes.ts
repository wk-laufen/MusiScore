import type { ValidationState } from './Validation'

export type CompositionVoices = {
  title: string
  /** voice names in voice definition order */
  voices: string[]
}

export type PrintConfigDto = {
  key: string
  name: string
  sortOrder: number
  cupsCommandLineArgs: string
  reorderPagesAsBooklet: boolean
  /** compositions using this print config, most voices first */
  compositions: CompositionVoices[]
  links: {
    self: string
  }
}

export type PrintConfigInputs = {
  key: string
  name: string
  cupsCommandLineArgs: string
  reorderPagesAsBooklet: boolean
  sortOrder: number
  delete: boolean
}

export type EditablePrintConfig = PrintConfigDto & {
  loadedData: PrintConfigInputs | undefined
  id: string
  isNew: boolean
  keyValidationState: ValidationState
  keyIsReadOnly: boolean
  nameValidationState: ValidationState
  cupsCommandLineArgsValidationState: ValidationState
  delete: boolean
  replacementConfigId: string
  replacementConfigIdValidationState: ValidationState
  isSaving: boolean
  hasSavingFailed: boolean
}

export type PrintConfigSaveError = 'InvalidKey' | 'EmptyName' | 'PrintConfigExists'

export type PrintConfigDeleteError = 'InvalidReplacementConfigId'

export type PageOrientation = 'portrait' | 'landscape'

/** conditions that are `null` match any voice file */
export type PrintConfigRuleDto = {
  minPageCount: number
  maxPageCount: number | null
  /** either both or none of page width and height are set, independent of the orientation */
  pageWidthMm: number | null
  pageHeightMm: number | null
  orientation: PageOrientation | null
  printConfig: string
}

export type PrintConfigRuleInputs = {
  /** empty number inputs give `''` */
  minPageCount: number | ''
  maxPageCount: number | ''
  pageWidthMm: number | ''
  pageHeightMm: number | ''
  orientation: PageOrientation | ''
  printConfig: string
}

export type EditablePrintConfigRule = PrintConfigRuleInputs & {
  /** client-side id, stable across reordering */
  id: string
  isNew: boolean
  delete: boolean
  errors: string[]
}

export type PrintConfigRuleSaveError = {
  /** `null` if the error doesn't belong to a specific rule */
  index: number | null
  errors: ('InvalidPageCount' | 'InvalidPageCountRange' | 'InvalidPageSize' | 'IncompletePageSize' | 'InvalidOrientation' | 'InvalidKey' | 'UnknownPrintConfig')[]
}
