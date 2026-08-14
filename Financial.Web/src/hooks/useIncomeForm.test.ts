import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { BankDto, IncomeSourceDto } from '../api/types'
import { selectActiveIncomeSources, useIncomeForm } from './useIncomeForm'

const createIncomeMock = vi.fn<FinancialApiClient['createIncome']>()
const updateIncomeMock = vi.fn<FinancialApiClient['updateIncome']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    createIncome: createIncomeMock,
    updateIncome: updateIncomeMock,
  }),
}))

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true },
]

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: '1', name: 'Gleison', isActive: true, group: 'Salary' },
  { id: '2', name: 'Ariana', isActive: true, group: 'Salary' },
  { id: '3', name: 'Lottery', isActive: true, group: 'NonReportable' },
  { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros' },
]

describe('useIncomeForm', () => {
  let onSaved: () => void

  beforeEach(() => {
    createIncomeMock.mockReset()
    updateIncomeMock.mockReset()
    onSaved = vi.fn<() => void>()
  })

  it('selectActiveIncomeSources filters out inactive sources and orders the rest', () => {
    const mixed: IncomeSourceDto[] = [
      { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros' },
      { id: '3', name: 'Lottery', isActive: false, group: 'NonReportable' },
      { id: '2', name: 'Ariana', isActive: true, group: 'Salary' },
      { id: '1', name: 'Gleison', isActive: true, group: 'Salary' },
    ]

    const result = selectActiveIncomeSources(mixed)

    expect(result.map((s) => s.name)).toEqual(['Gleison', 'Ariana', 'DividendoJuros'])
  })

  it('defaults the new-income source and bank fields to the first active option once the form opens', () => {
    const { result } = renderHook(() => useIncomeForm(BANKS, INCOME_SOURCES, onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.createIncomeSource).toBe('1')
    expect(result.current.createIncomeBank).toBe('bank-barclays')
  })

  it('leaves the default source unset when there are no income sources', () => {
    const { result } = renderHook(() => useIncomeForm(BANKS, [], onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.createIncomeSource).toBe('')
  })
})
