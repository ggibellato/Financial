export type FieldErrors<F extends string> = Partial<Record<F, string>>

export interface SaveErrorPayload<F extends string> {
  message: string | null
  fields: FieldErrors<F>
}

export interface InlineFormStatus<F extends string> {
  isFormVisible: boolean
  isSaving: boolean
  saveError: string | null
  saveErrorFields: FieldErrors<F>
}

export type InlineFormAction<F extends string> =
  | { type: 'CANCEL_FORM' }
  | { type: 'SET_FORM_FIELD'; payload: { field: F; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_ERROR'; payload: SaveErrorPayload<F> }

export function openInlineForm<F extends string, S extends InlineFormStatus<F>>(
  state: S,
  formValues: Partial<Omit<S, keyof InlineFormStatus<F>>>,
): S {
  return { ...state, ...formValues, isFormVisible: true, saveError: null, saveErrorFields: {}, isSaving: false }
}

export function snapshotFields<S, K extends keyof S>(state: S, fields: readonly K[]): Pick<S, K> {
  return Object.fromEntries(fields.map((field) => [field, state[field]])) as Pick<S, K>
}

export function hasChangedFields<S, K extends keyof S>(state: S, snapshot: Pick<S, K> | null, fields: readonly K[]): boolean {
  return snapshot !== null && fields.some((field) => state[field] !== snapshot[field])
}

export function reduceInlineForm<F extends string, S extends InlineFormStatus<F>>(
  state: S,
  action: InlineFormAction<F>,
  blankForm: Partial<S>,
): S {
  switch (action.type) {
    case 'CANCEL_FORM':
      return { ...state, ...blankForm }
    case 'SET_FORM_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null, saveErrorFields: {} }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload.message, saveErrorFields: action.payload.fields }
  }
}
