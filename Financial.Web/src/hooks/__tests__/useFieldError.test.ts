import { describe, expect, it } from 'vitest'
import { useFieldError } from '../useFieldError'

describe('useFieldError', () => {
  it('returns the field-specific error when the field has one', () => {
    const fieldError = useFieldError<'amount' | 'date'>({ amount: 'Amount must be a positive number.' })

    expect(fieldError('amount')).toBe('Amount must be a positive number.')
  })

  it('returns null for a field that has no error', () => {
    const fieldError = useFieldError<'amount' | 'date'>({ amount: 'Amount must be a positive number.' })

    expect(fieldError('date')).toBeNull()
  })

  it('returns each field its own error when several fields are invalid at once', () => {
    const fieldError = useFieldError<'amount' | 'date'>({
      amount: 'Amount must be a positive number.',
      date: 'Date is required.',
    })

    expect(fieldError('amount')).toBe('Amount must be a positive number.')
    expect(fieldError('date')).toBe('Date is required.')
  })

  it('returns null for every field when there are no field errors', () => {
    const fieldError = useFieldError<'amount'>({})

    expect(fieldError('amount')).toBeNull()
  })
})
