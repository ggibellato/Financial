import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { IncomeSourceDto } from '../../api/types'
import { selectActiveIncomeSources, useIncomeForm } from '../useIncomeForm'

const createIncomeMock = vi.fn<FinancialApiClient['createIncome']>()
const updateIncomeMock = vi.fn<FinancialApiClient['updateIncome']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    createIncome: createIncomeMock,
    updateIncome: updateIncomeMock,
  }),
}))

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: '1', name: 'Gleison', isActive: true, group: 'Salary', autoSplitToReserve: false },
  { id: '2', name: 'Ariana', isActive: true, group: 'Salary', autoSplitToReserve: true },
  { id: '3', name: 'Lottery', isActive: true, group: 'NonReportable', autoSplitToReserve: false },
  { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros', autoSplitToReserve: false },
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
      { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros', autoSplitToReserve: false },
      { id: '3', name: 'Lottery', isActive: false, group: 'NonReportable', autoSplitToReserve: false },
      { id: '2', name: 'Ariana', isActive: true, group: 'Salary', autoSplitToReserve: true },
      { id: '1', name: 'Gleison', isActive: true, group: 'Salary', autoSplitToReserve: false },
    ]

    const result = selectActiveIncomeSources(mixed)

    expect(result.map((s) => s.name)).toEqual(['Gleison', 'Ariana', 'DividendoJuros'])
  })

  it('defaults the new-income source to the first active option and leaves bank unselected once the form opens', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.createIncomeSource).toBe('1')
    expect(result.current.createIncomeBank).toBe('')
  })

  it('leaves the default source unset when there are no income sources', () => {
    const { result } = renderHook(() => useIncomeForm([], onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.createIncomeSource).toBe('')
  })

  it('submits a create request with a null bankId and null description when both are left blank', () => {
    createIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setCreateIncomeField('createIncomeDate', '2026-07-25'))
    act(() => result.current.setCreateIncomeField('createIncomeNetValue', '42.50'))

    act(() => result.current.submitCreateIncome())

    expect(createIncomeMock).toHaveBeenCalledWith(
      expect.objectContaining({ bankId: null, description: null }),
    )
  })

  it('submits a create request with the provided description', () => {
    createIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setCreateIncomeField('createIncomeDate', '2026-07-25'))
    act(() => result.current.setCreateIncomeField('createIncomeNetValue', '42.50'))
    act(() => result.current.setCreateIncomeField('createIncomeDescription', 'Chip ISA dividend'))

    act(() => result.current.submitCreateIncome())

    expect(createIncomeMock).toHaveBeenCalledWith(
      expect.objectContaining({ description: 'Chip ISA dividend' }),
    )
  })
})
