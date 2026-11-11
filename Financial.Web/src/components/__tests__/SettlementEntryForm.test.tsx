import { useState } from 'react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { SettlementEntryFormModel } from '../../hooks/useSettlementEntryForm'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import SettlementEntryForm from '../SettlementEntryForm'
import { getErrorAlert } from '../../test-utils/errorAlert'

function addState(overrides: Partial<SettlementEntryFormModel> = {}): SettlementEntryFormModel {
  return {
    isOpen: true,
    isEditing: false,
    isSettling: false,
    values: { date: '2026-10-31', paidBy: '', currency: 'BRL', description: '', amount: '', convertedValue: '', note: '' },
    currencies: ['BRL', 'GBP'],
    otherParticipant: 'Mãe',
    asksCurrency: true,
    convertedCurrency: null,
    isSaving: false,
    saveError: null,
    fieldErrors: {},
    warning: null,
    impliedRate: null,
    confirmation: null,
    isDirty: false,
    openAdd: vi.fn(),
    openEdit: vi.fn(),
    openSettle: vi.fn(),
    cancel: vi.fn(),
    setField: vi.fn(),
    submit: vi.fn(() => null),
    confirmSettle: vi.fn(),
    cancelConfirmation: vi.fn(),
    ...overrides,
  }
}

function editState(overrides: Partial<SettlementEntryFormModel> = {}): SettlementEntryFormModel {
  return addState({
    isEditing: true,
    asksCurrency: false,
    convertedCurrency: 'GBP',
    values: { date: '2026-10-10', paidBy: 'Other', currency: 'BRL', description: 'Farmácia', amount: '150', convertedValue: '21.5', note: 'metade' },
    ...overrides,
  })
}

function settleState(overrides: Partial<SettlementEntryFormModel> = {}): SettlementEntryFormModel {
  return addState({
    isSettling: true,
    convertedCurrency: 'GBP',
    impliedRate: 'Implied rate: 1 GBP = R$21.14',
    values: { date: '2026-11-07', paidBy: 'Other', currency: 'BRL', description: 'Settlement', amount: '167.88', convertedValue: '7.94', note: '' },
    ...overrides,
  })
}

type User = ReturnType<typeof userEvent.setup>

function ConfirmingSettle({ confirmedError = null }: { confirmedError?: string | null }) {
  const [confirmation, setConfirmation] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const state = settleState({
    confirmation,
    saveError,
    submit: () => {
      setConfirmation('Both totals become zero.')
      return null
    },
    confirmSettle: () => {
      setConfirmation(null)
      setSaveError(confirmedError)
    },
    cancelConfirmation: () => setConfirmation(null),
  })
  return <SettlementEntryForm form={state} onCancel={vi.fn()} />
}

const findConfirmation = () =>
  waitFor(() => {
    const dialog = screen.getAllByRole('dialog', { hidden: true }).find((candidate) => candidate.textContent?.includes('Both totals become zero.'))
    expect(dialog).toBeDefined()
    return dialog as HTMLElement
  })

function renderForm(state: SettlementEntryFormModel, onCancel = vi.fn()) {
  return render(<SettlementEntryForm form={state} onCancel={onCancel} />)
}

