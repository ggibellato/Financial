import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, NETWORK_FAILURE_STATUS } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { SettlementEntryDto } from '../../api/types'
import { pinDate } from '../../test-utils/pinDate'
import { NETWORK_SAVE_ERROR } from '../../utils/formatters'
import { useSettlementEntryForm, type SettlementEntryField } from '../useSettlementEntryForm'

const { createSettlementEntryMock, updateSettlementEntryMock, settleAccountMock } = vi.hoisted(() => ({
  settleAccountMock: vi.fn<FinancialApiClient['settleAccount']>(),
  createSettlementEntryMock: vi.fn<FinancialApiClient['createSettlementEntry']>(),
  updateSettlementEntryMock: vi.fn<FinancialApiClient['updateSettlementEntry']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    createSettlementEntry: createSettlementEntryMock,
    updateSettlementEntry: updateSettlementEntryMock,
    settleAccount: settleAccountMock,
  } as Partial<FinancialApiClient>,
}))

const MAE = {
  id: 'mae',
  name: 'Mãe account',
  currency: 'BRL',
  otherParticipant: 'Mãe',
  currentBalances: [
    { currency: 'BRL', balance: 167.88 },
    { currency: 'GBP', balance: 7.94 },
  ],
}
const SAMUEL = { id: 'samuel', name: 'Samuel', currency: 'GBP', otherParticipant: null, currentBalances: [{ currency: 'GBP', balance: 15 }] }

const ENTRY: SettlementEntryDto = {
  id: 'e1',
  accountId: 'mae',
  date: '2026-10-10',
  description: 'Farmácia',
  note: 'metade',
  paidBy: 'Other',
  currency: 'BRL',
  amount: 150,
  convertedCurrency: 'GBP',
  convertedValue: 21.5,
  warning: null,
}

function renderForm(account: typeof MAE | typeof SAMUEL = MAE, viewedMonth = { year: 2026, month: 10 }) {
  const onSaved = vi.fn<(message: string) => void>()
  const rendered = renderHook(() => useSettlementEntryForm(account, viewedMonth, onSaved))
  return { ...rendered, onSaved }
}

type Rendered = ReturnType<typeof renderForm>['result']

function fill(result: Rendered, fields: Partial<Record<SettlementEntryField, string>>) {
  act(() => {
    for (const [field, value] of Object.entries(fields)) result.current.setField(field as SettlementEntryField, value)
  })
}

function fillValidEntry(result: Rendered) {
  fill(result, { paidBy: 'You', description: 'Farmácia', amount: '150', note: 'metade' })
}

function submit(result: Rendered): SettlementEntryField | null {
  let firstInvalid: SettlementEntryField | null = null
  act(() => {
    firstInvalid = result.current.submit()
  })
  return firstInvalid
}

function maeWith(brl: number, gbp: number) {
  return { ...MAE, currentBalances: [{ currency: 'BRL', balance: brl }, { currency: 'GBP', balance: gbp }] }
}

function openSettle(account: typeof MAE | typeof SAMUEL = MAE) {
  const rendered = renderForm(account)
  act(() => rendered.result.current.openSettle())
  return rendered
}

