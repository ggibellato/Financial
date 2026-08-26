import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { IncomeDto, IncomeSourceDto } from '../../api/types'
import { SPLIT_CONFIRMATION_DELAY_MS, selectActiveIncomeSources, useIncomeForm } from '../useIncomeForm'

const { createIncomeMock, updateIncomeMock } = vi.hoisted(() => ({
  createIncomeMock: vi.fn<FinancialApiClient['createIncome']>(),
  updateIncomeMock: vi.fn<FinancialApiClient['updateIncome']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    createIncome: createIncomeMock,
    updateIncome: updateIncomeMock,
  } as Partial<FinancialApiClient>,
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
    // shouldAdvanceTime keeps real timers ticking so RTL's waitFor polling still resolves
    // while the split-confirmation dismiss timeout is driven by fake time.
    vi.useFakeTimers({ shouldAdvanceTime: true })
  })

  afterEach(() => {
    vi.useRealTimers()
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

  it('defaults splitToReserve to false when the default source is ineligible', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.createIncomeSource).toBe('1') // Gleison - not eligible
    expect(result.current.createIncomeSplitToReserve).toBe('false')
  })

  it('switching to an eligible source sets splitToReserve to true', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())

    act(() => result.current.setCreateIncomeField('createIncomeSource', '2')) // Ariana

    expect(result.current.createIncomeSplitToReserve).toBe('true')
  })

  it('switching back to an ineligible source sets splitToReserve to false', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setCreateIncomeField('createIncomeSource', '2')) // Ariana

    act(() => result.current.setCreateIncomeField('createIncomeSource', '1')) // Gleison

    expect(result.current.createIncomeSplitToReserve).toBe('false')
  })

  it('populates editIncomeSplitToReserve as true for a split income', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    const income: IncomeDto = {
      id: 'i1', date: '2026-07-25', incomeSourceId: '2', incomeSourceName: 'Ariana',
      grossValue: null, netValue: 2450, bankId: null, bankName: null, description: null,
      splitToReserve: true,
    }

    act(() => result.current.showEditIncomeForm(income))

    expect(result.current.editIncomeSplitToReserve).toBe('true')
  })

  it('populates editIncomeSplitToReserve as false for an unsplit income', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    const income: IncomeDto = {
      id: 'i1', date: '2026-07-25', incomeSourceId: '2', incomeSourceName: 'Ariana',
      grossValue: null, netValue: 2450, bankId: null, bankName: null, description: null,
      splitToReserve: false,
    }

    act(() => result.current.showEditIncomeForm(income))

    expect(result.current.editIncomeSplitToReserve).toBe('false')
  })

  it('submits a create request with splitToReserve true when checked', () => {
    createIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setCreateIncomeField('createIncomeSource', '2')) // Ariana, defaults checked
    act(() => result.current.setCreateIncomeField('createIncomeDate', '2026-07-25'))
    act(() => result.current.setCreateIncomeField('createIncomeNetValue', '2450'))

    act(() => result.current.submitCreateIncome())

    expect(createIncomeMock).toHaveBeenCalledWith(expect.objectContaining({ splitToReserve: true }))
  })

  it('sets splitConfirmationMessage when the create response comes back split', async () => {
    createIncomeMock.mockResolvedValue({ splitToReserve: true } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setCreateIncomeField('createIncomeDate', '2026-07-25'))
    act(() => result.current.setCreateIncomeField('createIncomeNetValue', '2450'))

    await act(async () => {
      result.current.submitCreateIncome()
    })

    expect(result.current.splitConfirmationMessage).toBe('Income saved and split to reserve')
  })

  it('leaves splitConfirmationMessage null when the create response comes back unsplit', async () => {
    createIncomeMock.mockResolvedValue({ splitToReserve: false } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setCreateIncomeField('createIncomeDate', '2026-07-25'))
    act(() => result.current.setCreateIncomeField('createIncomeNetValue', '2450'))

    await act(async () => {
      result.current.submitCreateIncome()
    })

    expect(result.current.splitConfirmationMessage).toBeNull()
  })

  it('clears splitConfirmationMessage automatically after the delay', async () => {
    createIncomeMock.mockResolvedValue({ splitToReserve: true } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setCreateIncomeField('createIncomeDate', '2026-07-25'))
    act(() => result.current.setCreateIncomeField('createIncomeNetValue', '2450'))
    await act(async () => {
      result.current.submitCreateIncome()
    })
    expect(result.current.splitConfirmationMessage).toBe('Income saved and split to reserve')

    act(() => {
      vi.advanceTimersByTime(SPLIT_CONFIRMATION_DELAY_MS)
    })

    await waitFor(() => expect(result.current.splitConfirmationMessage).toBeNull())
  })
})
