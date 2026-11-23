import { describe, expect, it } from 'vitest'
import {
  hasSavingsErrors,
  newRateRow,
  sameSavings,
  toSavingsTermsDto,
  validateSavings,
  type SavingsFormValues,
} from '../savingsTermsForm'

const terms = (overrides: Partial<SavingsFormValues> = {}): SavingsFormValues => ({
  interestFrequency: 'Monthly',
  paymentDay: '5',
  isTaxFree: false,
  rates: [newRateRow('4.50', '2026-09-01')],
  ...overrides,
})

describe('savingsTermsForm', () => {
  it('accepts valid terms', () => {
    expect(hasSavingsErrors(validateSavings(terms()))).toBe(false)
  })

  it.each([
    ['', 'Enter the day interest is paid.'],
    ['0', 'Interest payment day must be between 1 and 31.'],
    ['32', 'Interest payment day must be between 1 and 31.'],
    ['5.5', 'Interest payment day must be between 1 and 31.'],
  ])('[AC P64-F04-savings-web-02] rejects payment day %j', (paymentDay, message) => {
    expect(validateSavings(terms({ paymentDay })).paymentDay).toBe(message)
  })

  it('needs no payment day when interest is paid daily', () => {
    expect(validateSavings(terms({ interestFrequency: 'Daily', paymentDay: '' })).paymentDay).toBeUndefined()
  })

  it.each([
    ['', 'Enter the rate as a number, for example 4.50.'],
    ['abc', 'Enter the rate as a number, for example 4.50.'],
    ['-0.01', 'Interest rate must be between 0% and 100%.'],
    ['100.01', 'Interest rate must be between 0% and 100%.'],
    ['4.567', 'Interest rate can have at most 2 decimal places.'],
    ['4.570', undefined],
    ['0', undefined],
    ['100', undefined],
  ])('[AC P64-F04-savings-web-02] checks rate %j', (annualPercent, message) => {
    expect(validateSavings(terms({ rates: [newRateRow(annualPercent, '2026-09-01')] })).rates[0].annualPercent).toBe(message)
  })

  it('[AC P64-F04-savings-web-02] needs a date on each rate, and flags a repeated date on the later row', () => {
    const errors = validateSavings(
      terms({ rates: [newRateRow('4', ''), newRateRow('4', '2026-09-01'), newRateRow('3', '2026-09-01')] }),
    )

    expect(errors.rates.map((rate) => rate.effectiveFrom)).toEqual([
      'Enter the date the rate takes effect.',
      undefined,
      "Two rates can't take effect on the same date.",
    ])
  })

  it('compares terms by what they send, not by row keys', () => {
    const original = terms()
    const reentered = terms({ rates: [newRateRow('4.50', '2026-09-01')] })

    expect(sameSavings(original, reentered)).toBe(true)
    expect(sameSavings(original, terms({ isTaxFree: true }))).toBe(false)
  })

  it('sends no payment day for daily interest', () => {
    expect(toSavingsTermsDto(terms({ interestFrequency: 'Daily', paymentDay: '' }))).toEqual({
      interestFrequency: 'Daily',
      paymentDay: null,
      isTaxFree: false,
      rates: [{ annualPercent: 4.5, effectiveFrom: '2026-09-01' }],
    })
  })
})
