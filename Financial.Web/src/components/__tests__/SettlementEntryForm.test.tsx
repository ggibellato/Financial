import { describe, expect, it, vi } from 'vitest'
import type { SettlementEntryFormModel } from '../../hooks/useSettlementEntryForm'
import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import SettlementEntryForm from '../SettlementEntryForm'

function addState(overrides: Partial<SettlementEntryFormModel> = {}): SettlementEntryFormModel {
  return {
    isOpen: true,
    isEditing: false,
    values: { date: '2026-10-31', paidBy: '', currency: 'BRL', description: '', amount: '', convertedValue: '', note: '' },
    currencies: ['BRL', 'GBP'],
    otherParticipant: 'Mãe',
    asksCurrency: true,
    convertedCurrency: null,
    isSaving: false,
    saveError: null,
    fieldErrors: {},
    warning: null,
    isDirty: false,
    openAdd: vi.fn(),
    openEdit: vi.fn(),
    cancel: vi.fn(),
    setField: vi.fn(),
    submit: vi.fn(() => null),
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

function renderForm(state: SettlementEntryFormModel, onCancel = vi.fn()) {
  return render(<SettlementEntryForm form={state} onCancel={onCancel} />)
}

describe('SettlementEntryForm', () => {
  it('[AC P60-F12-settlement-screen-08] titles Add entry and shows the fields in the documented order', () => {
    renderForm(addState())

    expect(screen.getByRole('heading', { name: 'Add entry' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add entry' })).toBeEnabled()
    const labels = Array.from(document.querySelectorAll('form label'))
      .map((label) => label.textContent?.replace('*', '').trim())
      .filter((label) => !['You', 'Mãe', 'BRL', 'GBP'].includes(label ?? ''))
    expect(labels).toEqual(['Date', 'Paid by', 'Currency', 'Description', 'Amount (BRL)', 'Note'])
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

  it('[AC P60-F12-settlement-screen-08] disables the form and shows Saving... while it saves', () => {
    renderForm(addState({ isSaving: true }))

    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
    expect(screen.getByLabelText(/^Description/)).toBeDisabled()
    expect(screen.getByRole('radio', { name: 'You' })).toBeDisabled()
  })

  it('[AC P60-F12-settlement-screen-08] announces the server message with focus on the submit', () => {
    renderForm(addState({ saveError: 'Mãe is inactive.' }))

    expect(screen.getByRole('alert')).toHaveTextContent('Mãe is inactive.')
    expect(screen.getByRole('button', { name: 'Add entry' })).toHaveFocus()
  })

  it('[AC P60-F12-settlement-screen-04] shows the warning as status', () => {
    renderForm(addState({ warning: 'No exchange rate found for 31 Oct 2026; the GBP value is empty.' }))

    expect(screen.getByRole('status')).toHaveTextContent('No exchange rate found for 31 Oct 2026; the GBP value is empty.')
  })
})
