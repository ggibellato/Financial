import { describe, expect, it } from 'vitest'
import type { CategoryDto } from '../../api/types'
import { account, cardAccount, potsAccount } from '../../test/account'
import { accountCategoryIds, defaultExpenseCategoryId, eligibleWithdrawalCategories } from '../withdrawalExpenseCategories'

const category = (id: string, name: string, overrides: Partial<CategoryDto> = {}): CategoryDto => ({
  id,
  name,
  active: true,
  isInvestment: false,
  isTithe: false,
  hasReferences: false,
  ...overrides,
})

describe('accountCategoryIds', () => {
  it("collects any account's category, as the server's withdrawal rule refuses", () => {
    const result = accountCategoryIds([
      potsAccount({ id: 'p1', name: 'Reserva', categoryId: '4' }),
      account({ id: 'b1', name: 'Barclays', categoryId: '8' }),
      cardAccount({ id: 'c1', name: 'Amex' }),
    ])

    expect(result).toEqual(['4', '8'])
  })
})

describe('eligibleWithdrawalCategories', () => {
  it('keeps only active, non-investment categories that no Pots account uses', () => {
    const result = eligibleWithdrawalCategories(
      [
        category('1', 'Saude'),
        category('2', 'Old', { active: false }),
        category('3', 'Investimento', { isInvestment: true }),
        category('4', 'Reserva'),
        category('5', 'Mercado'),
        category('6', 'Holidays'),
      ],
      ['4', '6'],
    )

    expect(result.map((c) => c.name)).toEqual(['Saude', 'Mercado'])
  })

  it('[AC P60-F09-pots-template-and-reserva-migration-04] excludes by id after a rename, and offers a category named Reserva that no Pots account uses', () => {
    const result = eligibleWithdrawalCategories([category('4', 'Savings'), category('7', 'Reserva')], ['4'])

    expect(result.map((c) => c.name)).toEqual(['Reserva'])
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
