import { fireEvent, screen } from '@testing-library/react'
import { render } from '../../test/renderWithFluent'
import { describe, expect, it, vi } from 'vitest'
import BalanceAdjustmentForm from '../BalanceAdjustmentForm'
import type { BankDto } from '../../api/types'

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
]

const INACTIVE_BANK: BankDto = { id: 'bank-chase', name: 'Chase', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: false }

const optionNames = (select: HTMLElement) => Array.from(select.querySelectorAll('option')).map((option) => option.textContent)

const baseProps = {
  isEditing: false,
  bankName: 'bank-barclays',
  bankDisplayName: 'Barclays',
  banks: BANKS,
  currentBalance: 100,
  date: '2026-07-25',
  targetBalance: '',
  note: '',
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
  savedDelta: null,
  onFieldChange: vi.fn(),
  onSave: vi.fn(),
  onCancel: vi.fn(),
}

describe('BalanceAdjustmentForm', () => {
  it('renders the current calculated balance from the currentBalance prop', () => {
    render(<BalanceAdjustmentForm {...baseProps} bankName="bank-barclays" currentBalance={2344.37} />)

    expect(screen.getByTestId('balance-adjustment-form-panel')).toHaveTextContent(
      'Current calculated balance for Barclays: £2,344.37',
    )
  })

  it('renders the create form title', () => {
    render(<BalanceAdjustmentForm {...baseProps} />)

    expect(screen.getByRole('heading', { name: 'Adjust balance' })).toBeInTheDocument()
  })

  it('marks Bank and Target Balance as required, leaving Note optional', () => {
    render(<BalanceAdjustmentForm {...baseProps} />)

    expect(screen.getByLabelText(/^Bank/)).toBeRequired()
    expect(screen.getByRole('spinbutton', { name: /^Target Balance/ })).toBeRequired()
    expect(screen.getByLabelText('Note')).not.toBeRequired()
  })

  it('offers contextual help explaining Target Balance', () => {
    render(<BalanceAdjustmentForm {...baseProps} />)

    expect(
      screen.getByRole('button', {
        name: /Target Balance information/,
      }),
    ).toBeInTheDocument()
  })

  it('renders the edit form title and pre-filled values', () => {
    render(<BalanceAdjustmentForm {...baseProps} isEditing targetBalance="150" note="Matched statement" />)

    expect(screen.getByRole('heading', { name: 'Edit balance adjustment' })).toBeInTheDocument()
    expect(screen.getByRole('spinbutton', { name: /^Target Balance/ })).toHaveValue(150)
    expect(screen.getByLabelText('Note')).toHaveValue('Matched statement')
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
  })

  it('shows the saved-state confirmation with a positive delta and hides the editable fields', () => {
    render(<BalanceAdjustmentForm {...baseProps} savedDelta={4.2} />)

    expect(screen.getByTestId('balance-adjustment-form-panel')).toHaveTextContent('Adjustment of £4.20 recorded')
    expect(screen.queryByLabelText('Target Balance')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Close' })).toBeInTheDocument()
  })

  it('shows the saved-state confirmation with a negative delta', () => {
    render(<BalanceAdjustmentForm {...baseProps} savedDelta={-4.2} />)

    expect(screen.getByTestId('balance-adjustment-form-panel')).toHaveTextContent('Adjustment of -£4.20 recorded')
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

    fireEvent.click(screen.getByRole('button', { name: 'Adjust balance' }))
    expect(onSave).toHaveBeenCalled()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onCancel).toHaveBeenCalled()
  })

  it('renders a backend error under the field named in saveErrorFields', () => {
    render(
      <BalanceAdjustmentForm
        {...baseProps}
        saveError="Balance cannot be negative."
        saveErrorFields={{ targetBalance: 'Balance cannot be negative.' }}
      />,
    )

    expect(screen.getByText('Balance cannot be negative.')).toBeInTheDocument()
  })

  it('renders a general error banner when no field claims the error', () => {
    render(<BalanceAdjustmentForm {...baseProps} saveError="Network request failed" saveErrorFields={{}} />)

    expect(screen.getByText('Network request failed')).toBeInTheDocument()
  })

  it('shows Saving... and disables the button while isSaving', () => {
    render(<BalanceAdjustmentForm {...baseProps} isSaving />)

    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
  })

  it('only the Bank dropdown is enabled before a bank is chosen', () => {
    render(<BalanceAdjustmentForm {...baseProps} bankName="" currentBalance={0} />)

    expect(screen.getByLabelText(/^Bank/)).toBeInTheDocument()
    expect(screen.queryByText(/Current calculated balance/)).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Date')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Target Balance')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Note')).not.toBeInTheDocument()
  })

  it('reveals the reference line and fields once a bank is chosen', () => {
    render(<BalanceAdjustmentForm {...baseProps} bankName="bank-barclays" currentBalance={100} />)

    expect(screen.getByTestId('balance-adjustment-form-panel')).toHaveTextContent(
      'Current calculated balance for Barclays: £100.00',
    )
    expect(screen.getByLabelText(/^Date/)).toBeInTheDocument()
    expect(screen.getByRole('spinbutton', { name: /^Target Balance/ })).toBeInTheDocument()
    expect(screen.getByLabelText('Note')).toBeInTheDocument()
  })

  it('Save stays disabled until a bank is chosen', () => {
    render(<BalanceAdjustmentForm {...baseProps} bankName="" currentBalance={0} />)

    expect(screen.getByRole('button', { name: 'Adjust balance' })).toBeDisabled()
  })

  it('calls onFieldChange with the selected bank when the Bank dropdown changes', () => {
    const onFieldChange = vi.fn()
    render(<BalanceAdjustmentForm {...baseProps} bankName="" currentBalance={0} onFieldChange={onFieldChange} />)

    fireEvent.change(screen.getByLabelText(/^Bank/), { target: { value: 'bank-trading212' } })

    expect(onFieldChange).toHaveBeenCalledWith('bankName', 'bank-trading212')
  })

  it('renders the bank as static text (not a dropdown) when editing', () => {
    render(
      <BalanceAdjustmentForm {...baseProps} isEditing bankName="bank-barclays" currentBalance={100} targetBalance="150" />,
    )

    expect(screen.queryByLabelText('Bank')).not.toBeInTheDocument()
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument()
    expect(screen.getByTestId('balance-adjustment-form-panel')).toHaveTextContent(
      'Current calculated balance for Barclays: £100.00',
    )
  })

  it('shows a locked bank as text with no bank dropdown, and saves on Enter', () => {
    const onSave = vi.fn()
    render(
      <BalanceAdjustmentForm
        {...baseProps}
        bankName="bank-chase"
        bankDisplayName="Chase"
        isBankLocked
        onSave={onSave}
      />,
    )

    expect(screen.getByText('Chase', { selector: 'span' })).toBeInTheDocument()
    expect(screen.queryByRole('combobox', { name: /Bank/ })).not.toBeInTheDocument()

    fireEvent.submit(screen.getByTestId('balance-adjustment-form-panel'))

    expect(onSave).toHaveBeenCalledOnce()
  })

  it('shows balances in the given currency under the given label', () => {
    render(<BalanceAdjustmentForm {...baseProps} currentBalance={12.5} currency="EUR" balanceLabel="Balance at end of October 2026" />)

    expect(screen.getByTestId('balance-adjustment-form-panel')).toHaveTextContent('Balance at end of October 2026: €12.50')
  })

  it('[AC P60-F06-admin-accounts-page-05] leaves an inactive bank out of Bank on a new adjustment', () => {
    render(<BalanceAdjustmentForm {...baseProps} banks={[...BANKS, INACTIVE_BANK]} />)

    expect(optionNames(screen.getByLabelText(/^Bank/))).toEqual(['Select a bank', 'Barclays', 'Trading212'])
  })

  it('[AC P60-F06-admin-accounts-page-05] keeps an inactive bank that is already selected', () => {
    render(<BalanceAdjustmentForm {...baseProps} bankName="bank-chase" banks={[...BANKS, INACTIVE_BANK]} />)

    expect(screen.getByLabelText(/^Bank/)).toHaveValue('bank-chase')
  })
})
