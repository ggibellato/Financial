import { describe, expect, it } from 'vitest'
import { hasChangedFields, openInlineForm, reduceInlineForm, snapshotFields, type InlineFormStatus } from '../inlineFormSlice'

type Field = 'formDate' | 'formPrice'

interface FormState extends InlineFormStatus<Field> {
  editingDate: string | null
  formDate: string
  formPrice: string
  entries: string[]
}

const BLANK_FORM = {
  isFormVisible: false,
  editingDate: null,
  formDate: '',
  formPrice: '',
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
} as const

function buildState(overrides: Partial<FormState> = {}): FormState {
  return { ...BLANK_FORM, entries: ['kept'], ...overrides }
}

describe('openInlineForm', () => {
  it('shows the form with the given values and clears any previous save state', () => {
    const state = buildState({ isSaving: true, saveError: 'Failed', saveErrorFields: { formPrice: 'Bad' } })

    const opened = openInlineForm(state, { editingDate: '2026-01-02', formDate: '2026-01-02', formPrice: '10' })

    expect(opened).toEqual({
      isFormVisible: true,
      editingDate: '2026-01-02',
      formDate: '2026-01-02',
      formPrice: '10',
      isSaving: false,
      saveError: null,
      saveErrorFields: {},
      entries: ['kept'],
    })
  })
})

describe('reduceInlineForm', () => {
  it('CANCEL_FORM resets the form to the blank form and keeps the rest of the state', () => {
    const state = buildState({ isFormVisible: true, formDate: '2026-01-02', saveError: 'Failed' })

    expect(reduceInlineForm(state, { type: 'CANCEL_FORM' }, BLANK_FORM)).toEqual(buildState())
  })

  it('SET_FORM_FIELD sets only the named field', () => {
    const state = buildState({ formDate: '2026-01-02' })

    const next = reduceInlineForm(state, { type: 'SET_FORM_FIELD', payload: { field: 'formPrice', value: '12.5' } }, BLANK_FORM)

    expect(next.formPrice).toBe('12.5')
    expect(next.formDate).toBe('2026-01-02')
  })

  it('SAVE_START marks the form saving and clears the previous save error', () => {
    const state = buildState({ saveError: 'Failed', saveErrorFields: { formDate: 'Required' } })

    const next = reduceInlineForm(state, { type: 'SAVE_START' }, BLANK_FORM)

    expect(next.isSaving).toBe(true)
    expect(next.saveError).toBeNull()
    expect(next.saveErrorFields).toEqual({})
  })

  it('SAVE_ERROR stops saving and records the message and field errors', () => {
    const state = buildState({ isSaving: true, isFormVisible: true })

    const next = reduceInlineForm(
      state,
      { type: 'SAVE_ERROR', payload: { message: 'Price must be a positive number', fields: { formPrice: 'Price must be a positive number' } } },
      BLANK_FORM,
    )

    expect(next.isSaving).toBe(false)
    expect(next.isFormVisible).toBe(true)
    expect(next.saveError).toBe('Price must be a positive number')
    expect(next.saveErrorFields).toEqual({ formPrice: 'Price must be a positive number' })
  })
})

describe('form change tracking', () => {
  const fields = ['date', 'value'] as const

  it('snapshots only the listed fields', () => {
    expect(snapshotFields({ date: '2026-10-15', value: '1', isSaving: false }, fields)).toEqual({ date: '2026-10-15', value: '1' })
  })

  it('reports a change only while a listed field differs from the snapshot', () => {
    const snapshot = { date: '2026-10-15', value: '' }

    expect(hasChangedFields({ date: '2026-10-15', value: '', isSaving: true }, snapshot, fields)).toBe(false)
    expect(hasChangedFields({ date: '2026-10-15', value: '5', isSaving: false }, snapshot, fields)).toBe(true)
  })

  it('reports no change without a snapshot', () => {
    expect(hasChangedFields({ date: '2026-10-15', value: '5' }, null, fields)).toBe(false)
  })
})
