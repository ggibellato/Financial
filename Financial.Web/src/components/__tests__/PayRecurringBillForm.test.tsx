import { describe, expect, it, vi } from 'vitest'
import type { RecurringBillDto } from '../../api/types'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import PayRecurringBillForm from '../PayRecurringBillForm'

const COUNCIL_TAX: RecurringBillDto = {
  id: 'council',
  dueDay: 5,
  description: 'Council Tax',
  value: 120,
  area: 'UK',
  note: '',
  status: 'Unset',
  minimumWageValue: null,
  nitNumber: null,
}

const CATEGORIES = [{ id: 'cat-home', name: 'Home', active: true, isInvestment: false, isTithe: false, hasReferences: true }]

const baseProps = {
  bills: [COUNCIL_TAX],
  isLoadingBills: false,
  billsError: null,
  billId: '',
  values: null,
  categories: CATEGORIES,
  lockedBank: { id: 'barclays', name: 'Barclays' },
  canSubmit: false,
  isSaving: false,
  saveError: null,
  onRetryBills: vi.fn(),
  onSelectBill: vi.fn(),
  onFieldChange: vi.fn(),
  onSave: vi.fn(),
  onCancel: vi.fn(),
}

const SELECTED = {
  billId: 'council',
  values: { date: '2026-10-15', description: 'Council Tax', value: '120', bankId: 'barclays', categoryId: '' },
}

describe('PayRecurringBillForm', () => {
  it('lists each bill by due day, description and value, and reports the choice', () => {
    const onSelectBill = vi.fn()
    render(<PayRecurringBillForm {...baseProps} onSelectBill={onSelectBill} />)

    expect(screen.getByRole('heading', { name: 'Pay recurring bill' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Due 5 · Council Tax · £120.00' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Bill/), { target: { value: 'council' } })

    expect(onSelectBill).toHaveBeenCalledWith('council')
    expect(screen.queryByLabelText(/^Category/)).not.toBeInTheDocument()
  })

  it('shows the expense fields with this bank as text once a bill is chosen', () => {
    render(<PayRecurringBillForm {...baseProps} {...SELECTED} />)

    expect(screen.getByLabelText(/^Description/)).toHaveValue('Council Tax')
    expect(screen.getByLabelText(/^Value/)).toHaveValue(120)
    expect(screen.getByText('Barclays')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Pay recurring bill' })).toBeDisabled()
  })

  it('saves on Enter once the values are valid', () => {
    const onSave = vi.fn()
    render(<PayRecurringBillForm {...baseProps} {...SELECTED} canSubmit onSave={onSave} />)

    expect(screen.getByRole('button', { name: 'Pay recurring bill' })).toBeEnabled()
    fireEvent.submit(screen.getByRole('button', { name: 'Pay recurring bill' }).closest('form') as HTMLFormElement)

    expect(onSave).toHaveBeenCalledOnce()
  })

  it('shows a loading indicator while the bills load', () => {
    render(<PayRecurringBillForm {...baseProps} bills={[]} isLoadingBills />)

    expect(screen.getByText('Loading bills…')).toBeInTheDocument()
  })

  it('says when there are no unpaid UK bills', () => {
    render(<PayRecurringBillForm {...baseProps} bills={[]} />)

    expect(screen.getByText('No unpaid UK bills.')).toBeInTheDocument()
  })

  it('offers Try again when the bills fail to load', () => {
    const onRetryBills = vi.fn()
    render(<PayRecurringBillForm {...baseProps} bills={[]} billsError="Unable to load the recurring bills" onRetryBills={onRetryBills} />)

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(screen.getByText('Unable to load the recurring bills')).toBeInTheDocument()
    expect(onRetryBills).toHaveBeenCalledOnce()
  })

  it('shows a save error above the fields and disables everything while saving', () => {
    const { rerender } = render(<PayRecurringBillForm {...baseProps} {...SELECTED} saveError="This bill is already paid." />)
    expect(screen.getByText('This bill is already paid.')).toBeInTheDocument()

    rerender(<PayRecurringBillForm {...baseProps} {...SELECTED} isSaving />)

    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
    expect(screen.getByLabelText(/^Description/)).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })
})
