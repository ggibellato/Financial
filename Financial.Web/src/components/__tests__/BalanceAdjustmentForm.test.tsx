import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import BalanceAdjustmentForm from '../BalanceAdjustmentForm'
import type { BankDto } from '../../api/types'

const BANKS: BankDto[] = [
  { name: 'Barclays', roundUpEnabled: false },
  { name: 'Trading212', roundUpEnabled: true },
]

const baseProps = {
  isEditing: false,
  bankName: 'Barclays',
  banks: BANKS,
  currentBalance: 100,
  date: '2026-07-25',
  targetBalance: '',
  note: '',
  isSaving: false,
  saveError: null,
  saveErrorField: null,
  savedDelta: null,
  onFieldChange: vi.fn(),
  onSave: vi.fn(),
  onCancel: vi.fn(),
}

describe('BalanceAdjustmentForm', () => {
  it('renders the current calculated balance from the currentBalance prop', () => {
    render(<BalanceAdjustmentForm {...baseProps} bankName="Barclays" currentBalance={2344.37} />)

    expect(screen.getByText('Current calculated balance for Barclays: £2,344.37')).toBeInTheDocument()
  })

  it('renders the create form title', () => {
    const { container } = render(<BalanceAdjustmentForm {...baseProps} />)

    expect(container.querySelector('.monthly-page__form-title')).toHaveTextContent('Correct Balance')
  })

  it('renders the edit form title and pre-filled values', () => {
    const { container } = render(
      <BalanceAdjustmentForm {...baseProps} isEditing targetBalance="150" note="Matched statement" />,
    )

    expect(container.querySelector('.monthly-page__form-title')).toHaveTextContent('Edit Balance Adjustment')
    expect(screen.getByLabelText('Target Balance')).toHaveValue(150)
    expect(screen.getByLabelText('Note')).toHaveValue('Matched statement')
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
  })

  it('shows the saved-state confirmation with a positive delta and hides the editable fields', () => {
    render(<BalanceAdjustmentForm {...baseProps} savedDelta={4.2} />)

    expect(screen.getByText('Adjustment of £4.20 recorded')).toBeInTheDocument()
    expect(screen.queryByLabelText('Target Balance')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Close' })).toBeInTheDocument()
  })

  it('shows the saved-state confirmation with a negative delta', () => {
    render(<BalanceAdjustmentForm {...baseProps} savedDelta={-4.2} />)

    expect(screen.getByText('Adjustment of -£4.20 recorded')).toBeInTheDocument()
  })

  it('calls onCancel when the Close button is clicked in the saved state', () => {
    const onCancel = vi.fn()
    render(<BalanceAdjustmentForm {...baseProps} savedDelta={4.2} onCancel={onCancel} />)

    fireEvent.click(screen.getByRole('button', { name: 'Close' }))

    expect(onCancel).toHaveBeenCalled()
  })

  it('calls onSave and onCancel', () => {
    const onSave = vi.fn()
    const onCancel = vi.fn()
    render(<BalanceAdjustmentForm {...baseProps} onSave={onSave} onCancel={onCancel} />)

    fireEvent.click(screen.getByRole('button', { name: 'Correct Balance' }))
    expect(onSave).toHaveBeenCalled()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onCancel).toHaveBeenCalled()
  })

  it('renders a backend error under the field named by saveErrorField', () => {
    render(<BalanceAdjustmentForm {...baseProps} saveError="Balance cannot be negative." saveErrorField="targetBalance" />)

    const field = screen.getByLabelText('Target Balance').closest('.monthly-page__form-field')
    expect(field).toHaveTextContent('Balance cannot be negative.')
  })

  it('renders a general error banner when saveErrorField is null', () => {
    render(<BalanceAdjustmentForm {...baseProps} saveError="Network request failed" saveErrorField={null} />)

    expect(screen.getByText('Network request failed')).toBeInTheDocument()
  })

  it('shows Saving... and disables the button while isSaving', () => {
    render(<BalanceAdjustmentForm {...baseProps} isSaving />)

    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
  })

  it('only the Bank dropdown is enabled before a bank is chosen', () => {
    render(<BalanceAdjustmentForm {...baseProps} bankName="" currentBalance={0} />)

    expect(screen.getByLabelText('Bank')).toBeInTheDocument()
    expect(screen.queryByText(/Current calculated balance/)).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Date')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Target Balance')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Note')).not.toBeInTheDocument()
  })

  it('reveals the reference line and fields once a bank is chosen', () => {
    render(<BalanceAdjustmentForm {...baseProps} bankName="Barclays" currentBalance={100} />)

    expect(screen.getByText('Current calculated balance for Barclays: £100.00')).toBeInTheDocument()
    expect(screen.getByLabelText('Date')).toBeInTheDocument()
    expect(screen.getByLabelText('Target Balance')).toBeInTheDocument()
    expect(screen.getByLabelText('Note')).toBeInTheDocument()
  })

  it('Save stays disabled until a bank is chosen', () => {
    render(<BalanceAdjustmentForm {...baseProps} bankName="" currentBalance={0} />)

    expect(screen.getByRole('button', { name: 'Correct Balance' })).toBeDisabled()
  })

  it('calls onFieldChange with the selected bank when the Bank dropdown changes', () => {
    const onFieldChange = vi.fn()
    render(<BalanceAdjustmentForm {...baseProps} bankName="" currentBalance={0} onFieldChange={onFieldChange} />)

    fireEvent.change(screen.getByLabelText('Bank'), { target: { value: 'Trading212' } })

    expect(onFieldChange).toHaveBeenCalledWith('bankName', 'Trading212')
  })

  it('renders the bank as static text (not a dropdown) when editing', () => {
    render(<BalanceAdjustmentForm {...baseProps} isEditing bankName="Barclays" currentBalance={100} targetBalance="150" />)

    expect(screen.queryByLabelText('Bank')).not.toBeInTheDocument()
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument()
    expect(screen.getByText('Current calculated balance for Barclays: £100.00')).toBeInTheDocument()
  })
})
