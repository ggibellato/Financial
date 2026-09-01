import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import UkExpensePromptDialog from '../UkExpensePromptDialog'
import type { BankDto, CategoryDto, RecurringBillDto } from '../../api/types'
import { todayIsoDate } from '../../utils/formatters'

const BILL: RecurringBillDto = {
  id: 'b1',
  dueDay: 1,
  description: 'Council Tax',
  value: 120,
  area: 'UK',
  note: '',
  nitNumber: null,
  minimumWageValue: null,
  status: 'Unset',
}

const BANKS: BankDto[] = [
  { id: 'bank-1', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'cat-1', name: 'Bills', active: true, isInvestment: false, isTithe: false, hasReferences: false },
]

function renderDialog(overrides: Partial<React.ComponentProps<typeof UkExpensePromptDialog>> = {}) {
  return render(
    <UkExpensePromptDialog
      bill={BILL}
      banks={BANKS}
      categories={CATEGORIES}
      isCreatingExpense={false}
      isUpdatingStatus={false}
      expenseCreateError={null}
      statusUpdateError={null}
      isRetryOnly={false}
      onConfirm={vi.fn()}
      onSkip={vi.fn()}
      onRetry={vi.fn()}
      onCancel={vi.fn()}
      {...overrides}
    />,
  )
}

describe('UkExpensePromptDialog', () => {
  it('renders prefilled Description, Value, and today\'s Date', () => {
    renderDialog()

    expect(screen.getByLabelText(/^Description/)).toHaveValue('Council Tax')
    expect(screen.getByLabelText(/^Value/)).toHaveValue(120)
    expect(screen.getByLabelText(/^Date/)).toHaveValue(todayIsoDate())
  })

  it('Confirm is disabled until Bank and Category are selected', () => {
    renderDialog()

    expect(screen.getByRole('button', { name: 'Confirm' })).toBeDisabled()

    fireEvent.change(screen.getByLabelText(/^Bank/), { target: { value: 'bank-1' } })
    expect(screen.getByRole('button', { name: 'Confirm' })).toBeDisabled()

    fireEvent.change(screen.getByLabelText(/^Category/), { target: { value: 'cat-1' } })
    expect(screen.getByRole('button', { name: 'Confirm' })).not.toBeDisabled()
  })

  it('calls onConfirm with the form values', () => {
    const onConfirm = vi.fn()
    renderDialog({ onConfirm })

    fireEvent.change(screen.getByLabelText(/^Bank/), { target: { value: 'bank-1' } })
    fireEvent.change(screen.getByLabelText(/^Category/), { target: { value: 'cat-1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Confirm' }))

    expect(onConfirm).toHaveBeenCalledWith({
      description: 'Council Tax',
      value: 120,
      date: todayIsoDate(),
      bankId: 'bank-1',
      categoryId: 'cat-1',
    })
  })

  it('calls onSkip when Skip is clicked', () => {
    const onSkip = vi.fn()
    renderDialog({ onSkip })

    fireEvent.click(screen.getByRole('button', { name: 'Skip' }))

    expect(onSkip).toHaveBeenCalled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const onCancel = vi.fn()
    renderDialog({ onCancel })

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })

  it('shows the expense creation error inline', () => {
    renderDialog({ expenseCreateError: 'Category is required.' })

    expect(screen.getByText('Category is required.')).toBeInTheDocument()
  })

  it('retry-only mode hides the form and shows a single retry action', () => {
    renderDialog({ isRetryOnly: true, statusUpdateError: 'Recurring bill not found.' })

    expect(screen.queryByLabelText(/^Bank/)).not.toBeInTheDocument()
    expect(screen.queryByLabelText(/^Category/)).not.toBeInTheDocument()
    expect(screen.queryByLabelText(/^Description/)).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Confirm' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Skip' })).not.toBeInTheDocument()
    expect(screen.getByText(/Recurring bill not found\./)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Retry marking as Paid' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Close' })).toBeInTheDocument()
  })

  it('calls onRetry when the retry-only action is clicked', () => {
    const onRetry = vi.fn()
    renderDialog({ isRetryOnly: true, statusUpdateError: 'Recurring bill not found.', onRetry })

    fireEvent.click(screen.getByRole('button', { name: 'Retry marking as Paid' }))

    expect(onRetry).toHaveBeenCalled()
  })
})
