import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { recurringBill } from '../../test/recurringBill'
import { pinDate } from '../../test-utils/pinDate'
import { getStoredDefault, setStoredDefault } from '../../utils/createFormDefaults'
import { ALREADY_PAID_ERROR, CHOOSE_BILL_ERROR, unpaidUkBills, usePayRecurringBill } from '../usePayRecurringBill'
import { never } from '../../test-utils/deferred'

const { getMensaisBillsMock, markMensaisBillPaidMock } = vi.hoisted(() => ({
  getMensaisBillsMock: vi.fn<FinancialApiClient['getMensaisBills']>(),
  markMensaisBillPaidMock: vi.fn<FinancialApiClient['markMensaisBillPaid']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getMensaisBills: getMensaisBillsMock,
    markMensaisBillPaid: markMensaisBillPaidMock,
  } as Partial<FinancialApiClient>,
}))

const COUNCIL_TAX = recurringBill({ id: 'council', dueDay: 5, description: 'Council Tax', value: 120 })
const WATER = recurringBill({ id: 'water', dueDay: 20, description: 'Water', value: 35.5, status: 'Scheduled' })
const RENT_PAID = recurringBill({ id: 'rent', dueDay: 1, description: 'Rent', value: 900, status: 'Paid' })
const BRAZIL = recurringBill({ id: 'iptu', dueDay: 2, description: 'IPTU', value: 80, area: 'BR' })
const BILLS = [WATER, RENT_PAID, BRAZIL, COUNCIL_TAX]

const BARCLAYS = { id: 'barclays', name: 'Barclays' }

const REOPENERS = [
  ['opened again', (result: { current: ReturnType<typeof usePayRecurringBill> }) => act(() => result.current.open())],
  ['cancelled', (result: { current: ReturnType<typeof usePayRecurringBill> }) => act(() => result.current.cancel())],
] as const

function renderPayBill(onPaid = vi.fn(), viewedMonth = { year: 2026, month: 10 }) {
  return renderHook(() => usePayRecurringBill(BARCLAYS, onPaid, { viewedMonth }))
}

async function openWithCouncilTax(result: { current: ReturnType<typeof usePayRecurringBill> }) {
  act(() => result.current.open())
  await waitFor(() => expect(result.current.bills).toHaveLength(2))
  act(() => result.current.selectBill('council'))
}

describe('unpaidUkBills', () => {
  it('keeps only unpaid UK bills, ordered by due day', () => {
    expect(unpaidUkBills(BILLS).map((b) => b.id)).toEqual(['council', 'water'])
  })

  it('orders bills due on the same day by description', () => {
    const gas = recurringBill({ id: 'gas', dueDay: 5, description: 'Gas', value: 40 })

    expect(unpaidUkBills([gas, COUNCIL_TAX]).map((b) => b.id)).toEqual(['council', 'gas'])
  })
})

