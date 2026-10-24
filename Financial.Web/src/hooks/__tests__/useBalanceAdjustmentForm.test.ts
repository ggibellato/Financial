import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { ApiError } from '../../api/apiError'
import type { BalanceAdjustmentDto, BankDto } from '../../api/types'
import type { BankTotal } from '../useMonthly'
import { useBalanceAdjustmentForm } from '../useBalanceAdjustmentForm'
import { pinDate } from '../../test-utils/pinDate'
import { getStoredDefault, setStoredDefault } from '../../utils/createFormDefaults'

const { createBalanceAdjustmentMock, updateBalanceAdjustmentMock } = vi.hoisted(() => ({
  createBalanceAdjustmentMock: vi.fn<FinancialApiClient['createBalanceAdjustment']>(),
  updateBalanceAdjustmentMock: vi.fn<FinancialApiClient['updateBalanceAdjustment']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    createBalanceAdjustment: createBalanceAdjustmentMock,
    updateBalanceAdjustment: updateBalanceAdjustmentMock,
  } as Partial<FinancialApiClient>,
}))

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: true },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: true },
]

const BANK_TOTALS: BankTotal[] = [
  { bankId: 'bank-barclays', bank: 'Barclays', currency: 'GBP', balance: 100, roundUpTotal: 0 },
  { bankId: 'bank-trading212', bank: 'Trading212', currency: 'GBP', balance: 8.8, roundUpTotal: 0.6 },
]

const ADJUSTMENT: BalanceAdjustmentDto = {
  id: 'a1',
  date: '2026-07-20',
  bankId: 'bank-barclays',
  bankName: 'Barclays',
  targetBalance: 150,
  delta: 50,
  note: 'Matched statement',
}