describe('useSettlementEntryForm in Settle mode', () => {
  beforeEach(() => {
    pinDate('2026-11-07T12:00:00+00:00')
    sessionStorage.clear()
    settleAccountMock.mockReset().mockResolvedValue({ ...ENTRY, description: 'Settlement' })
  })

  afterEach(() => vi.useRealTimers())

  it('[AC P60-F12-settlement-screen-05] pre-fills who pays, the amount and a same-direction converted value at 2 decimals', () => {
    const { result } = openSettle(maeWith(167.8768, 7.94214099))

    expect(result.current.isSettling).toBe(true)
    expect(result.current.isDirty).toBe(false)
    expect(result.current.convertedCurrency).toBe('GBP')
    expect(result.current.values).toMatchObject({ currency: 'BRL', paidBy: 'Other', amount: '167.88', convertedValue: '7.94', description: 'Settlement', date: '2026-10-31' })
    expect(result.current.impliedRate).toBe('Implied rate: 1 GBP = R$21.14')
  })

  it('[AC P60-F12-settlement-screen-05] leaves the converted value empty for opposite directions and says what remains', () => {
    const { result } = openSettle(maeWith(100, -10))

    expect(result.current.values).toMatchObject({ currency: 'BRL', paidBy: 'Other', amount: '100.00', convertedValue: '' })
    expect(result.current.impliedRate).toBeNull()
    submit(result)
    expect(result.current.confirmation).toBe('This leaves R$0.00 / £10.00 outstanding (you owe Mãe).')
  })

  it('starts on the first currency not settled, paid by you when you owe', () => {
    const { result } = openSettle(maeWith(0.004, -5))

    expect(result.current.values).toMatchObject({ currency: 'GBP', paidBy: 'You', amount: '5.00', convertedValue: '' })
    expect(result.current.convertedCurrency).toBe('BRL')
  })

  it('re-fills paid by, amount and converted value when the currency changes', () => {
    const { result } = openSettle(maeWith(100, -10))

    fill(result, { currency: 'GBP' })

    expect(result.current.values).toMatchObject({ currency: 'GBP', paidBy: 'You', amount: '10.00', convertedValue: '' })
    expect(result.current.convertedCurrency).toBe('BRL')
    expect(result.current.isDirty).toBe(true)
  })

  it('gives the same implied rate whichever currency is settled', () => {
    const { result } = openSettle()

    fill(result, { currency: 'GBP' })

    expect(result.current.values).toMatchObject({ amount: '7.94', convertedValue: '167.88' })
    expect(result.current.impliedRate).toBe('Implied rate: 1 GBP = R$21.14')
  })

  it('pre-fills only the amount on a one-currency account', () => {
    const { result } = openSettle(SAMUEL)

    expect(result.current.asksCurrency).toBe(false)
    expect(result.current.convertedCurrency).toBeNull()
    expect(result.current.values).toMatchObject({ currency: 'GBP', paidBy: 'Other', amount: '15.00', convertedValue: '' })
    expect(result.current.impliedRate).toBeNull()
    submit(result)
    expect(result.current.confirmation).toBe('The total becomes zero.')
  })

  it('[AC P60-F12-settlement-screen-05] says both totals become zero, accepting sub-penny residue', () => {
    const { result } = openSettle(maeWith(167.8768, 7.94214099))

    submit(result)

    expect(result.current.confirmation).toBe('Both totals become zero.')
    expect(settleAccountMock).not.toHaveBeenCalled()
  })

  it('[AC P60-F12-settlement-screen-06] shows the leftover when an edited value does not reach zero', () => {
    const { result } = openSettle(maeWith(167.8768, 7.94214099))

    fill(result, { amount: '100' })
    submit(result)

    expect(result.current.confirmation).toBe('This leaves R$67.88 / £0.00 outstanding (Mãe owes you).')
  })

  it('[AC P60-F12-settlement-screen-06] names who owes per currency when the leftovers point different ways', () => {
    const { result } = openSettle(maeWith(103.2, -1))

    fill(result, { amount: '100' })
    submit(result)

    expect(result.current.confirmation).toBe('This leaves R$3.20 / £1.00 outstanding (Mãe owes you R$3.20; you owe Mãe £1.00).')
  })

  it('does not ask for confirmation while a field is invalid', () => {
    const { result } = openSettle()

    fill(result, { amount: '' })

    expect(submit(result)).toBe('amount')
    expect(result.current.confirmation).toBeNull()
  })

  it('keeps the form open and saves nothing when the confirmation is cancelled', () => {
    const { result } = openSettle()
    submit(result)

    act(() => result.current.cancelConfirmation())

    expect(result.current.confirmation).toBeNull()
    expect(result.current.isOpen).toBe(true)
    expect(settleAccountMock).not.toHaveBeenCalled()
  })

  it('saves the values as entered on confirmation, closes and reports it', async () => {
    const { result, onSaved } = openSettle()
    submit(result)

    act(() => result.current.confirmSettle())

    expect(result.current.isSaving).toBe(true)
    expect(result.current.confirmation).toBeNull()
    await waitFor(() => expect(result.current.isOpen).toBe(false))
    expect(settleAccountMock).toHaveBeenCalledWith('mae', { date: '2026-10-31', description: 'Settlement', note: null, paidBy: 'Other', currency: 'BRL', amount: 167.88, convertedValue: 7.94 })
    expect(onSaved).toHaveBeenCalledWith('Settlement saved')
  })

  it('sends no converted value when it is left empty', async () => {
    const { result } = openSettle()
    fill(result, { convertedValue: '' })
    submit(result)

    act(() => result.current.confirmSettle())

    await waitFor(() => expect(settleAccountMock).toHaveBeenCalledWith('mae', expect.objectContaining({ convertedValue: null })))
  })

  it('keeps a warning the save returned in the saved message', async () => {
    const warning = 'The GBP value rounds to 0.00, so it was left empty.'
    settleAccountMock.mockResolvedValue({ ...ENTRY, warning })
    const { result, onSaved } = openSettle()
    submit(result)

    act(() => result.current.confirmSettle())

    await waitFor(() => expect(onSaved).toHaveBeenCalledWith(`Settlement saved. ${warning}`))
    expect(result.current.isOpen).toBe(false)
  })

  it('shows a server refusal in the form and keeps the values', async () => {
    settleAccountMock.mockRejectedValue(new ApiError('Nothing to settle.', 400))
    const { result, onSaved } = openSettle()
    submit(result)

    act(() => result.current.confirmSettle())

    await waitFor(() => expect(result.current.saveError).toBe('Nothing to settle.'))
    expect(result.current.isOpen).toBe(true)
    expect(result.current.values.amount).toBe('167.88')
    expect(onSaved).not.toHaveBeenCalled()
  })
})

