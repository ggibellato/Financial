import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import { ApiError } from '../api/apiError'
import type { BalanceAdjustmentDto } from '../api/types'
import { useBalanceAdjustmentForm } from './useBalanceAdjustmentForm'

const createBalanceAdjustmentMock = vi.fn<FinancialApiClient['createBalanceAdjustment']>()
const updateBalanceAdjustmentMock = vi.fn<FinancialApiClient['updateBalanceAdjustment']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    createBalanceAdjustment: createBalanceAdjustmentMock,
    updateBalanceAdjustment: updateBalanceAdjustmentMock,
  }),
}))

const ADJUSTMENT: BalanceAdjustmentDto = {
  id: 'a1',
  date: '2026-07-20',
  bank: 'Barclays',
  targetBalance: 150,
  delta: 50,
  note: 'Matched statement',
}

describe('useBalanceAdjustmentForm', () => {
  beforeEach(() => {
    createBalanceAdjustmentMock.mockReset()
    updateBalanceAdjustmentMock.mockReset()
  })

  it('starts closed', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(vi.fn()))

    expect(result.current.isOpen).toBe(false)
  })

  it('openCreateForm defaults date to today and stores bank/currentBalance', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(vi.fn()))

    act(() => result.current.openCreateForm('Barclays', 100))

    const today = new Date().toISOString().slice(0, 10)
    expect(result.current.isOpen).toBe(true)
    expect(result.current.isEditing).toBe(false)
    expect(result.current.bankName).toBe('Barclays')
    expect(result.current.currentBalance).toBe(100)
    expect(result.current.date).toBe(today)
    expect(result.current.savedDelta).toBeNull()
  })

  it('openEditForm pre-fills targetBalance/date/note from the given adjustment', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(vi.fn()))

    act(() => result.current.openEditForm('Barclays', 100, ADJUSTMENT))

    expect(result.current.isOpen).toBe(true)
    expect(result.current.isEditing).toBe(true)
    expect(result.current.bankName).toBe('Barclays')
    expect(result.current.currentBalance).toBe(100)
    expect(result.current.date).toBe('2026-07-20')
    expect(result.current.targetBalance).toBe('150')
    expect(result.current.note).toBe('Matched statement')
  })

  it('cancel resets the form to closed and clears savedDelta', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(vi.fn()))
    act(() => result.current.openEditForm('Barclays', 100, ADJUSTMENT))

    act(() => result.current.cancel())

    expect(result.current.isOpen).toBe(false)
    expect(result.current.savedDelta).toBeNull()
  })

  it('submit blocks with an error when the target balance is missing', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(vi.fn()))
    act(() => result.current.openCreateForm('Barclays', 100))

    act(() => result.current.submit())

    expect(result.current.saveErrorField).toBe('targetBalance')
    expect(createBalanceAdjustmentMock).not.toHaveBeenCalled()
  })

  it('submit blocks with an error when the target balance is negative', () => {
    const { result } = renderHook(() => useBalanceAdjustmentForm(vi.fn()))
    act(() => result.current.openCreateForm('Barclays', 100))
    act(() => result.current.setField('targetBalance', '-1'))

    act(() => result.current.submit())

    expect(result.current.saveErrorField).toBe('targetBalance')
    expect(createBalanceAdjustmentMock).not.toHaveBeenCalled()
  })

  it('submit calls createBalanceAdjustment, sets isSaving, and sets savedDelta on success while staying open', async () => {
    let resolveCreate!: (value: BalanceAdjustmentDto) => void
    createBalanceAdjustmentMock.mockReturnValue(new Promise((resolve) => (resolveCreate = resolve)))
    const onSaved = vi.fn()
    const { result } = renderHook(() => useBalanceAdjustmentForm(onSaved))
    act(() => result.current.openCreateForm('Barclays', 100))
    act(() => result.current.setField('targetBalance', '150'))

    act(() => result.current.submit())

    expect(result.current.isSaving).toBe(true)
    expect(createBalanceAdjustmentMock).toHaveBeenCalledWith('Barclays', {
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
    const { result } = renderHook(() => useBalanceAdjustmentForm(vi.fn()))
    act(() => result.current.openEditForm('Barclays', 100, ADJUSTMENT))
    act(() => result.current.setField('targetBalance', '120'))

    await act(async () => {
      result.current.submit()
      await Promise.resolve()
    })

    expect(updateBalanceAdjustmentMock).toHaveBeenCalledWith('Barclays', 'a1', {
      date: '2026-07-20',
      targetBalance: 120,
      note: 'Matched statement',
    })
  })

  it('sets saveError and saveErrorField from a failed request', async () => {
    createBalanceAdjustmentMock.mockRejectedValue(new ApiError('Balance cannot be negative.', 400))
    const { result } = renderHook(() => useBalanceAdjustmentForm(vi.fn()))
    act(() => result.current.openCreateForm('Barclays', 100))
    act(() => result.current.setField('targetBalance', '150'))

    await act(async () => {
      result.current.submit()
      await Promise.resolve()
      await Promise.resolve()
    })

    expect(result.current.isSaving).toBe(false)
    expect(result.current.saveError).toBe('Balance cannot be negative.')
    expect(result.current.saveErrorField).toBe('targetBalance')
    expect(result.current.isOpen).toBe(true)
  })
})