describe('useBalanceAdjustmentForm', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  beforeEach(() => {
    createBalanceAdjustmentMock.mockReset()
    updateBalanceAdjustmentMock.mockReset()
    sessionStorage.clear()
  })

  it('starts closed', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))

    expect(result.current.isOpen).toBe(false)
  })

  it('openCreateForm defaults date to today and opens with no bank pre-selected, when nothing was persisted yet', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))

    pinDate('2026-07-01T00:30:00+01:00')
    act(() => result.current.openCreateForm())

    expect(result.current.isOpen).toBe(true)
    expect(result.current.isEditing).toBe(false)
    expect(result.current.bankName).toBe('')
    expect(result.current.currentBalance).toBe(0)
    expect(result.current.date).toBe('2026-07-01')
    expect(result.current.savedDelta).toBeNull()
  })

  it('openCreateForm resolves currentBalance from bankTotals on bank selection', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))
    act(() => result.current.openCreateForm())

    act(() => result.current.setField('bankName', 'bank-trading212'))

    expect(result.current.bankName).toBe('bank-trading212')
    expect(result.current.currentBalance).toBe(8.8)
  })

  it('resolves currentBalance to 0 when the chosen bank has no matching BankTotal', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))
    act(() => result.current.openCreateForm())

    act(() => result.current.setField('bankName', 'UnknownBank'))

    expect(result.current.currentBalance).toBe(0)
  })

  it('openEditForm pre-fills bank/currentBalance/targetBalance/date/note from the given adjustment', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))

    act(() => result.current.openEditForm(ADJUSTMENT))

    expect(result.current.isOpen).toBe(true)
    expect(result.current.isEditing).toBe(true)
    expect(result.current.bankName).toBe('bank-barclays')
    expect(result.current.currentBalance).toBe(100)
    expect(result.current.date).toBe('2026-07-20')
    expect(result.current.targetBalance).toBe('150')
    expect(result.current.note).toBe('Matched statement')
  })

  it('cancel resets the form to closed and clears savedDelta', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))
    act(() => result.current.openEditForm(ADJUSTMENT))

    act(() => result.current.cancel())

    expect(result.current.isOpen).toBe(false)
    expect(result.current.savedDelta).toBeNull()
  })

  it('submit is a no-op when no bank has been chosen', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))
    act(() => result.current.openCreateForm())

    act(() => result.current.submit())

    expect(result.current.saveError).toBeNull()
    expect(createBalanceAdjustmentMock).not.toHaveBeenCalled()
  })

  it('submit blocks with an error when the target balance is missing', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))
    act(() => result.current.openCreateForm())
    act(() => result.current.setField('bankName', 'bank-barclays'))

    act(() => result.current.submit())

    expect(result.current.saveErrorFields).toHaveProperty('targetBalance')
    expect(createBalanceAdjustmentMock).not.toHaveBeenCalled()
  })

  it('submit blocks with an error when the target balance is negative', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))
    act(() => result.current.openCreateForm())
    act(() => result.current.setField('bankName', 'bank-barclays'))
    act(() => result.current.setField('targetBalance', '-1'))

    act(() => result.current.submit())

    expect(result.current.saveErrorFields).toHaveProperty('targetBalance')
    expect(createBalanceAdjustmentMock).not.toHaveBeenCalled()
  })

  it('submit calls createBalanceAdjustment, sets isSaving, and sets savedDelta on success while staying open', async () => {
    let resolveCreate!: (value: BalanceAdjustmentDto) => void
    createBalanceAdjustmentMock.mockReturnValue(new Promise((resolve) => (resolveCreate = resolve)))
    const onSaved = vi.fn()
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, onSaved))
    act(() => result.current.openCreateForm())
    act(() => result.current.setField('bankName', 'bank-barclays'))
    act(() => result.current.setField('targetBalance', '150'))

    act(() => result.current.submit())

    expect(result.current.isSaving).toBe(true)
    expect(createBalanceAdjustmentMock).toHaveBeenCalledWith('bank-barclays', {
      date: expect.any(String),
      targetBalance: 150,
      note: null,
    })

    await act(async () => {
      resolveCreate({ ...ADJUSTMENT, delta: -4.2 })
      await Promise.resolve()
    })

    expect(result.current.isSaving).toBe(false)
    expect(result.current.isOpen).toBe(true)
    expect(result.current.savedDelta).toBe(-4.2)
    expect(onSaved).toHaveBeenCalledOnce()
  })

  it('submit calls updateBalanceAdjustment when editing', async () => {
    updateBalanceAdjustmentMock.mockResolvedValue(ADJUSTMENT)
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))
    act(() => result.current.openEditForm(ADJUSTMENT))
    act(() => result.current.setField('targetBalance', '120'))

    await act(async () => {
      result.current.submit()
      await Promise.resolve()
    })

    expect(updateBalanceAdjustmentMock).toHaveBeenCalledWith('bank-barclays', 'a1', {
      date: '2026-07-20',
      targetBalance: 120,
      note: 'Matched statement',
    })
  })

  it('sets saveError and saveErrorFields from a failed request', async () => {
    createBalanceAdjustmentMock.mockRejectedValue(new ApiError('Balance cannot be negative.', 400))
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))
    act(() => result.current.openCreateForm())
    act(() => result.current.setField('bankName', 'bank-barclays'))
    act(() => result.current.setField('targetBalance', '150'))

    await act(async () => {
      result.current.submit()
      await Promise.resolve()
      await Promise.resolve()
    })

    expect(result.current.isSaving).toBe(false)
    expect(result.current.saveError).toBe('Balance cannot be negative.')
    expect(result.current.saveErrorFields).toHaveProperty('targetBalance')
    expect(result.current.isOpen).toBe(true)
  })

  it('persists date and bank after a successful create, for the next create form', async () => {
    createBalanceAdjustmentMock.mockResolvedValue({ ...ADJUSTMENT, delta: 8.8 })
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))
    act(() => result.current.openCreateForm())
    act(() => result.current.setField('date', '2026-07-25'))
    act(() => result.current.setField('bankName', 'bank-trading212'))
    act(() => result.current.setField('targetBalance', '20'))

    await act(async () => {
      result.current.submit()
      await Promise.resolve()
    })
    act(() => result.current.cancel())

    act(() => result.current.openCreateForm())

    expect(result.current.date).toBe('2026-07-25')
    expect(result.current.bankName).toBe('bank-trading212')
    expect(result.current.currentBalance).toBe(8.8)
  })

  it('falls back to no bank pre-selected when the persisted bank no longer exists', () => {
    sessionStorage.setItem('financial.createFormDefault.balanceAdjustment.bank', 'bank-deleted')
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))

    act(() => result.current.openCreateForm())

    expect(result.current.bankName).toBe('')
    expect(result.current.currentBalance).toBe(0)
  })

  it('[AC P60-F06-admin-accounts-page-05] falls back to no bank pre-selected when the persisted bank is inactive', () => {
    setStoredDefault('balanceAdjustment.bank', 'bank-barclays')
    const banks = [{ ...BANKS[0], isActive: false }, BANKS[1]]
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, banks, vi.fn()))

    act(() => result.current.openCreateForm())

    expect(result.current.bankName).toBe('')
  })

  it('always starts target balance and note blank on a new create form, even after a persisted save', async () => {
    createBalanceAdjustmentMock.mockResolvedValue({ ...ADJUSTMENT, delta: 50 })
    const { result } = renderHook(() => useBalanceAdjustmentForm(BANK_TOTALS, BANKS, vi.fn()))
    act(() => result.current.openCreateForm())
    act(() => result.current.setField('bankName', 'bank-barclays'))
    act(() => result.current.setField('targetBalance', '150'))
    act(() => result.current.setField('note', 'Matched statement'))

    await act(async () => {
      result.current.submit()
      await Promise.resolve()
    })
    act(() => result.current.cancel())

    act(() => result.current.openCreateForm())

    expect(result.current.targetBalance).toBe('')
    expect(result.current.note).toBe('')
  })
})

