import { readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'
import { findInAncestors } from '../../test-utils/findInAncestors'
import { computeRoundUpSuggestion, parseRoundUpAmount, roundsUp, suggestRoundUpAmount } from '../roundUp'

describe('computeRoundUpSuggestion', () => {
  const casesPath = findInAncestors('Tests/Financial.CashFlow.Domain.Tests/TestData/round-up-suggestion-cases.json', __dirname)
  const cases: { value: string; suggestion: string }[] = JSON.parse(readFileSync(casesPath, 'utf8'))

  it.each(cases.filter((c) => Number(c.value) > 0).map((c) => [c.value, c.suggestion]))(
    'matches the committed Domain case %s -> %s',
    (value, suggestion) => {
      expect(computeRoundUpSuggestion(Number(value)).toFixed(2)).toBe(suggestion)
    },
  )
})

const BANKS = [
  { id: 'barclays', name: 'Barclays', roundUpEnabled: false, isActive: true },
  { id: 'chase', name: 'Chase', roundUpEnabled: true, isActive: true },
]

describe('roundsUp', () => {
  it('is true only for a known bank with round-up enabled', () => {
    expect(roundsUp(BANKS, 'chase')).toBe(true)
    expect(roundsUp(BANKS, 'barclays')).toBe(false)
    expect(roundsUp(BANKS, 'unknown')).toBe(false)
  })
})

describe('suggestRoundUpAmount', () => {
  it('suggests up to the next whole unit for a round-up bank', () => {
    expect(suggestRoundUpAmount(BANKS, 'chase', '12.40')).toBe('0.60')
  })

  it.each([
    ['a bank without round-up', 'barclays', '12.40'],
    ['a blank amount', 'chase', ' '],
    ['an amount that is not a number', 'chase', 'abc'],
    ['a zero amount', 'chase', '0'],
    ['a negative amount', 'chase', '-5'],
  ])('suggests nothing for %s', (_, bankId, value) => {
    expect(suggestRoundUpAmount(BANKS, bankId, value)).toBeNull()
  })
})

describe('parseRoundUpAmount', () => {
  it.each([
    ['0', 0],
    ['0.99', 0.99],
    ['1', null],
    ['-0.01', null],
  ])('reads %s as %s', (value, expected) => {
    expect(parseRoundUpAmount(value)).toBe(expected)
  })
})
