import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountTransferDto } from '../../api/types'
import { pinDate } from '../../test-utils/pinDate'
import { getStoredDefault, setStoredDefault } from '../../utils/createFormDefaults'
import { useAccountTransferForm, type TransferCounterparts } from '../useAccountTransferForm'

const { createAccountTransferMock, updateAccountTransferMock } = vi.hoisted(() => ({
  createAccountTransferMock: vi.fn<FinancialApiClient['createAccountTransfer']>(),
  updateAccountTransferMock: vi.fn<FinancialApiClient['updateAccountTransfer']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    createAccountTransfer: createAccountTransferMock,
    updateAccountTransfer: updateAccountTransferMock,
  } as Partial<FinancialApiClient>,
}))

const BARCLAYS = { id: 'barclays', name: 'Barclays', currency: 'GBP' }
const CHASE = { id: 'chase', name: 'Chase', currency: 'GBP' }
const WISE_EUR = { id: 'wise-eur', name: 'Wise EUR', currency: 'EUR' }

const COUNTERPARTS: TransferCounterparts = {
  destinations: [CHASE, WISE_EUR],
  sources: [CHASE],
}

const TRANSFER: AccountTransferDto = {
  id: 't1',
  date: '2026-10-05',
  sourceAccountId: 'chase',
  sourceAccountName: 'Chase',
  sourceCurrency: 'GBP',
  destinationAccountId: 'barclays',
  destinationAccountName: 'Barclays',
  destinationCurrency: 'GBP',
  amount: 200,
  destinationAmount: 200,
  note: 'Rent',
}

function renderTransferForm(onSaved = vi.fn()) {
  return renderHook(() => useAccountTransferForm(BARCLAYS, COUNTERPARTS, onSaved, { viewedMonth: { year: 2026, month: 10 } }))
}

