import { describe, expect, it } from 'vitest'
import type { CategoryDto } from '../../api/types'
import { defaultExpenseCategoryId, eligibleWithdrawalCategories } from '../withdrawalExpenseCategories'

const category = (id: string, name: string, overrides: Partial<CategoryDto> = {}): CategoryDto => ({
  id,
  name,
  active: true,
  isInvestment: false,
  isTithe: false,
  hasReferences: false,
  ...overrides,
})

describe('eligibleWithdrawalCategories', () => {
  it('keeps only active, non-investment categories that are not Reserva', () => {
    const result = eligibleWithdrawalCategories([
      category('1', 'Saude'),
      category('2', 'Old', { active: false }),
      category('3', 'Investimento', { isInvestment: true }),
      category('4', 'Reserva'),
      category('5', 'RESERVA'),
    ])

    expect(result.map((c) => c.name)).toEqual(['Saude'])
  })
})

describe('defaultExpenseCategoryId', () => {
  const eligible = [category('1', 'Ariana'), category('2', 'Saude')]

  it('matches the bucket name case-insensitively', () => {
    expect(defaultExpenseCategoryId(eligible, 'ARIANA')).toBe('1')
  })

  it('returns an empty id when no category matches', () => {
    expect(defaultExpenseCategoryId(eligible, 'HouseTreats')).toBe('')
  })
})
