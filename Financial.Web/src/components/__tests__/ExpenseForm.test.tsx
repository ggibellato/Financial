import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import ExpenseForm from '../ExpenseForm'
import type { BankDto } from '../../api/types'

const BANKS: BankDto[] = [
  { name: 'Barclays', roundUpEnabled: false },
  { name: 'Trading212', roundUpEnabled: true },
]

const baseProps = {
  isEditing: false,
  date: '',
  description: '',
  value: '',
  category: 'Mercado',
  paymentSource: 'Barclays',
  cardTag: '',
  invoiceDate: '',
  roundUpAmount: '',
  paymentMode: 'bank' as const,
  banks: BANKS,
  isSettled: false,
  isSaving: false,
  saveError: null,
  onFieldChange: vi.fn(),
  onSave: vi.fn(),
  onCancel: vi.fn(),
}

describe('ExpenseForm', () => {
  it('renders the create form with empty date/description/value fields', () => {
    render(<ExpenseForm {...baseProps} />)

    expect(screen.getByText('New Expense')).toBeInTheDocument()
    expect(screen.getByLabelText('Date')).toHaveValue('')
    expect(screen.getByLabelText('Description')).toHaveValue('')
    expect(screen.getByLabelText('Value')).toHaveValue(null)
    expect(screen.getByRole('button', { name: 'Add Expense' })).toBeInTheDocument()
  })

  it('shows the settlement note and hides payment fields when settled', () => {
    render(
      <ExpenseForm
        {...baseProps}
        isEditing
        isSettled
        paymentSource="Trading212"
        cardTag="BaAmex"
      />,
    )

    expect(screen.getByText(/Settled via its card statement/)).toBeInTheDocument()
    expect(screen.queryByLabelText('Payment Source')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Card')).not.toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('shows the bank picker and no toggle in bank mode', () => {
    render(<ExpenseForm {...baseProps} paymentMode="bank" />)
    expect(screen.getByLabelText('Payment Source')).toBeInTheDocument()
    expect(screen.queryByLabelText('Card')).not.toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('shows the card picker and no toggle in card mode', () => {
    render(<ExpenseForm {...baseProps} paymentMode="card" />)
    expect(screen.queryByLabelText('Payment Source')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Card')).toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('shows an editable invoice month field pre-filled from the date when card mode is selected', () => {
    render(<ExpenseForm {...baseProps} paymentMode="card" date="2026-07-15" />)

    const invoiceField = screen.getByLabelText('Invoice Month')
    expect(invoiceField).toBeInTheDocument()
    expect(invoiceField).not.toBeDisabled()
    expect(invoiceField).toHaveValue('2026-07')
  })

  it('persists a changed invoice month while unpaid', () => {
    const onFieldChange = vi.fn()
    render(<ExpenseForm {...baseProps} paymentMode="card" date="2026-07-15" onFieldChange={onFieldChange} />)

    fireEvent.change(screen.getByLabelText('Invoice Month'), { target: { value: '2026-08' } })

    expect(onFieldChange).toHaveBeenCalledWith('invoiceDate', '2026-08')
  })

  it('shows the invoice month field disabled once settled', () => {
    render(
      <ExpenseForm
        {...baseProps}
        isEditing
        isSettled
        paymentSource="Trading212"
        cardTag="BaAmex"
        invoiceDate="2026-07"
      />,
    )

    const invoiceField = screen.getByLabelText('Invoice Month')
    expect(invoiceField).toBeInTheDocument()
    expect(invoiceField).toBeDisabled()
    expect(invoiceField).toHaveValue('2026-07')
  })

  it('hides the invoice month field in bank mode', () => {
    render(<ExpenseForm {...baseProps} paymentMode="bank" />)

    expect(screen.queryByLabelText('Invoice Month')).not.toBeInTheDocument()
  })

  it('shows the round-up field only for a round-up-enabled bank in bank mode', () => {
    const { rerender } = render(<ExpenseForm {...baseProps} paymentMode="bank" paymentSource="Barclays" />)
    expect(screen.queryByLabelText('Round-Up')).not.toBeInTheDocument()

    rerender(<ExpenseForm {...baseProps} paymentMode="bank" paymentSource="Trading212" />)
    expect(screen.getByLabelText('Round-Up')).toBeInTheDocument()

    rerender(<ExpenseForm {...baseProps} paymentMode="card" paymentSource="Trading212" />)
    expect(screen.queryByLabelText('Round-Up')).not.toBeInTheDocument()
  })

  it('calls onSave and onCancel', () => {
    const onSave = vi.fn()
    const onCancel = vi.fn()
    render(<ExpenseForm {...baseProps} onSave={onSave} onCancel={onCancel} />)

    fireEvent.click(screen.getByRole('button', { name: 'Add Expense' }))
    expect(onSave).toHaveBeenCalled()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onCancel).toHaveBeenCalled()
  })
})
