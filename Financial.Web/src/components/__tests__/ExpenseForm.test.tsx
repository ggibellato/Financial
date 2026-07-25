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
  roundUpAmount: '',
  paymentMode: 'bank' as const,
  banks: BANKS,
  isSettled: false,
  isSaving: false,
  saveError: null,
  onFieldChange: vi.fn(),
  onModeChange: vi.fn(),
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

  it('shows the bank picker in bank mode and the card picker in card mode', () => {
    const { rerender } = render(<ExpenseForm {...baseProps} paymentMode="bank" />)
    expect(screen.getByLabelText('Payment Source')).toBeInTheDocument()
    expect(screen.queryByLabelText('Card')).not.toBeInTheDocument()

    rerender(<ExpenseForm {...baseProps} paymentMode="card" />)
    expect(screen.queryByLabelText('Payment Source')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Card')).toBeInTheDocument()
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
