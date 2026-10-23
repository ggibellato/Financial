import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { ApiError } from '../../api/apiError'
import type { IncomeDto, IncomeSourceDto } from '../../api/types'
import { setStoredDefault } from '../../utils/createFormDefaults'
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
  { id: '1', name: 'Gleison', isActive: true, group: 'Salary', autoSplitToReserve: false, hasReferences: false },
  { id: '2', name: 'Ariana', isActive: true, group: 'Salary', autoSplitToReserve: true, hasReferences: false },
  { id: '3', name: 'Lottery', isActive: true, group: 'NonReportable', autoSplitToReserve: false, hasReferences: false },
  { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros', autoSplitToReserve: false, hasReferences: false },
]

describe('useIncomeForm', () => {
  let onSaved: () => void

  beforeEach(() => {
    createIncomeMock.mockReset()
    updateIncomeMock.mockReset()
    onSaved = vi.fn<() => void>()
    sessionStorage.clear()
    // shouldAdvanceTime keeps real timers ticking so RTL's waitFor polling still resolves
    // while the split-confirmation dismiss timeout is driven by fake time.
    vi.useFakeTimers({ shouldAdvanceTime: true })
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('selectActiveIncomeSources filters out inactive sources and orders the rest', () => {
    const mixed: IncomeSourceDto[] = [
      { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros', autoSplitToReserve: false, hasReferences: false },
      { id: '3', name: 'Lottery', isActive: false, group: 'NonReportable', autoSplitToReserve: false, hasReferences: false },
      { id: '2', name: 'Ariana', isActive: true, group: 'Salary', autoSplitToReserve: true, hasReferences: false },
      { id: '1', name: 'Gleison', isActive: true, group: 'Salary', autoSplitToReserve: false, hasReferences: false },
    ]

    const result = selectActiveIncomeSources(mixed)

    expect(result.map((s) => s.name)).toEqual(['Gleison', 'Ariana', 'DividendoJuros'])
  })

  it('defaults the new-income source to the first active option and leaves bank unselected once the form opens', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeSource).toBe('1')
    expect(result.current.incomeBank).toBe('')
  })

  it('leaves the default source unset when there are no income sources', () => {
    const { result } = renderHook(() => useIncomeForm([], onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeSource).toBe('')
  })

  it('submits a create request with a null bankId and null description when both are left blank', () => {
    createIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '42.50'))

    act(() => result.current.submitIncome())

    expect(createIncomeMock).toHaveBeenCalledWith(
      expect.objectContaining({ bankId: null, description: null }),
    )
  })

  it('submits a create request with the provided description', () => {
    createIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '42.50'))
    act(() => result.current.setIncomeField('description', 'Chip ISA dividend'))

    act(() => result.current.submitIncome())

    expect(createIncomeMock).toHaveBeenCalledWith(
      expect.objectContaining({ description: 'Chip ISA dividend' }),
    )
  })

  it('defaults splitToReserve to false when the default source is ineligible', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeSource).toBe('1')
    expect(result.current.incomeSplitToReserve).toBe('false')
  })

  it('switching to an eligible source sets splitToReserve to true', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())

    act(() => result.current.setIncomeField('incomeSource', '2'))

    expect(result.current.incomeSplitToReserve).toBe('true')
  })

  it('switching back to an ineligible source sets splitToReserve to false', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('incomeSource', '2'))

    act(() => result.current.setIncomeField('incomeSource', '1'))

    expect(result.current.incomeSplitToReserve).toBe('false')
  })

  it('populates incomeSplitToReserve as true for a split income', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    const income: IncomeDto = {
      id: 'i1', date: '2026-07-25', incomeSourceId: '2', incomeSourceName: 'Ariana',
      grossValue: null, netValue: 2450, bankId: null, bankName: null, description: null,
      splitToReserve: true,
    }

    act(() => result.current.showEditIncomeForm(income))

    expect(result.current.incomeSplitToReserve).toBe('true')
  })

  it('populates incomeSplitToReserve as false for an unsplit income', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    const income: IncomeDto = {
      id: 'i1', date: '2026-07-25', incomeSourceId: '2', incomeSourceName: 'Ariana',
      grossValue: null, netValue: 2450, bankId: null, bankName: null, description: null,
      splitToReserve: false,
    }

    act(() => result.current.showEditIncomeForm(income))

    expect(result.current.incomeSplitToReserve).toBe('false')
  })

  it('submits a create request with splitToReserve true when checked', () => {
    createIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('incomeSource', '2'))
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))

    act(() => result.current.submitIncome())

    expect(createIncomeMock).toHaveBeenCalledWith(expect.objectContaining({ splitToReserve: true }))
  })

  it('sets splitConfirmationMessage when the create response comes back split', async () => {
    createIncomeMock.mockResolvedValue({ splitToReserve: true } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))

    await act(async () => {
      result.current.submitIncome()
    })

    expect(result.current.splitConfirmationMessage).toBe('Income saved and split to reserve')
  })

  it('leaves splitConfirmationMessage null when the create response comes back unsplit', async () => {
    createIncomeMock.mockResolvedValue({ splitToReserve: false } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))

    await act(async () => {
      result.current.submitIncome()
    })

    expect(result.current.splitConfirmationMessage).toBeNull()
  })

  it('clears splitConfirmationMessage automatically after the delay', async () => {
    createIncomeMock.mockResolvedValue({ splitToReserve: true } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))
    await act(async () => {
      result.current.submitIncome()
    })
    expect(result.current.splitConfirmationMessage).toBe('Income saved and split to reserve')

    act(() => {
      vi.advanceTimersByTime(SPLIT_CONFIRMATION_DELAY_MS)
    })

    await waitFor(() => expect(result.current.splitConfirmationMessage).toBeNull())
  })

  it('persists date, bank, and income source after a successful create, for the next create form', async () => {
    createIncomeMock.mockResolvedValue({ splitToReserve: false } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('bank', 'bank-barclays'))
    act(() => result.current.setIncomeField('incomeSource', INCOME_SOURCES[1].id))
    act(() => result.current.setIncomeField('netValue', '2450'))
    await act(async () => {
      result.current.submitIncome()
    })

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeDate).toBe('2026-07-25')
    expect(result.current.incomeBank).toBe('bank-barclays')
    expect(result.current.incomeSource).toBe(INCOME_SOURCES[1].id)
  })

  it('always starts gross/net value and description blank on a new create form, even after a persisted save', async () => {
    createIncomeMock.mockResolvedValue({ splitToReserve: false } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))
    act(() => result.current.setIncomeField('description', 'Salary'))
    await act(async () => {
      result.current.submitIncome()
    })

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeNetValue).toBe('')
    expect(result.current.incomeGrossValue).toBe('')
    expect(result.current.incomeDescription).toBe('')
  })

  it('rejects a submit with a blank date, blank source, and a gross value below the net value, without calling the API', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', ''))
    act(() => result.current.setIncomeField('incomeSource', ''))
    act(() => result.current.setIncomeField('netValue', '100'))
    act(() => result.current.setIncomeField('grossValue', '50'))

    act(() => result.current.submitIncome())

    expect(createIncomeMock).not.toHaveBeenCalled()
    expect(result.current.saveIncomeErrorFields.date).toBe('Date is required')
    expect(result.current.saveIncomeErrorFields.incomeSource).toBe('Income source is required')
    expect(result.current.saveIncomeErrorFields.grossValue).toBe('Gross value must be at least the net value')
  })

  it('rejects a submit with a non-numeric net value, without calling the API', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', 'not-a-number'))

    act(() => result.current.submitIncome())

    expect(createIncomeMock).not.toHaveBeenCalled()
    expect(result.current.saveIncomeErrorFields.netValue).toBe('Net value must be a non-negative number')
  })

  it('cancelIncomeForm closes the form and resets its fields', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('description', 'Draft'))

    act(() => result.current.cancelIncomeForm())

    expect(result.current.isIncomeFormOpen).toBe(false)
    expect(result.current.incomeDescription).toBe('')
  })

  it('surfaces a create error returned by the backend', async () => {
    createIncomeMock.mockRejectedValue(new Error('Income source is inactive'))
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '42.50'))

    await act(async () => {
      result.current.submitIncome()
    })

    expect(result.current.saveIncomeError).toBe('Income source is inactive')
  })
})

