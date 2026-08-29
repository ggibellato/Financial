import { describe, expect, it } from 'vitest'
import { useFieldError } from '../useFieldError'

describe('useFieldError', () => {
  it('returns the save error when it targets the given field', () => {
    const fieldError = useFieldError<'amount' | 'date'>('Amount must be a positive number.', 'amount')

    expect(fieldError('amount')).toBe('Amount must be a positive number.')
  })

  it('returns null for a field the save error does not target', () => {
    const fieldError = useFieldError<'amount' | 'date'>('Amount must be a positive number.', 'amount')

    expect(fieldError('date')).toBeNull()
  })

  it('returns null for every field when saveErrorField is null', () => {
    const fieldError = useFieldError<'amount'>('Network request failed', null)

    expect(fieldError('amount')).toBeNull()
  })
})
