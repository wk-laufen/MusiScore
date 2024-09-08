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
}

export type EditablePrintConfig = PrintConfigDto & {
  loadedData: PrintConfigInputs | undefined
  id: string
  isNew: boolean
  keyValidationState: ValidationState
  keyIsReadOnly: boolean
  nameValidationState: ValidationState
  sortOrder: number
  cupsCommandLineArgsValidationState: ValidationState
  isSaving: boolean
  hasSavingFailed: boolean
}

export type PrintConfigSaveError = 'InvalidKey' | 'EmptyName' | 'PrintConfigExists'
