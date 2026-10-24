import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import UkExpensePromptDialog from '../UkExpensePromptDialog'
import type { BankDto, CategoryDto, RecurringBillDto } from '../../api/types'
import { pinDate } from '../../test-utils/pinDate'

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
  { id: 'bank-1', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
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
      isSaving={false}
      error={null}
      onConfirm={vi.fn()}
      onSkip={vi.fn()}
      onCancel={vi.fn()}
      {...overrides}
    />,
  )
}

describe('UkExpensePromptDialog', () => {
  beforeEach(() => {
    pinDate('2026-07-01T00:30:00+01:00')
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('renders prefilled Description, Value, and today\'s Date', () => {
    renderDialog()

    expect(screen.getByLabelText(/^Description/)).toHaveValue('Council Tax')
    expect(screen.getByLabelText(/^Value/)).toHaveValue(120)
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-07-01')
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
      date: '2026-07-01',
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

  it('shows the error inline and keeps the form editable for another attempt', () => {
    renderDialog({ error: 'Category is required.' })

    expect(screen.getByText('Category is required.')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Bank/)).toBeEnabled()
  })

  it('disables every action while saving', () => {
    renderDialog({ isSaving: true })

    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Skip' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })
})
