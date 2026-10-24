import { describe, expect, it, vi } from 'vitest'
import { recurringBill } from '../../test/recurringBill'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import PayRecurringBillForm from '../PayRecurringBillForm'

const COUNCIL_TAX = recurringBill({ id: 'council', dueDay: 5, description: 'Council Tax', value: 120 })

const CATEGORIES = [{ id: 'cat-home', name: 'Home', active: true, isInvestment: false, isTithe: false, hasReferences: true }]

const baseProps = {
  bills: [COUNCIL_TAX],
  isLoadingBills: false,
  billsError: null,
  billId: '',
  values: null,
  categories: CATEGORIES,
  lockedBank: { id: 'barclays', name: 'Barclays', currency: 'GBP' },
  isSaving: false,
  saveError: null,
  fieldErrors: {},
  billError: null,
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
    expect(screen.queryByRole('option', { name: 'Select a bill' })).not.toBeInTheDocument()
  })

  it('shows field and bill validation messages next to their fields', () => {
    const { rerender } = render(<PayRecurringBillForm {...baseProps} billError="Choose a bill." />)
    expect(screen.getByText('Choose a bill.')).toBeInTheDocument()

    rerender(<PayRecurringBillForm {...baseProps} {...SELECTED} fieldErrors={{ categoryId: 'Choose a category.' }} />)

    expect(screen.getByText('Choose a category.')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Category/)).toHaveAttribute('aria-invalid', 'true')
  })

  it('warns when the bank is not in the bills\' currency', () => {
    render(<PayRecurringBillForm {...baseProps} lockedBank={{ id: 'wise', name: 'Wise', currency: 'EUR' }} />)

    expect(screen.getByText("Recurring bills are in GBP; the expense is recorded in Wise's EUR.")).toBeInTheDocument()
  })

  it('saves on Enter', () => {
    const onSave = vi.fn()
    render(<PayRecurringBillForm {...baseProps} {...SELECTED} onSave={onSave} />)

    expect(screen.getByRole('button', { name: 'Pay recurring bill' })).toBeEnabled()
    fireEvent.submit(screen.getByRole('button', { name: 'Pay recurring bill' }).closest('form') as HTMLFormElement)

    expect(onSave).toHaveBeenCalledOnce()
  })

  it('shows a loading indicator while the bills load', () => {
    render(<PayRecurringBillForm {...baseProps} bills={[]} isLoadingBills />)

    expect(screen.getByText('Loading bills…')).toBeInTheDocument()
  })

  it('says when there are no unpaid UK bills and offers only Cancel', () => {
    render(<PayRecurringBillForm {...baseProps} bills={[]} />)

    expect(screen.getByText('No unpaid UK bills. Bills are managed on Mensais.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Pay recurring bill' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeEnabled()
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