describe('useBalanceAdjustmentForm account settings', () => {
  const LOCKED_CHASE = { id: 'bank-chase', name: 'Chase' }
  const CHASE_BALANCE = [{ bankId: 'bank-chase', bank: 'Chase', balance: 412.5 }]

  afterEach(() => {
    vi.useRealTimers()
  })

  beforeEach(() => {
    createBalanceAdjustmentMock.mockReset()
    sessionStorage.clear()
  })

  it('opens on the locked bank with its passed-in balance, ignoring the last-used bank', () => {
    setStoredDefault('balanceAdjustment.bank', 'bank-barclays')
    const { result } = renderHook(() => useBalanceAdjustmentForm(CHASE_BALANCE, BANKS, vi.fn(), { lockedBank: LOCKED_CHASE }))

    act(() => result.current.openCreateForm())

    expect(result.current.bankName).toBe('bank-chase')
    expect(result.current.bankDisplayName).toBe('Chase')
    expect(result.current.currentBalance).toBe(412.5)
  })

  it('creates on the locked bank and does not store it as the next default bank', async () => {
    createBalanceAdjustmentMock.mockResolvedValue({ ...ADJUSTMENT, bankId: 'bank-chase', delta: -12.5 })
    const onSaved = vi.fn()
    const { result } = renderHook(() => useBalanceAdjustmentForm(CHASE_BALANCE, BANKS, onSaved, { lockedBank: LOCKED_CHASE }))

    act(() => result.current.openCreateForm())
    act(() => result.current.setField('targetBalance', '400'))
    await act(async () => result.current.submit())

    expect(createBalanceAdjustmentMock).toHaveBeenCalledWith('bank-chase', expect.objectContaining({ targetBalance: 400 }))
    expect(getStoredDefault('balanceAdjustment.bank')).toBeNull()
    expect(result.current.savedDelta).toBe(-12.5)
    expect(result.current.isDirty).toBe(false)
    expect(onSaved).toHaveBeenCalledOnce()
  })

  it('opens on the last day of a past viewed month', () => {
    pinDate('2026-10-15T12:00:00+01:00')
    const { result } = renderHook(() =>
      useBalanceAdjustmentForm(CHASE_BALANCE, BANKS, vi.fn(), { lockedBank: LOCKED_CHASE, viewedMonth: { year: 2026, month: 9 } }),
    )

    act(() => result.current.openCreateForm())

    expect(result.current.date).toBe('2026-09-30')
  })

  it('reports dirty only while the values differ from those the form opened with', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(CHASE_BALANCE, BANKS, vi.fn(), { lockedBank: LOCKED_CHASE }))

    act(() => result.current.openEditForm({ ...ADJUSTMENT, bankId: 'bank-chase' }))
    expect(result.current.isDirty).toBe(false)

    act(() => result.current.setField('note', 'Statement'))
    expect(result.current.isDirty).toBe(true)

    act(() => result.current.setField('note', ADJUSTMENT.note!))
    expect(result.current.isDirty).toBe(false)
  })

  it('shows the connection message and keeps the values when the save cannot reach the server', async () => {
    createBalanceAdjustmentMock.mockRejectedValue(new ApiError('Failed to fetch', 0))
    const { result } = renderHook(() => useBalanceAdjustmentForm(CHASE_BALANCE, BANKS, vi.fn(), { lockedBank: LOCKED_CHASE }))

    act(() => result.current.openCreateForm())
    act(() => result.current.setField('targetBalance', '400'))
    await act(async () => result.current.submit())

    expect(result.current.saveError).toBe('Could not save. Check the connection and try again.')
    expect(result.current.saveErrorFields).toEqual({})
    expect(result.current.targetBalance).toBe('400')
  })
})
