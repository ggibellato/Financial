import { describe, expect, it, vi } from 'vitest'
import type { CardCreditRefundForm as CardCreditRefundFormState } from '../../hooks/useCardCreditRefundForm'
import { account } from '../../test/account'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import CardCreditRefundForm from '../CardCreditRefundForm'

const AMEX = { id: 'amex', name: 'BA Amex' }

function formState(overrides: Partial<CardCreditRefundFormState> = {}): CardCreditRefundFormState {
  return {
    isOpen: true,
    banks: [account({ id: 'barclays', name: 'Barclays' })],
    currency: 'GBP',
    values: { destinationAccountId: '', amount: '35.00', date: '2026-11-05' },
    isLoading: false,
    loadError: null,
    retry: vi.fn(),
    hasNoBanks: false,
    isSaving: false,
    saveError: null,
    fieldErrors: {},
    isDirty: false,
    open: vi.fn(),
    cancel: vi.fn(),
    setField: vi.fn(),
    submit: vi.fn(() => null),
    ...overrides,
  }
}

function renderForm(overrides: Partial<CardCreditRefundFormState> = {}) {
  return render(<CardCreditRefundForm refundForm={formState(overrides)} lockedCard={AMEX} onCancel={vi.fn()} />)
}

describe('CardCreditRefundForm', () => {
  it('names the action in the title and submit, shows the card as text, the amount with its currency, and caps the date at today', () => {
    renderForm()

    expect(screen.getByRole('heading', { name: 'Refund credit to bank' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Refund credit to bank' })).toBeEnabled()
    expect(screen.getByText('BA Amex')).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Barclays' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Amount \(GBP\)/)).toHaveValue(35)
    expect(screen.getByLabelText(/^Date/)).toHaveAttribute('max')
  })

  it('keeps the bank picker focusable and busy while the banks load', () => {
    renderForm({ isLoading: true, banks: [] })

    const bank = screen.getByRole('combobox', { name: /^To bank/ })
    expect(bank).toBeEnabled()
    expect(bank).toHaveAttribute('aria-busy', 'true')
    expect(screen.getByRole('option', { name: 'Loading banks…' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Refund credit to bank' })).toBeDisabled()
  })

  it('offers Try again and disables the submit when the banks failed to load', () => {
    const retry = vi.fn()
    renderForm({ loadError: 'Unable to load the banks', retry })

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(retry).toHaveBeenCalledTimes(1)
    expect(screen.getByRole('button', { name: 'Refund credit to bank' })).toBeDisabled()
  })

  it('shows a hint and disables the submit when no bank can receive the refund', () => {
    renderForm({ hasNoBanks: true, banks: [] })

    expect(screen.getByText('No active bank in GBP can receive this refund.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Refund credit to bank' })).toBeDisabled()
  })

  it('disables the form and shows Saving... while the refund saves', () => {
    renderForm({ isSaving: true })

    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
    expect(screen.getByRole('combobox', { name: /^To bank/ })).toBeDisabled()
  })

  it.each([
    ['destinationAccountId', /^To bank/],
    ['amount', /^Amount/],
    ['date', /^Date/],
  ] as const)('focuses %s when the submit reports it invalid', (field, label) => {
    renderForm({ submit: vi.fn(() => field) })

    fireEvent.click(screen.getByRole('button', { name: 'Refund credit to bank' }))

    expect(screen.getByLabelText(label)).toHaveFocus()
  })

  it('shows the field message and the server message', () => {
    renderForm({ fieldErrors: { amount: "The card's credit is 35.00 GBP." }, saveError: 'The server refused.' })

    expect(screen.getByText("The card's credit is 35.00 GBP.")).toBeInTheDocument()
    expect(screen.getByText('The server refused.')).toBeInTheDocument()
  })

  it('reports each field change and cancels', () => {
    const setField = vi.fn()
    const onCancel = vi.fn()
    render(<CardCreditRefundForm refundForm={formState({ setField })} lockedCard={AMEX} onCancel={onCancel} />)

    fireEvent.change(screen.getByRole('combobox', { name: /^To bank/ }), { target: { value: 'barclays' } })
    fireEvent.change(screen.getByLabelText(/^Amount/), { target: { value: '12' } })
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-11-01' } })
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(setField.mock.calls).toEqual([
      ['destinationAccountId', 'barclays'],
      ['amount', '12'],
      ['date', '2026-11-01'],
    ])
    expect(onCancel).toHaveBeenCalledTimes(1)
  })
})