describe('useSettlementEntryForm', () => {
  beforeEach(() => {
    pinDate('2026-11-07T12:00:00+00:00')
    sessionStorage.clear()
    createSettlementEntryMock.mockReset().mockResolvedValue(ENTRY)
    updateSettlementEntryMock.mockReset().mockResolvedValue(ENTRY)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  describe('opening Add', () => {
    it('defaults to the main currency and the last day of a past month, with no payer chosen and nothing dirty', () => {
      const { result } = renderForm()

      act(() => result.current.openAdd())

      expect(result.current.isOpen).toBe(true)
      expect(result.current.isEditing).toBe(false)
      expect(result.current.values).toMatchObject({ date: '2026-10-31', currency: 'BRL', paidBy: '', description: '', amount: '', note: '' })
      expect(result.current.isDirty).toBe(false)
    })

    it('defaults to today when the current month is viewed', () => {
      const { result } = renderForm(MAE, { year: 2026, month: 11 })

      act(() => result.current.openAdd())

      expect(result.current.values.date).toBe('2026-11-07')
    })

    it('asks the currency only with two currencies and never offers a converted value', () => {
      const two = renderForm()
      const one = renderForm(SAMUEL)

      act(() => two.result.current.openAdd())
      act(() => one.result.current.openAdd())

      expect(two.result.current.asksCurrency).toBe(true)
      expect(two.result.current.currencies).toEqual(['BRL', 'GBP'])
      expect(two.result.current.convertedCurrency).toBeNull()
      expect(one.result.current.asksCurrency).toBe(false)
      expect(one.result.current.values.currency).toBe('GBP')
    })

    it('names the other participant, falling back to the account name', () => {
      expect(renderForm().result.current.otherParticipant).toBe('Mãe')
      expect(renderForm(SAMUEL).result.current.otherParticipant).toBe('Samuel')
    })

    it('does not open without a loaded account', () => {
      const { result } = renderHook(() => useSettlementEntryForm(null, { year: 2026, month: 10 }, vi.fn()))

      act(() => result.current.openAdd())

      expect(result.current.isOpen).toBe(false)
    })
  })

  describe('validation', () => {
    it('reports every message and returns the first invalid field in field order, without saving', () => {
      const { result } = renderForm()
      act(() => result.current.openAdd())
      fill(result, { date: '', currency: '' })

      expect(submit(result)).toBe('date')

      expect(result.current.fieldErrors).toEqual({
        date: 'Enter a date.',
        paidBy: 'Choose who paid.',
        currency: 'Choose the currency.',
        description: 'Description is required.',
        amount: 'Amount must be greater than zero.',
        convertedValue: undefined,
      })
      expect(createSettlementEntryMock).not.toHaveBeenCalled()
    })

    it.each([
      [{ date: '2026-11-08' }, 'date', 'Date must not be in the future.'],
      [{ paidBy: '' }, 'paidBy', 'Choose who paid.'],
      [{ description: '   ' }, 'description', 'Description is required.'],
      [{ amount: '0' }, 'amount', 'Amount must be greater than zero.'],
      [{ amount: '-5' }, 'amount', 'Amount must be greater than zero.'],
      [{ amount: '0.004' }, 'amount', 'Amount must be greater than zero.'],
    ] as const)('refuses %o on add', (change, field, message) => {
      const { result } = renderForm()
      act(() => result.current.openAdd())
      fillValidEntry(result)
      fill(result, change)

      expect(submit(result)).toBe(field)

      expect(result.current.fieldErrors[field]).toBe(message)
      expect(createSettlementEntryMock).not.toHaveBeenCalled()
    })

    it('refuses a negative converted value on edit', () => {
      const { result } = renderForm()
      act(() => result.current.openEdit(ENTRY))
      fill(result, { convertedValue: '-1' })

      expect(submit(result)).toBe('convertedValue')

      expect(result.current.fieldErrors.convertedValue).toBe('The converted value must be greater than zero.')
    })

    it('clears the error of the field being changed', () => {
      const { result } = renderForm()
      act(() => result.current.openAdd())
      submit(result)

      fill(result, { paidBy: 'You' })

      expect(result.current.fieldErrors.paidBy).toBeUndefined()
      expect(result.current.fieldErrors.description).toBe('Description is required.')
    })
  })

  describe('saving an Add', () => {
    it('posts the entered values to the account and toasts', async () => {
      const { result, onSaved } = renderForm()
      act(() => result.current.openAdd())
      fillValidEntry(result)
      fill(result, { currency: 'GBP' })

      submit(result)

      expect(createSettlementEntryMock).toHaveBeenCalledWith('mae', { date: '2026-10-31', description: 'Farmácia', note: 'metade', paidBy: 'You', amount: 150, currency: 'GBP' })
      await waitFor(() => expect(onSaved).toHaveBeenCalledWith('Entry added'))
    })

    it('sends a blank note as null', async () => {
      const { result, onSaved } = renderForm()
      act(() => result.current.openAdd())
      fillValidEntry(result)
      fill(result, { note: '  ' })

      submit(result)

      expect(createSettlementEntryMock.mock.calls[0][1].note).toBeNull()
      await waitFor(() => expect(onSaved).toHaveBeenCalled())
    })

    it('stays open keeping date, payer and currency, empties the rest and is not dirty', async () => {
      const { result, onSaved } = renderForm()
      act(() => result.current.openAdd())
      fillValidEntry(result)
      fill(result, { date: '2026-10-05', currency: 'GBP' })

      submit(result)
      await waitFor(() => expect(onSaved).toHaveBeenCalled())

      expect(result.current.isOpen).toBe(true)
      expect(result.current.isSaving).toBe(false)
      expect(result.current.values).toMatchObject({ date: '2026-10-05', paidBy: 'You', currency: 'GBP', description: '', amount: '', note: '' })
      expect(result.current.isDirty).toBe(false)
    })

    it('remembers the date for the next Add in the same month', async () => {
      const first = renderForm()
      act(() => first.result.current.openAdd())
      fillValidEntry(first.result)
      fill(first.result, { date: '2026-10-05' })
      submit(first.result)
      await waitFor(() => expect(first.onSaved).toHaveBeenCalled())

      const second = renderForm()
      act(() => second.result.current.openAdd())

      expect(second.result.current.values.date).toBe('2026-10-05')
    })

    it('[AC P60-F12-settlement-screen-04] shows the warning above the next entry until the next change', async () => {
      const warning = 'No exchange rate found for 31 Oct 2026; the GBP value is empty.'
      createSettlementEntryMock.mockResolvedValue({ ...ENTRY, convertedValue: null, warning })
      const { result, onSaved } = renderForm()
      act(() => result.current.openAdd())
      fillValidEntry(result)

      submit(result)
      await waitFor(() => expect(onSaved).toHaveBeenCalled())

      expect(result.current.warning).toBe(warning)
      fill(result, { description: 'Next' })
      expect(result.current.warning).toBeNull()
    })

    it('does not save twice while saving', () => {
      createSettlementEntryMock.mockReturnValue(new Promise(() => {}))
      const { result } = renderForm()
      act(() => result.current.openAdd())
      fillValidEntry(result)

      submit(result)
      expect(result.current.isSaving).toBe(true)
      submit(result)

      expect(createSettlementEntryMock).toHaveBeenCalledTimes(1)
    })

    it('does not reopen a form cancelled while its save was running', async () => {
      let finishSave: (entry: SettlementEntryDto) => void = () => {}
      createSettlementEntryMock.mockReturnValue(new Promise((resolve) => { finishSave = resolve }))
      const { result, onSaved } = renderForm()
      act(() => result.current.openAdd())
      fillValidEntry(result)
      submit(result)

      act(() => result.current.cancel())
      await act(async () => finishSave(ENTRY))

      expect(onSaved).toHaveBeenCalledWith('Entry added')
      expect(result.current.isOpen).toBe(false)
    })
  })

  describe('save failures', () => {
    it('does not show a failure on a form opened after the failed save was cancelled', async () => {
      let failSave: (reason: Error) => void = () => {}
      createSettlementEntryMock.mockReturnValue(new Promise((_, reject) => { failSave = reject }))
      const { result } = renderForm()
      act(() => result.current.openAdd())
      fillValidEntry(result)
      submit(result)

      act(() => result.current.cancel())
      act(() => result.current.openAdd())
      await act(async () => failSave(new Error('Refused.')))

      expect(result.current.saveError).toBeNull()
    })

    it('shows the server message and keeps the values', async () => {
      createSettlementEntryMock.mockRejectedValue(new Error('Mãe is inactive.'))
      const { result, onSaved } = renderForm()
      act(() => result.current.openAdd())
      fillValidEntry(result)

      submit(result)
      await waitFor(() => expect(result.current.saveError).toBe('Mãe is inactive.'))

      expect(result.current.isSaving).toBe(false)
      expect(result.current.values).toMatchObject({ paidBy: 'You', description: 'Farmácia', amount: '150' })
      expect(result.current.isDirty).toBe(true)
      expect(onSaved).not.toHaveBeenCalled()
    })

    it('shows the network message when the request cannot reach the server', async () => {
      updateSettlementEntryMock.mockRejectedValue(new ApiError('Failed to fetch', NETWORK_FAILURE_STATUS))
      const { result } = renderForm()
      act(() => result.current.openEdit(ENTRY))

      submit(result)

      await waitFor(() => expect(result.current.saveError).toBe(NETWORK_SAVE_ERROR))
    })

    it('clears the server message on the next change', async () => {
      createSettlementEntryMock.mockRejectedValue(new Error('Refused.'))
      const { result } = renderForm()
      act(() => result.current.openAdd())
      fillValidEntry(result)
      submit(result)
      await waitFor(() => expect(result.current.saveError).toBe('Refused.'))

      fill(result, { note: 'x' })

      expect(result.current.saveError).toBeNull()
    })
  })

  describe('editing', () => {
    it('opens on the entry, not dirty, with its converted currency', () => {
      const { result } = renderForm()

      act(() => result.current.openEdit(ENTRY))

      expect(result.current.isEditing).toBe(true)
      expect(result.current.asksCurrency).toBe(false)
      expect(result.current.convertedCurrency).toBe('GBP')
      expect(result.current.values).toEqual({ date: '2026-10-10', paidBy: 'Other', currency: 'BRL', description: 'Farmácia', amount: '150', convertedValue: '21.5', note: 'metade' })
      expect(result.current.isDirty).toBe(false)
    })

    it('[AC P60-F12-settlement-screen-02] puts every field but the currency, leaving the converted value as typed, then closes', async () => {
      const { result, onSaved } = renderForm()
      act(() => result.current.openEdit(ENTRY))
      fill(result, { date: '2026-10-09', description: 'Mercado', note: '', paidBy: 'You', amount: '180', convertedValue: '25.6' })
      expect(result.current.isDirty).toBe(true)

      submit(result)

      expect(updateSettlementEntryMock).toHaveBeenCalledWith('mae', 'e1', { date: '2026-10-09', description: 'Mercado', note: null, paidBy: 'You', amount: 180, convertedValue: 25.6 })
      await waitFor(() => expect(onSaved).toHaveBeenCalledWith('Entry updated'))
      expect(result.current.isOpen).toBe(false)
    })

    it('sends an emptied converted value as null', async () => {
      const { result, onSaved } = renderForm()
      act(() => result.current.openEdit(ENTRY))
      fill(result, { convertedValue: '' })

      submit(result)

      expect(updateSettlementEntryMock.mock.calls[0][2].convertedValue).toBeNull()
      await waitFor(() => expect(onSaved).toHaveBeenCalled())
    })

    it('sends no converted value for a one-currency entry', async () => {
      const { result, onSaved } = renderForm(SAMUEL)
      act(() => result.current.openEdit({ ...ENTRY, accountId: 'samuel', currency: 'GBP', convertedCurrency: null, convertedValue: null }))

      expect(result.current.convertedCurrency).toBeNull()
      submit(result)

      expect(updateSettlementEntryMock).toHaveBeenCalledWith('samuel', 'e1', expect.objectContaining({ convertedValue: null }))
      await waitFor(() => expect(onSaved).toHaveBeenCalled())
    })

    it('stays open on the saved values, not dirty, when the response carries a warning, and Cancel clears it', async () => {
      const warning = 'The GBP value rounds to 0.00, so it was left empty.'
      updateSettlementEntryMock.mockResolvedValue({ ...ENTRY, amount: 200, convertedValue: null, warning })
      const { result, onSaved } = renderForm()
      act(() => result.current.openEdit(ENTRY))
      fill(result, { amount: '200', convertedValue: '0.001' })

      submit(result)
      await waitFor(() => expect(onSaved).toHaveBeenCalledWith('Entry updated'))

      expect(result.current.isOpen).toBe(true)
      expect(result.current.isEditing).toBe(true)
      expect(result.current.warning).toBe(warning)
      expect(result.current.values).toMatchObject({ amount: '200', convertedValue: '' })
      expect(result.current.isDirty).toBe(false)

      act(() => result.current.cancel())
      expect(result.current.isOpen).toBe(false)
      expect(result.current.warning).toBeNull()
    })
  })
})
