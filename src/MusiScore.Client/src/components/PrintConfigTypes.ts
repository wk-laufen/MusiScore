import type { ValidationState } from './Validation'

export type PrintConfigDto = {
  key: string
  name: string
  sortOrder: number
  cupsCommandLineArgs: string
  reorderPagesAsBooklet: boolean
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