describe('SettlementEntryForm', () => {
  it('[AC P60-F12-settlement-screen-08] titles New entry and shows the fields in the documented order', () => {
    renderForm(addState())

    expect(screen.getByRole('heading', { name: 'New entry' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add entry' })).toBeEnabled()
    const labels = Array.from(document.querySelectorAll('form label'))
      .map((label) => label.textContent?.replace('*', '').trim())
      .filter((label) => !['You', 'Mãe', 'BRL', 'GBP'].includes(label ?? ''))
    expect(labels).toEqual(['Date', 'Paid by', 'Currency', 'Description', 'Amount (BRL)', 'Note'])
  })

  it('[AC P60-F12-settlement-screen-05] titles Settle, puts the currency first and shows the implied rate', () => {
    renderForm(settleState())

    expect(screen.getByRole('heading', { name: 'Settle' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save settlement' })).toBeEnabled()
    const labels = Array.from(document.querySelectorAll('form label'))
      .map((label) => label.textContent?.replace('*', '').trim())
      .filter((label) => !['You', 'Mãe', 'BRL', 'GBP'].includes(label ?? ''))
    expect(labels).toEqual(['Currency', 'Date', 'Paid by', 'Description', 'Amount (BRL)', 'GBP value', 'Note'])
    expect(screen.getByText('Implied rate: 1 GBP = R$21.14')).toHaveAttribute('aria-live', 'polite')
  })

  it('[AC P60-F12-settlement-screen-05] confirms with the outcome and hands the answer back', () => {
    const confirmSettle = vi.fn()
    const cancelConfirmation = vi.fn()
    renderForm(settleState({ confirmation: 'Both totals become zero.', confirmSettle, cancelConfirmation }))

    const dialog = screen.getByRole('dialog', { name: 'Save this settlement?' })
    expect(within(dialog).getByText('Both totals become zero.')).toBeInTheDocument()
    fireEvent.click(within(dialog).getByRole('button', { name: 'Save settlement' }))
    expect(confirmSettle).toHaveBeenCalledOnce()
    fireEvent.click(within(dialog).getByRole('button', { name: 'Cancel' }))
    expect(cancelConfirmation).toHaveBeenCalledOnce()
  })

  it('has no implied rate region on a one-currency Settle', () => {
    renderForm(settleState({ convertedCurrency: null, impliedRate: null, asksCurrency: false, currencies: ['GBP'] }))

    expect(document.querySelector('[aria-live="polite"]')).toBeNull()
  })

  it('shows no confirmation and no rate outside Settle', () => {
    renderForm(addState())

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.queryByText(/Implied rate/)).not.toBeInTheDocument()
  })

  it('offers You and the other participant with nothing selected', () => {
    renderForm(addState())

    const paidBy = screen.getByRole('radiogroup', { name: /^Paid by/ })
    expect(within(paidBy).getByRole('radio', { name: 'You' })).not.toBeChecked()
    expect(within(paidBy).getByRole('radio', { name: 'Mãe' })).not.toBeChecked()
  })

  it('[AC P60-F12-settlement-screen-04] asks the currency on a two-currency add with the main one chosen, and hides the converted value', () => {
    renderForm(addState())

    const currency = screen.getByRole('radiogroup', { name: /^Currency/ })
    expect(within(currency).getByRole('radio', { name: 'BRL' })).toBeChecked()
    expect(within(currency).getByRole('radio', { name: 'GBP' })).not.toBeChecked()
    expect(screen.queryByLabelText(/value$/)).not.toBeInTheDocument()
  })

  it('[AC P60-F12-settlement-screen-04] does not ask the currency on a one-currency add', () => {
    renderForm(addState({ asksCurrency: false, currencies: ['GBP'], values: { ...addState().values, currency: 'GBP' } }))

    expect(screen.queryByRole('radiogroup', { name: /^Currency/ })).not.toBeInTheDocument()
    expect(screen.getByLabelText(/^Amount \(GBP\)/)).toBeInTheDocument()
  })

  it('caps the date at today', () => {
    renderForm(addState())

    expect(screen.getByLabelText(/^Date/)).toHaveAttribute('max')
  })

  it('[AC P60-F12-settlement-screen-02] edits every field but the currency, which reads as text', () => {
    renderForm(editState())

    expect(screen.getByRole('heading', { name: 'Edit entry' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeEnabled()
    expect(screen.queryByRole('radiogroup', { name: /^Currency/ })).not.toBeInTheDocument()
    expect(screen.getByText('BRL')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-10-10')
    expect(screen.getByRole('radio', { name: 'Mãe' })).toBeChecked()
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Farmácia')
    expect(screen.getByLabelText(/^Amount \(BRL\)/)).toHaveValue(150)
    expect(screen.getByLabelText('GBP value')).toHaveValue(21.5)
    expect(screen.getByText('Leave empty when there is no GBP value.')).toBeInTheDocument()
    expect(screen.getByLabelText('Note')).toHaveValue('metade')
  })

  it('shows no converted value when editing a one-currency entry', () => {
    renderForm(editState({ convertedCurrency: null, currencies: ['GBP'] }))

    expect(screen.queryByLabelText(/value$/)).not.toBeInTheDocument()
  })

  it('reports every field change and cancels', () => {
    const setField = vi.fn()
    const onCancel = vi.fn()
    renderForm(editState({ setField }), onCancel)

    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-10-01' } })
    fireEvent.click(screen.getByRole('radio', { name: 'You' }))
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'd' } })
    fireEvent.change(screen.getByLabelText(/^Amount/), { target: { value: '1' } })
    fireEvent.change(screen.getByLabelText('GBP value'), { target: { value: '2' } })
    fireEvent.change(screen.getByLabelText('Note'), { target: { value: 'n' } })
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(setField.mock.calls).toEqual([
      ['date', '2026-10-01'],
      ['paidBy', 'You'],
      ['description', 'd'],
      ['amount', '1'],
      ['convertedValue', '2'],
      ['note', 'n'],
    ])
    expect(onCancel).toHaveBeenCalledTimes(1)
  })

  it('reports the currency choice', () => {
    const setField = vi.fn()
    renderForm(addState({ setField }))

    fireEvent.click(screen.getByRole('radio', { name: 'GBP' }))

    expect(setField).toHaveBeenCalledWith('currency', 'GBP')
  })

  it.each([
    ['date', () => screen.getByLabelText(/^Date/)],
    ['paidBy', () => screen.getByRole('radio', { name: 'You' })],
    ['currency', () => screen.getByRole('radio', { name: 'BRL' })],
    ['description', () => screen.getByLabelText(/^Description/)],
    ['amount', () => screen.getByLabelText(/^Amount/)],
  ] as const)('[AC P60-F12-settlement-screen-08] focuses %s when the submit reports it invalid', (field, control) => {
    renderForm(addState({ submit: vi.fn(() => field) }))

    fireEvent.click(screen.getByRole('button', { name: 'Add entry' }))

    expect(control()).toHaveFocus()
  })

  it('shows the field messages', () => {
    renderForm(addState({ fieldErrors: { paidBy: 'Choose who paid.', amount: 'Amount must be greater than zero.' } }))

    expect(screen.getByText('Choose who paid.')).toBeInTheDocument()
    expect(screen.getByText('Amount must be greater than zero.')).toBeInTheDocument()
  })

  it('[AC P60-F12-settlement-screen-08] disables the fields and keeps Saving… focused while it saves', () => {
    const { rerender } = renderForm(addState())
    screen.getByRole('button', { name: 'Add entry' }).focus()

    rerender(<SettlementEntryForm form={addState({ isSaving: true })} onCancel={vi.fn()} />)

    const save = screen.getByRole('button', { name: 'Saving…' })
    expect(save).toHaveAttribute('aria-disabled', 'true')
    expect(save).toHaveFocus()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
    expect(screen.getByLabelText(/^Description/)).toBeDisabled()
    expect(screen.getByRole('radio', { name: 'You' })).toBeDisabled()
  })

  it('[AC P60-F12-settlement-screen-08] announces the server message and moves focus to it', () => {
    renderForm(addState({ saveError: 'Mãe is inactive.' }))

    expect(getErrorAlert()).toHaveTextContent('Mãe is inactive.')
    expect(getErrorAlert()).toHaveFocus()
  })

  it.each([
    ['a click on Save', (user: User) => user.click(screen.getByRole('button', { name: 'Save settlement' }))],
    ['Enter in a field', async (user: User) => {
      await user.click(screen.getByLabelText(/^Description/))
      await user.keyboard('{Enter}')
    }],
  ])('returns focus to Save when the confirmation is cancelled after %s', async (_, submitFrom) => {
    const user = userEvent.setup()
    render(<ConfirmingSettle />)

    await submitFrom(user)
    await user.click(within(await findConfirmation()).getByRole('button', { name: 'Cancel', hidden: true }))

    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
    await waitFor(() => expect(screen.getByRole('button', { name: 'Save settlement' })).toHaveFocus())
  })

  it('moves focus to the alert when the confirmed settlement fails', async () => {
    const user = userEvent.setup()
    render(<ConfirmingSettle confirmedError="Mãe is inactive." />)

    await user.click(screen.getByRole('button', { name: 'Save settlement' }))
    await user.click(within(await findConfirmation()).getByRole('button', { name: 'Save settlement', hidden: true }))

    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
    await waitFor(() => expect(getErrorAlert()).toHaveFocus())
  })

  it('[AC P60-F12-settlement-screen-04] shows the warning as status', () => {
    renderForm(addState({ warning: 'No exchange rate found for 31 Oct 2026; the GBP value is empty.' }))

    expect(screen.getAllByRole('status').filter((region) => region.textContent?.includes('No exchange rate found for 31 Oct 2026; the GBP value is empty.'))).toHaveLength(1)
  })
})