describe('useAccountTransferForm', () => {
  beforeEach(() => {
    pinDate('2026-10-15T12:00:00+01:00')
    createAccountTransferMock.mockReset()
    updateAccountTransferMock.mockReset()
    sessionStorage.clear()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('Transfer to locks this account as the source and lists the destinations', () => {
    const { result } = renderTransferForm()

    act(() => result.current.openCreateForm('to'))

    expect(result.current.sourceBank).toBe('barclays')
    expect(result.current.destinationBank).toBe('')
    expect(result.current.counterpartOptions).toEqual([CHASE, WISE_EUR])
    expect(result.current.date).toBe('2026-10-15')
  })

  it('Transfer from locks this account as the destination and lists the sources', () => {
    const { result } = renderTransferForm()

    act(() => result.current.openCreateForm('from'))

    expect(result.current.sourceBank).toBe('')
    expect(result.current.destinationBank).toBe('barclays')
    expect(result.current.counterpartOptions).toEqual([CHASE])
  })

  it('asks for the destination amount only when the two currencies differ', () => {
    const { result } = renderTransferForm()

    act(() => result.current.openCreateForm('to'))
    act(() => result.current.setField('destinationBank', 'chase'))
    expect(result.current.destinationCurrency).toBeNull()

    act(() => result.current.setField('destinationBank', 'wise-eur'))
    expect(result.current.destinationCurrency).toBe('EUR')
  })

  it('posts to accounts/transfers with the destination amount for different currencies', async () => {
    createAccountTransferMock.mockResolvedValue(TRANSFER)
    const onSaved = vi.fn()
    const { result } = renderTransferForm(onSaved)

    act(() => result.current.openCreateForm('to'))
    act(() => result.current.setField('destinationBank', 'wise-eur'))
    act(() => result.current.setField('amount', '100'))
    act(() => result.current.setField('destinationAmount', '115.40'))
    await act(async () => result.current.submit())

    expect(createAccountTransferMock).toHaveBeenCalledWith({
      date: '2026-10-15',
      sourceAccountId: 'barclays',
      destinationAccountId: 'wise-eur',
      amount: 100,
      destinationAmount: 115.4,
      note: null,
    })
    expect(onSaved).toHaveBeenCalledWith(false)
    expect(result.current.isOpen).toBe(false)
    expect(getStoredDefault('accountTransfer.destination')).toBe('wise-eur')
  })

  it('sends no destination amount when the currencies match', async () => {
    createAccountTransferMock.mockResolvedValue(TRANSFER)
    const { result } = renderTransferForm()

    act(() => result.current.openCreateForm('from'))
    act(() => result.current.setField('sourceBank', 'chase'))
    act(() => result.current.setField('amount', '50'))
    await act(async () => result.current.submit())

    expect(createAccountTransferMock).toHaveBeenCalledWith(
      expect.objectContaining({ sourceAccountId: 'chase', destinationAccountId: 'barclays', destinationAmount: null }),
    )
  })

  it('opens with the last-used counterpart when it is still listed', () => {
    setStoredDefault('accountTransfer.destination', 'chase')
    const { result } = renderTransferForm()

    act(() => result.current.openCreateForm('to'))

    expect(result.current.destinationBank).toBe('chase')
  })

  it('validates the counterpart, the amount and the destination amount before saving', async () => {
    const { result } = renderTransferForm()

    act(() => result.current.openCreateForm('to'))
    await act(async () => result.current.submit())

    expect(result.current.saveErrorFields).toEqual({
      destinationBank: 'Choose an account',
      amount: 'Transfer amount must be greater than zero.',
    })

    act(() => result.current.setField('destinationBank', 'wise-eur'))
    act(() => result.current.setField('amount', '100'))
    await act(async () => result.current.submit())

    expect(result.current.saveErrorFields).toEqual({ destinationAmount: 'Enter the amount received in EUR.' })
    expect(createAccountTransferMock).not.toHaveBeenCalled()
  })

  it('edits a transfer into this account from its own record, keeping a counterpart no longer listed', async () => {
    updateAccountTransferMock.mockResolvedValue(TRANSFER)
    const onSaved = vi.fn()
    const { result } = renderHook(() => useAccountTransferForm(BARCLAYS, { destinations: [], sources: [] }, onSaved))

    act(() => result.current.openEditForm(TRANSFER))

    expect(result.current.direction).toBe('from')
    expect(result.current.isEditing).toBe(true)
    expect(result.current.date).toBe('2026-10-05')
    expect(result.current.counterpartOptions).toEqual([CHASE])
    expect(result.current.isDirty).toBe(false)

    act(() => result.current.setField('amount', '250'))
    expect(result.current.isDirty).toBe(true)
    await act(async () => result.current.submit())

    expect(updateAccountTransferMock).toHaveBeenCalledWith('t1', expect.objectContaining({ amount: 250, note: 'Rent' }))
    expect(onSaved).toHaveBeenCalledWith(true)
  })

  it.each([
    ['Date must not be in the future.', 'date'],
    ['Enter the amount received in EUR.', 'destinationAmount'],
    ['Transfers from Barclays to Chase are not allowed.', 'destinationBank'],
    ['Chase is inactive.', 'destinationBank'],
  ])('shows the server message %s under its field', async (message, field) => {
    createAccountTransferMock.mockRejectedValue(new ApiError(message, 400))
    const { result } = renderTransferForm()

    act(() => result.current.openCreateForm('to'))
    act(() => result.current.setField('destinationBank', 'chase'))
    act(() => result.current.setField('amount', '10'))
    await act(async () => result.current.submit())

    expect(result.current.saveErrorFields).toEqual({ [field]: message })
    expect(result.current.isOpen).toBe(true)
  })

  it('shows the connection message and keeps the values when the save cannot reach the server', async () => {
    createAccountTransferMock.mockRejectedValue(new ApiError('Failed to fetch', 0))
    const { result } = renderTransferForm()

    act(() => result.current.openCreateForm('to'))
    act(() => result.current.setField('destinationBank', 'chase'))
    act(() => result.current.setField('amount', '10'))
    await act(async () => result.current.submit())

    expect(result.current.saveError).toBe('Could not save. Check the connection and try again.')
    expect(result.current.saveErrorFields).toEqual({})
    expect(result.current.amount).toBe('10')
  })
})
