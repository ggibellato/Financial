import { describe, expect, it } from 'vitest'
import { ApiError, isBadRequest, isDuplicateName } from '../apiError'

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

describe('isBadRequest', () => {
  it('matches an API error with status 400', () => {
    expect(isBadRequest(new ApiError('There is no balance to carry.', 400))).toBe(true)
  })

  it('does not match another status or an error that is not an API error', () => {
    expect(isBadRequest(new ApiError('Conflict.', 409))).toBe(false)
    expect(isBadRequest(new Error('Bad request'))).toBe(false)
  })
})