describe('usePayRecurringBill', () => {
  beforeEach(() => {
    pinDate('2026-10-15T12:00:00+01:00')
    getMensaisBillsMock.mockReset().mockResolvedValue(BILLS)
    markMensaisBillPaidMock.mockReset()
    sessionStorage.clear()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('loads the bills only once opened', async () => {
    const { result } = renderPayBill()
    expect(getMensaisBillsMock).not.toHaveBeenCalled()

    act(() => result.current.open())

    await waitFor(() => expect(result.current.bills.map((b) => b.id)).toEqual(['council', 'water']))
  })

  it('pre-fills description and value from the chosen bill, with this bank and no category', async () => {
    const { result } = renderPayBill()

    await openWithCouncilTax(result)

    expect(result.current.values).toEqual({
      date: '2026-10-15',
      description: 'Council Tax',
      value: '120',
      bankId: 'barclays',
      categoryId: '',
    })
    expect(result.current.isDirty).toBe(true)
  })

  it('keeps the chosen category and date when another bill is picked', async () => {
    const { result } = renderPayBill()
    await openWithCouncilTax(result)

    act(() => result.current.setField('categoryId', 'cat-home'))
    act(() => result.current.setField('date', '2026-10-03'))
    act(() => result.current.selectBill('water'))

    expect(result.current.values).toMatchObject({ description: 'Water', value: '35.5', categoryId: 'cat-home', date: '2026-10-03' })
  })

  it('asks for a bill, then names each invalid field, instead of saving', async () => {
    const { result } = renderPayBill()
    act(() => result.current.open())
    await waitFor(() => expect(result.current.bills).toHaveLength(2))

    act(() => result.current.submit())
    expect(result.current.billError).toBe(CHOOSE_BILL_ERROR)

    act(() => result.current.selectBill('council'))
    act(() => result.current.setField('value', '0'))
    act(() => result.current.submit())

    expect(result.current.fieldErrors).toEqual({ value: 'Enter a value greater than 0.', categoryId: 'Choose a category.' })
    expect(markMensaisBillPaidMock).not.toHaveBeenCalled()

    act(() => result.current.setField('categoryId', 'cat-home'))
    expect(result.current.fieldErrors.categoryId).toBeUndefined()
    expect(result.current.fieldErrors.value).toBe('Enter a value greater than 0.')
  })

  it('opens on the last day of a past viewed month, keeping a last-used date inside it', async () => {
    const past = renderPayBill(vi.fn(), { year: 2026, month: 9 })
    await openWithCouncilTax(past.result)
    expect(past.result.current.values?.date).toBe('2026-09-30')

    setStoredDefault('payRecurringBill.date', '2026-09-12')
    const kept = renderPayBill(vi.fn(), { year: 2026, month: 9 })
    await openWithCouncilTax(kept.result)
    expect(kept.result.current.values?.date).toBe('2026-09-12')
  })

  it('pays through mensais/{id}/paid with this bank, then closes and remembers the date', async () => {
    markMensaisBillPaidMock.mockResolvedValue({ ...COUNCIL_TAX, status: 'Paid' })
    const onPaid = vi.fn()
    const { result } = renderPayBill(onPaid)
    await openWithCouncilTax(result)

    act(() => result.current.setField('categoryId', 'cat-home'))
    await act(async () => result.current.submit())

    expect(markMensaisBillPaidMock).toHaveBeenCalledWith('council', {
      expense: {
        date: '2026-10-15',
        description: 'Council Tax',
        value: 120,
        categoryId: 'cat-home',
        paymentSourceBankId: 'barclays',
      },
    })
    expect(onPaid).toHaveBeenCalledOnce()
    expect(result.current.isOpen).toBe(false)
    expect(getStoredDefault('payRecurringBill.date')).toBe('2026-10-15')
  })

  it('says the bill is already paid and refreshes the list when the server refuses because it was paid', async () => {
    markMensaisBillPaidMock.mockRejectedValue(
      new ApiError('The bill is already Paid; recording another payment expense would duplicate it.', 400),
    )
    const { result } = renderPayBill()
    await openWithCouncilTax(result)
    getMensaisBillsMock.mockResolvedValue([{ ...COUNCIL_TAX, status: 'Paid' }, WATER])

    act(() => result.current.setField('categoryId', 'cat-home'))
    await act(async () => result.current.submit())

    await waitFor(() => expect(result.current.saveError).toBe(ALREADY_PAID_ERROR))
    expect(result.current.bills.map((b) => b.id)).toEqual(['water'])
    expect(result.current.billId).toBe('')
    expect(result.current.isOpen).toBe(true)
  })

  it('shows a server message for a field under that field, and any other refusal above the form', async () => {
    markMensaisBillPaidMock.mockRejectedValueOnce(new ApiError('Date must not be in the future.', 400))
    const { result } = renderPayBill()
    await openWithCouncilTax(result)

    act(() => result.current.setField('categoryId', 'cat-home'))
    await act(async () => result.current.submit())
    await waitFor(() => expect(result.current.fieldErrors).toEqual({ date: 'Date must not be in the future.' }))
    expect(result.current.saveError).toBeNull()

    markMensaisBillPaidMock.mockRejectedValueOnce(new ApiError('Bank is inactive.', 400))
    await act(async () => result.current.submit())

    await waitFor(() => expect(result.current.saveError).toBe('Bank is inactive.'))
    expect(result.current.fieldErrors).toEqual({})
    expect(result.current.billId).toBe('council')
  })

  it('keeps the values and shows the connection message when the payment cannot reach the server', async () => {
    markMensaisBillPaidMock.mockRejectedValue(new ApiError('Failed to fetch', 0))
    const { result } = renderPayBill()
    await openWithCouncilTax(result)

    act(() => result.current.setField('categoryId', 'cat-home'))
    await act(async () => result.current.submit())

    expect(result.current.saveError).toBe('Could not save. Check the connection and try again.')
    expect(result.current.values?.categoryId).toBe('cat-home')
    expect(getMensaisBillsMock).toHaveBeenCalledOnce()
  })

  async function failWithInactiveBank(result: { current: ReturnType<typeof usePayRecurringBill> }) {
    markMensaisBillPaidMock.mockRejectedValueOnce(new ApiError('Bank is inactive.', 400))
    await act(async () => result.current.submit())
    await waitFor(() => expect(result.current.saveError).toBe('Bank is inactive.'))
  }

  it('shows the bills as loading only while an open form waits for them', async () => {
    let resolveLoad: (bills: typeof BILLS) => void = () => {}
    getMensaisBillsMock.mockReturnValue(new Promise((resolve) => { resolveLoad = resolve }))
    const { result } = renderPayBill()
    const closed = result.current.isLoadingBills
    act(() => result.current.open())
    const waiting = result.current.isLoadingBills

    await act(async () => resolveLoad(BILLS))

    expect({ closed, waiting, loaded: result.current.isLoadingBills }).toEqual({ closed: false, waiting: true, loaded: false })
  })

  it('stops showing the bills as loading when they fail to load', async () => {
    getMensaisBillsMock.mockRejectedValue(new Error('offline'))
    const { result } = renderPayBill()

    act(() => result.current.open())

    await waitFor(() => expect(result.current.billsError).not.toBeNull())
    expect(result.current.isLoadingBills).toBe(false)
  })

  it('clears the bill message once a bill is chosen', async () => {
    const { result } = renderPayBill()
    act(() => result.current.open())
    await waitFor(() => expect(result.current.bills).toHaveLength(2))
    act(() => result.current.submit())

    act(() => result.current.selectBill('council'))

    expect(result.current.billError).toBeNull()
  })

  it.each(REOPENERS)('drops the bill and its field messages when %s', async (_, reopen) => {
    const { result } = renderPayBill()
    await openWithCouncilTax(result)
    act(() => result.current.setField('value', '0'))
    act(() => result.current.submit())

    reopen(result)

    expect(result.current.billId).toBe('')
    expect(result.current.fieldErrors).toEqual({})
  })

  it.each(REOPENERS)('drops the save message when %s', async (_, reopen) => {
    const { result } = renderPayBill()
    await openWithCouncilTax(result)
    act(() => result.current.setField('categoryId', 'cat-home'))
    await failWithInactiveBank(result)

    reopen(result)

    expect(result.current.saveError).toBeNull()
  })

  it('clears the save message when a field changes and when a payment starts', async () => {
    const { result } = renderPayBill()
    await openWithCouncilTax(result)
    act(() => result.current.setField('categoryId', 'cat-home'))
    await failWithInactiveBank(result)

    act(() => result.current.setField('description', 'Council Tax October'))
    const afterEdit = result.current.saveError
    await failWithInactiveBank(result)
    markMensaisBillPaidMock.mockReturnValue(never())
    act(() => result.current.submit())

    expect(afterEdit).toBeNull()
    expect(result.current.saveError).toBeNull()
  })

  it('pays once while a payment is still running, and shows it as saving', async () => {
    markMensaisBillPaidMock.mockReturnValue(never())
    const { result } = renderPayBill()
    await openWithCouncilTax(result)
    act(() => result.current.setField('categoryId', 'cat-home'))

    act(() => result.current.submit())
    act(() => result.current.submit())

    expect(result.current.isSaving).toBe(true)
    expect(markMensaisBillPaidMock).toHaveBeenCalledOnce()
  })

  it('shows the refusal above the form when the bills cannot be reloaded after it', async () => {
    markMensaisBillPaidMock.mockRejectedValue(new ApiError('Bank is inactive.', 400))
    const { result } = renderPayBill()
    await openWithCouncilTax(result)
    getMensaisBillsMock.mockRejectedValue(new Error('offline'))
    act(() => result.current.setField('categoryId', 'cat-home'))

    await act(async () => result.current.submit())

    await waitFor(() => expect(result.current.saveError).toBe('Bank is inactive.'))
  })
})
