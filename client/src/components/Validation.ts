export type ValidationState =
  | { type: 'notValidated' }
  | { type: 'error'; error: string }
  | { type: 'success' }
