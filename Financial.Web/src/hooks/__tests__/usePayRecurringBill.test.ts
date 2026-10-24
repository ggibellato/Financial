import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { RecurringBillDto } from '../../api/types'
import { pinDate } from '../../test-utils/pinDate'
import { getStoredDefault, setStoredDefault } from '../../utils/createFormDefaults'
import { ALREADY_PAID_ERROR, unpaidUkBills, usePayRecurringBill } from '../usePayRecurringBill'

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

function bill(fields: Partial<RecurringBillDto> & Pick<RecurringBillDto, 'id' | 'dueDay' | 'description'>): RecurringBillDto {
  return { value: 50, area: 'UK', note: '', status: 'Unset', minimumWageValue: null, nitNumber: null, ...fields }
}

const COUNCIL_TAX = bill({ id: 'council', dueDay: 5, description: 'Council Tax', value: 120 })
const WATER = bill({ id: 'water', dueDay: 20, description: 'Water', value: 35.5, status: 'Scheduled' })
const RENT_PAID = bill({ id: 'rent', dueDay: 1, description: 'Rent', status: 'Paid' })
const BRAZIL = bill({ id: 'iptu', dueDay: 2, description: 'IPTU', area: 'BR' })
const BILLS = [WATER, RENT_PAID, BRAZIL, COUNCIL_TAX]

const BARCLAYS = { id: 'barclays', name: 'Barclays' }

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
    expect(result.current.canSubmit).toBe(false)
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

  it('shows the server message for any other refusal', async () => {
    markMensaisBillPaidMock.mockRejectedValue(new ApiError('Category is required.', 400))
    const { result } = renderPayBill()
    await openWithCouncilTax(result)

    act(() => result.current.setField('categoryId', 'cat-home'))
    await act(async () => result.current.submit())

    await waitFor(() => expect(result.current.saveError).toBe('Category is required.'))
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
})
