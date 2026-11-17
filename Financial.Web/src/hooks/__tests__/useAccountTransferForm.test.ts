import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountTransferDto } from '../../api/types'
import { pinDate } from '../../test-utils/pinDate'
import { getStoredDefault, setStoredDefault } from '../../utils/createFormDefaults'
import type { TransferCounterparts } from '../useAccountFormOptions'
import { useAccountTransferForm } from '../useAccountTransferForm'

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

function fillTransferToChase(result: ReturnType<typeof renderTransferForm>['result']) {
  act(() => result.current.openCreateForm('to'))
  act(() => {
    result.current.setField('destinationBank', 'chase')
    result.current.setField('amount', '10')
  })
}

async function submitTransferToChase() {
  const { result } = renderTransferForm()
  fillTransferToChase(result)
  await act(async () => result.current.submit())
  return result
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


  it('validates the counterpart, the amount and the destination amount before saving', async () => {
    const { result } = renderTransferForm()

    act(() => result.current.openCreateForm('to'))
    await act(async () => result.current.submit())

    expect(result.current.saveErrorFields).toEqual({
      destinationBank: 'Choose the account to transfer to.',
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
    ['Transfers from Barclays to Chase are not allowed.', 'destinationBank'],
    ['Chase is inactive.', 'destinationBank'],
  ])('shows the server message %s under its field', async (message, field) => {
    createAccountTransferMock.mockRejectedValue(new ApiError(message, 400))

    const result = await submitTransferToChase()

    expect(result.current.saveErrorFields).toEqual({ [field]: message })
    expect(result.current.isOpen).toBe(true)
  })

  it('shows an amount-received message in the banner when that field is not shown', async () => {
    createAccountTransferMock.mockRejectedValue(new ApiError('Enter the amount received in EUR.', 400))

    const result = await submitTransferToChase()

    expect(result.current.saveErrorFields).toEqual({})
    expect(result.current.saveError).toBe('Enter the amount received in EUR.')
  })

  it('shows the connection message and keeps the values when the save cannot reach the server', async () => {
    createAccountTransferMock.mockRejectedValue(new ApiError('Failed to fetch', 0))

    const result = await submitTransferToChase()

    expect(result.current.saveError).toBe('Could not save. Check the connection and try again.')
    expect(result.current.saveErrorFields).toEqual({})
    expect(result.current.amount).toBe('10')
  })

  const EUR_TRANSFER: AccountTransferDto = {
    ...TRANSFER,
    id: 't2',
    sourceAccountId: 'barclays',
    sourceAccountName: 'Barclays',
    sourceCurrency: 'GBP',
    destinationAccountId: 'wise-eur',
    destinationAccountName: 'Wise EUR',
    destinationCurrency: 'EUR',
    amount: 100,
    destinationAmount: 115.4,
    note: null,
  }

  it('does not list an edited counterpart twice when it is still listed', () => {
    const { result } = renderHook(() => useAccountTransferForm(BARCLAYS, { destinations: [WISE_EUR], sources: [] }, vi.fn()))

    act(() => result.current.openEditForm(EUR_TRANSFER))

    expect(result.current.counterpartOptions).toEqual([WISE_EUR])
  })

  it('edits a transfer out of this account against its destination, with the amount received', () => {
    const { result } = renderHook(() => useAccountTransferForm(BARCLAYS, { destinations: [], sources: [] }, vi.fn()))

    act(() => result.current.openEditForm(EUR_TRANSFER))

    expect(result.current).toMatchObject({
      direction: 'to',
      counterpartOptions: [WISE_EUR],
      destinationAmount: '115.4',
      note: '',
      destinationCurrency: 'EUR',
    })
  })

  it('leaves the amount received blank when editing a transfer in one currency', () => {
    const { result } = renderTransferForm()

    act(() => result.current.openEditForm(TRANSFER))

    expect(result.current.destinationAmount).toBe('')
  })

  it.each([
    ['to', 'accountTransfer.destination', 'wise-eur', 'destinationBank', 'wise-eur'],
    ['from', 'accountTransfer.source', 'wise-eur', 'sourceBank', ''],
    ['to', 'accountTransfer.destination', 'gone', 'destinationBank', ''],
  ] as const)('opening %s with %s stored as %s sets %s to %j', (direction, key, stored, field, expected) => {
    setStoredDefault(key, stored)
    const { result } = renderTransferForm()

    act(() => result.current.openCreateForm(direction))

    expect(result.current[field]).toBe(expected)
  })

  it('refuses a whitespace-only date, a zero amount and a zero amount received', async () => {
    const { result } = renderTransferForm()
    act(() => result.current.openCreateForm('to'))
    act(() => {
      result.current.setField('date', '  ')
      result.current.setField('destinationBank', 'wise-eur')
      result.current.setField('amount', '0')
      result.current.setField('destinationAmount', '0')
    })

    await act(async () => result.current.submit())

    expect(result.current.saveErrorFields).toEqual({
      date: 'Enter the transfer date.',
      amount: 'Transfer amount must be greater than zero.',
      destinationAmount: 'Enter the amount received in EUR.',
    })
    expect(result.current.saveError).toBe('Enter the transfer date.')
  })

  it('shows the form as saving until the save settles, then the fallback message', async () => {
    createAccountTransferMock.mockRejectedValue('unexpected')
    const { result } = renderTransferForm()
    fillTransferToChase(result)

    act(() => result.current.submit())
    expect(result.current.isSaving).toBe(true)
    await act(async () => {})

    expect(result.current.isSaving).toBe(false)
    expect(result.current.saveError).toBe('Failed to save transfer.')
  })

  it('sends a whitespace-only note as no note and remembers the date', async () => {
    createAccountTransferMock.mockResolvedValue(TRANSFER)
    const { result } = renderTransferForm()
    fillTransferToChase(result)
    act(() => {
      result.current.setField('date', '2026-10-03')
      result.current.setField('note', '  ')
    })

    await act(async () => result.current.submit())

    expect(createAccountTransferMock).toHaveBeenCalledWith(expect.objectContaining({ note: null }))
    expect(getStoredDefault('transfer.date')).toBe('2026-10-03')
  })
})
