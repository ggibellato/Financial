import { describe, expect, it, vi } from 'vitest'
import type { CardCreditRefundForm as CardCreditRefundFormState } from '../../hooks/useCardCreditRefundForm'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import CardCreditRefundForm from '../CardCreditRefundForm'
import { getErrorAlert } from '../../test-utils/errorAlert'

function formState(overrides: Partial<CardCreditRefundFormState> = {}): CardCreditRefundFormState {
  return {
    isOpen: true,
    mode: 'card',
    accountName: 'BA Amex',
    counterparts: [{ id: 'barclays', label: 'Barclays', currency: 'GBP' }],
    currency: 'GBP',
    values: { counterpartId: '', amount: '35.00', date: '2026-11-05' },
    isLoading: false,
    loadError: null,
    retry: vi.fn(),
    hasNoCounterparts: false,
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
  return render(<CardCreditRefundForm refundForm={formState(overrides)} onCancel={vi.fn()} />)
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
    renderForm({ isLoading: true, counterparts: [] })

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
    expect(screen.getByRole('combobox', { name: /^To bank/ })).toHaveFocus()
    expect(screen.getByRole('button', { name: 'Refund credit to bank' })).toBeDisabled()
  })

  it('shows a hint and disables the submit when no bank can receive the refund', () => {
    renderForm({ hasNoCounterparts: true, counterparts: [] })

    expect(screen.getByText('No active bank in GBP can receive this refund.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Refund credit to bank' })).toBeDisabled()
  })

  it('disables the fields and keeps Saving… focused while the refund saves', () => {
    const { rerender } = renderForm()
    screen.getByRole('button', { name: 'Refund credit to bank' }).focus()

    rerender(<CardCreditRefundForm refundForm={formState({ isSaving: true })} onCancel={vi.fn()} />)

    const save = screen.getByRole('button', { name: 'Saving…' })
    expect(save).toHaveAttribute('aria-disabled', 'true')
    expect(save).toHaveFocus()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
    expect(screen.getByRole('combobox', { name: /^To bank/ })).toBeDisabled()
  })

  it.each([
    ['counterpartId', /^To bank/],
    ['amount', /^Amount/],
    ['date', /^Date/],
  ] as const)('focuses %s when the submit reports it invalid', (field, label) => {
    renderForm({ submit: vi.fn(() => field) })

    fireEvent.click(screen.getByRole('button', { name: 'Refund credit to bank' }))

    expect(screen.getByLabelText(label)).toHaveFocus()
  })

  it('shows the field message', () => {
    renderForm({ fieldErrors: { amount: "The card's credit is 35.00 GBP." } })

    expect(screen.getByText("The card's credit is 35.00 GBP.")).toBeInTheDocument()
  })

  it('announces the server message and moves focus to it', () => {
    renderForm({ saveError: 'The server refused.' })

    expect(getErrorAlert()).toHaveTextContent('The server refused.')
    expect(getErrorAlert()).toHaveFocus()
  })

  it('reports each field change and cancels', () => {
    const setField = vi.fn()
    const onCancel = vi.fn()
    render(<CardCreditRefundForm refundForm={formState({ setField })} onCancel={onCancel} />)

    fireEvent.change(screen.getByRole('combobox', { name: /^To bank/ }), { target: { value: 'barclays' } })
    fireEvent.change(screen.getByLabelText(/^Amount/), { target: { value: '12' } })
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-11-01' } })
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(setField.mock.calls).toEqual([
      ['counterpartId', 'barclays'],
      ['amount', '12'],
      ['date', '2026-11-01'],
    ])
    expect(onCancel).toHaveBeenCalledTimes(1)
  })
})

describe('CardCreditRefundForm in bank mode', () => {
  const bankState = (overrides: Partial<CardCreditRefundFormState> = {}) =>
    formState({
      mode: 'bank',
      accountName: 'Barclays',
      counterparts: [{ id: 'visa', label: 'Platinum Visa (inactive) · Credit 12.50', currency: 'GBP', credit: 12.5 }],
      values: { counterpartId: '', amount: '', date: '2026-11-05' },
      ...overrides,
    })

  it('[AC P60-F15-card-refunds-and-credit-05] picks the card first and shows the bank locked, under Receive card credit', () => {
    render(<CardCreditRefundForm refundForm={bankState()} onCancel={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Receive card credit' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Receive card credit' })).toBeEnabled()
    expect(screen.getByRole('combobox', { name: /^From/ })).toBeInTheDocument()
    expect(screen.queryByRole('combobox', { name: /^To bank/ })).not.toBeInTheDocument()
    expect(screen.getByText('Barclays')).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Platinum Visa (inactive) · Credit 12.50' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Amount \(GBP\)/)).toHaveAttribute('placeholder', '—')
  })

  it('shows the loading text for cards', () => {
    render(<CardCreditRefundForm refundForm={bankState({ isLoading: true, counterparts: [] })} onCancel={vi.fn()} />)

    expect(screen.getByRole('combobox', { name: /^From/ })).toHaveAttribute('aria-busy', 'true')
    expect(screen.getByRole('option', { name: 'Loading cards…' })).toBeInTheDocument()
  })

  it('shows a hint and disables the submit when no card in the currency has credit', () => {
    render(<CardCreditRefundForm refundForm={bankState({ hasNoCounterparts: true, counterparts: [] })} onCancel={vi.fn()} />)

    expect(screen.getByText('No card in GBP has credit to refund.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Receive card credit' })).toBeDisabled()
  })

  it('focuses the card picker when it is the first invalid field', () => {
    render(<CardCreditRefundForm refundForm={bankState({ submit: vi.fn(() => 'counterpartId' as const) })} onCancel={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Receive card credit' }))

    expect(screen.getByRole('combobox', { name: /^From/ })).toHaveFocus()
  })
})
