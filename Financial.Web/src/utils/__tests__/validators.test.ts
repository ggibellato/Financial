import { afterEach, describe, expect, it, vi } from 'vitest'
import { pinDate } from '../../test-utils/pinDate'
import { futureDateError, paymentDateError } from '../validators'

describe('futureDateError', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('flags only a date after today', () => {
    pinDate('2026-09-17T12:00:00+01:00')

    expect(futureDateError('2026-09-18')).toBe('Date must not be in the future.')
    expect(futureDateError('2026-09-17')).toBeUndefined()
  })
})

describe('paymentDateError', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('asks for a date when there is none', () => {
    expect(paymentDateError('')).toBe('Enter a date.')
  })

  it('rejects a date after today and accepts today or earlier', () => {
    pinDate('2026-09-17T12:00:00+01:00')

    expect(paymentDateError('2026-09-18')).toBe('Date must not be in the future.')
    expect(paymentDateError('2026-09-17')).toBeUndefined()
    expect(paymentDateError('2025-01-01')).toBeUndefined()
  })
})