describe('useIncomeForm account settings', () => {
  const LOCKED_CHASE = { id: 'bank-chase', name: 'Chase' }
  const SAVED: IncomeDto = {
    id: 'i9', date: '2026-10-14', incomeSourceId: '1', incomeSourceName: 'Gleison',
    grossValue: null, netValue: 100, bankId: 'bank-chase', bankName: 'Chase', description: 'Bonus',
    splitToReserve: false,
  }
  let onSaved: () => void

  beforeEach(() => {
    createIncomeMock.mockReset()
    updateIncomeMock.mockReset()
    onSaved = vi.fn<() => void>()
    sessionStorage.clear()
  })

  async function addIncome(result: { current: ReturnType<typeof useIncomeForm> }) {
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-10-14'))
    act(() => result.current.setIncomeField('grossValue', '120'))
    act(() => result.current.setIncomeField('netValue', '100'))
    act(() => result.current.setIncomeField('description', 'Bonus'))
    await act(async () => result.current.submitIncome())
  }

  it('opens with the locked bank and sends it in the request', async () => {
    createIncomeMock.mockResolvedValue(SAVED)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved, { lockedBank: LOCKED_CHASE }))

    await addIncome(result)

    expect(createIncomeMock).toHaveBeenCalledWith(expect.objectContaining({ bankId: 'bank-chase' }))
  })

  it('does not remember the locked bank as the default bank', async () => {
    createIncomeMock.mockResolvedValue(SAVED)
    const locked = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved, { lockedBank: LOCKED_CHASE }))
    await addIncome(locked.result)

    const monthly = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))
    act(() => monthly.result.current.showCreateIncomeForm())

    expect(monthly.result.current.incomeBank).toBe('')
  })

  it('keeps the form open after a create with the same date and source, clearing values and description', async () => {
    createIncomeMock.mockResolvedValue(SAVED)
    const { result } = renderHook(() =>
      useIncomeForm(INCOME_SOURCES, onSaved, { lockedBank: LOCKED_CHASE, keepOpenAfterSave: true }),
    )

    await addIncome(result)

    expect(onSaved).toHaveBeenCalledOnce()
    expect(result.current.isIncomeFormOpen).toBe(true)
    expect(result.current.incomeDate).toBe('2026-10-14')
    expect(result.current.incomeSource).toBe('1')
    expect(result.current.incomeBank).toBe('bank-chase')
    expect(result.current.incomeGrossValue).toBe('')
    expect(result.current.incomeNetValue).toBe('')
    expect(result.current.incomeDescription).toBe('')
    expect(result.current.isIncomeDirty).toBe(false)
  })

  it('closes the form after a create when keep-open is not set', async () => {
    createIncomeMock.mockResolvedValue(SAVED)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))

    await addIncome(result)

    expect(result.current.isIncomeFormOpen).toBe(false)
  })

  it('opens on today when viewing the current month and the last-used date is in another month', () => {
    vi.setSystemTime(new Date('2026-10-15T12:00:00+01:00'))
    setStoredDefault('income.date', '2026-08-30')
    const { result } = renderHook(() =>
      useIncomeForm(INCOME_SOURCES, onSaved, { lockedBank: LOCKED_CHASE, viewedMonth: { year: 2026, month: 10 } }),
    )

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeDate).toBe('2026-10-15')
  })

  it('closes the form after an edit even when keep-open is set', async () => {
    updateIncomeMock.mockResolvedValue(SAVED)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved, { keepOpenAfterSave: true }))

    act(() => result.current.showEditIncomeForm(SAVED))
    await act(async () => result.current.submitIncome())

    expect(result.current.isIncomeFormOpen).toBe(false)
  })

  it('keeps the values and shows the connection message when the save cannot reach the server', async () => {
    createIncomeMock.mockRejectedValue(new ApiError('Failed to fetch', 0))
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved, { lockedBank: LOCKED_CHASE }))

    await addIncome(result)

    expect(result.current.saveIncomeError).toBe('Could not save. Check the connection and try again.')
    expect(result.current.incomeDescription).toBe('Bonus')
    expect(result.current.isIncomeFormOpen).toBe(true)
  })

  it('reports dirty only while the values differ from those the form opened with', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, onSaved))

    act(() => result.current.showCreateIncomeForm())
    expect(result.current.isIncomeDirty).toBe(false)

    act(() => result.current.setIncomeField('netValue', '10'))
    expect(result.current.isIncomeDirty).toBe(true)
  })
})
