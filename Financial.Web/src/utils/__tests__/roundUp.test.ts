import { readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'
import { findInAncestors } from '../../test-utils/findInAncestors'
import { computeRoundUpSuggestion } from '../roundUp'

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
