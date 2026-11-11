import { describe, expect, it } from 'vitest'
import { ApiError, isDuplicateName } from '../apiError'

describe('isDuplicateName', () => {
  it('matches a 409 carrying the duplicate-name code, whatever its message says', () => {
    expect(isDuplicateName(new ApiError('That name is taken.', 409, { code: 'duplicateName' }))).toBe(true)
  })

  it('does not match a 409 without the code, even when its message reads like a duplicate', () => {
    expect(isDuplicateName(new ApiError('An account named "Barclays" already exists.', 409))).toBe(false)
  })

  it('does not match a 409 carrying another code', () => {
    expect(isDuplicateName(new ApiError('This withdrawal exceeds the balance.', 409, { code: 'overdraftConfirmationRequired' }))).toBe(false)
  })

  it('does not match the code with another status', () => {
    expect(isDuplicateName(new ApiError('That name is taken.', 400, { code: 'duplicateName' }))).toBe(false)
  })

  it('does not match an error that is not an API error', () => {
    expect(isDuplicateName(new Error('An account named "Barclays" already exists.'))).toBe(false)
  })
})
