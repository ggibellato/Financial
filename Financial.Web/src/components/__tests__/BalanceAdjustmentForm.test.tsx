import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import BalanceAdjustmentForm from '../BalanceAdjustmentForm'

const baseProps = {
  isEditing: false,
  bankName: 'Barclays',
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
})
