import { describe, expect, it } from 'vitest'
import { parseDueDay } from '../recurringBillRules'

describe('parseDueDay', () => {
  it.each([
    ['1', 1],
    ['31', 31],
  ])('accepts %s', (value, expected) => {
    expect(parseDueDay(value)).toBe(expected)
  })

  it.each(['', '0', '32', '1.5', 'abc'])('rejects "%s"', (value) => {
    expect(parseDueDay(value)).toBeNull()
  })
})
