import { describe, expect, it } from 'vitest'
import { isDuplicateNameConflict } from '../apiError'

describe('isDuplicateNameConflict', () => {
  it('matches a 409 saying the submitted name already exists, ignoring case', () => {
    expect(isDuplicateNameConflict(409, 'An account named "barclays" already exists.', 'Barclays')).toBe(true)
  })

  it('does not match a 409 that quotes the name for another reason', () => {
    expect(isDuplicateNameConflict(409, '"Barclays" has a pending statement payment.', 'Barclays')).toBe(false)
  })

  it('does not match a duplicate message for a different name', () => {
    expect(isDuplicateNameConflict(409, 'An account named "Monzo" already exists.', 'Barclays')).toBe(false)
  })

  it('does not match the same message with another status', () => {
    expect(isDuplicateNameConflict(400, 'An account named "Barclays" already exists.', 'Barclays')).toBe(false)
  })
})
