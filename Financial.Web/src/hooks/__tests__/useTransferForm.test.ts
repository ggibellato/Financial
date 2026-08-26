import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { ApiError } from '../../api/apiError'
import type { BankDto, TransferDto } from '../../api/types'
import { useTransferForm } from '../useTransferForm'

const { createTransferMock, updateTransferMock } = vi.hoisted(() => ({
  createTransferMock: vi.fn<FinancialApiClient['createTransfer']>(),
  updateTransferMock: vi.fn<FinancialApiClient['updateTransfer']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    createTransfer: createTransferMock,
    updateTransfer: updateTransferMock,
  } as Partial<FinancialApiClient>,
}))

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01' },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01' },
]

const TRANSFER: TransferDto = {
  id: 't1',
  date: '2026-07-20',
  sourceBankId: 'bank-barclays',
  sourceBankName: 'Barclays',
  destinationBankId: 'bank-trading212',
  destinationBankName: 'Trading212',
  amount: 500,
  note: 'Round-up top-up',
}

describe('useTransferForm', () => {
  beforeEach(() => {
    createTransferMock.mockReset()
    updateTransferMock.mockReset()
  })

  it('starts closed', () => {
    const { result } = renderHook(() => useTransferForm(BANKS, vi.fn()))

    expect(result.current.isOpen).toBe(false)
  })

  it('openCreateForm defaults date to today and source to the first bank', () => {
    const { result } = renderHook(() => useTransferForm(BANKS, vi.fn()))

    act(() => result.current.openCreateForm())

    const today = new Date().toISOString().slice(0, 10)
    expect(result.current.isOpen).toBe(true)
    expect(result.current.isEditing).toBe(false)
    expect(result.current.date).toBe(today)
    expect(result.current.sourceBank).toBe('bank-barclays')
    expect(result.current.destinationBank).toBe('')
  })

  it('openCreateForm uses the given preselected source bank instead of the first bank', () => {
    const { result } = renderHook(() => useTransferForm(BANKS, vi.fn()))

    act(() => result.current.openCreateForm('bank-trading212'))

    expect(result.current.sourceBank).toBe('bank-trading212')
  })

  it('openEditForm pre-fills every field from the given transfer', () => {
    const { result } = renderHook(() => useTransferForm(BANKS, vi.fn()))

    act(() => result.current.openEditForm(TRANSFER))

    expect(result.current.isOpen).toBe(true)
    expect(result.current.isEditing).toBe(true)
    expect(result.current.date).toBe('2026-07-20')
    expect(result.current.sourceBank).toBe('bank-barclays')
    expect(result.current.destinationBank).toBe('bank-trading212')
    expect(result.current.amount).toBe('500')
    expect(result.current.note).toBe('Round-up top-up')
  })

  it('cancel resets the form to closed', () => {
    const { result } = renderHook(() => useTransferForm(BANKS, vi.fn()))
    act(() => result.current.openEditForm(TRANSFER))

    act(() => result.current.cancel())

    expect(result.current.isOpen).toBe(false)
    expect(result.current.date).toBe('')
  })

  it('submit blocks with a required-field error when the amount is missing', () => {
    const { result } = renderHook(() => useTransferForm(BANKS, vi.fn()))
    act(() => result.current.openCreateForm())
    act(() => result.current.setField('destinationBank', 'bank-trading212'))

    act(() => result.current.submit())

    expect(result.current.saveError).toBe('Amount must be greater than zero.')
    expect(result.current.saveErrorField).toBe('amount')
    expect(createTransferMock).not.toHaveBeenCalled()
  })

  it('submit blocks when source and destination match, without calling the API', () => {
    const { result } = renderHook(() => useTransferForm(BANKS, vi.fn()))
    act(() => result.current.openCreateForm())
    act(() => result.current.setField('destinationBank', 'bank-barclays'))
    act(() => result.current.setField('amount', '100'))

    act(() => result.current.submit())

    expect(result.current.saveErrorField).toBe('destinationBank')
    expect(createTransferMock).not.toHaveBeenCalled()
  })

  it('submit calls createTransfer, sets isSaving, and calls onSaved on success', async () => {
    let resolveCreate!: (value: TransferDto) => void
    createTransferMock.mockReturnValue(new Promise((resolve) => (resolveCreate = resolve)))
    const onSaved = vi.fn()
    const { result } = renderHook(() => useTransferForm(BANKS, onSaved))
    act(() => result.current.openCreateForm())
    act(() => result.current.setField('destinationBank', 'bank-trading212'))
    act(() => result.current.setField('amount', '500'))

    act(() => result.current.submit())

    expect(result.current.isSaving).toBe(true)
    expect(createTransferMock).toHaveBeenCalledWith({
      date: expect.any(String),
      sourceBankId: 'bank-barclays',
      destinationBankId: 'bank-trading212',
      amount: 500,
      note: null,
    })

    await act(async () => {
      resolveCreate(TRANSFER)
      await Promise.resolve()
    })

    expect(result.current.isSaving).toBe(false)
    expect(result.current.isOpen).toBe(false)
    expect(onSaved).toHaveBeenCalledOnce()
  })

  it('submit calls updateTransfer when editing', async () => {
    updateTransferMock.mockResolvedValue(TRANSFER)
    const { result } = renderHook(() => useTransferForm(BANKS, vi.fn()))
    act(() => result.current.openEditForm(TRANSFER))
    act(() => result.current.setField('amount', '250'))

    await act(async () => {
      result.current.submit()
      await Promise.resolve()
    })

    expect(updateTransferMock).toHaveBeenCalledWith('t1', {
      date: '2026-07-20',
      sourceBankId: 'bank-barclays',
      destinationBankId: 'bank-trading212',
      amount: 250,
      note: 'Round-up top-up',
    })
  })

  it('sets saveError and saveErrorField from a failed request', async () => {
    createTransferMock.mockRejectedValue(new ApiError('Transfer amount must be greater than zero.', 400))
    const { result } = renderHook(() => useTransferForm(BANKS, vi.fn()))
    act(() => result.current.openCreateForm())
    act(() => result.current.setField('destinationBank', 'bank-trading212'))
    act(() => result.current.setField('amount', '500'))

    await act(async () => {
      result.current.submit()
      await Promise.resolve()
      await Promise.resolve()
    })

    expect(result.current.isSaving).toBe(false)
    expect(result.current.saveError).toBe('Transfer amount must be greater than zero.')
    expect(result.current.saveErrorField).toBe('amount')
    expect(result.current.isOpen).toBe(true)
  })
})
