import { fireEvent, screen } from '@testing-library/react'
import { render } from '../../test/renderWithFluent'
import { describe, expect, it, vi } from 'vitest'
import TransferForm from '../TransferForm'
import type { BankDto } from '../../api/types'
import { optionNames } from '../../test-utils/optionNames'

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
  { id: 'bank-chase', name: 'Chase', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
]

const INACTIVE_BANK = { id: 'bank-monzo', name: 'Monzo', isActive: false }

const baseProps = {
  isEditing: false,
  date: '2026-07-25',
  sourceBank: 'bank-barclays',
  destinationBank: 'bank-trading212',
  amount: '',
  note: '',
  banks: BANKS,
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
  onFieldChange: vi.fn(),
  onSave: vi.fn(),
  onCancel: vi.fn(),
}

describe('TransferForm', () => {
  it('renders the create form title and pre-filled values', () => {
    render(<TransferForm {...baseProps} />)

    expect(screen.getByRole('heading', { name: 'New Transfer' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-07-25')
    expect(screen.getByLabelText(/^From/)).toHaveValue('bank-barclays')
    expect(screen.getByLabelText(/^To/)).toHaveValue('bank-trading212')
  })

  it('marks Date, From, To, and Amount as required, leaving Note optional', () => {
    render(<TransferForm {...baseProps} />)

    expect(screen.getByLabelText(/^Date/)).toBeRequired()
    expect(screen.getByLabelText(/^From/)).toBeRequired()
    expect(screen.getByLabelText(/^To/)).toBeRequired()
    expect(screen.getByLabelText(/^Amount/)).toBeRequired()
    expect(screen.getByLabelText('Note')).not.toBeRequired()
  })

  it('renders the edit form title', () => {
    render(<TransferForm {...baseProps} isEditing />)

    expect(screen.getByText('Edit Transfer')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
  })

  it('excludes the selected source bank from the destination dropdown', () => {
    render(<TransferForm {...baseProps} sourceBank="bank-barclays" destinationBank="bank-trading212" />)

    const destinationOptions = screen.getByLabelText(/^To/).querySelectorAll('option')
    const optionValues = Array.from(destinationOptions).map((o) => o.textContent)
    expect(optionValues).not.toContain('Barclays')
    expect(optionValues).toContain('Trading212')
    expect(optionValues).toContain('Chase')
  })

  it('shows an inline error and disables submit when source and destination match', () => {
    render(<TransferForm {...baseProps} sourceBank="bank-barclays" destinationBank="bank-barclays" amount="100" />)

    expect(screen.getByText('Source and destination must be different banks.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add Transfer' })).toBeDisabled()
  })

  it('does not show the same-bank error when source and destination differ', () => {
    render(<TransferForm {...baseProps} sourceBank="bank-barclays" destinationBank="bank-trading212" />)

    expect(screen.queryByText('Source and destination must be different banks.')).not.toBeInTheDocument()
  })

  it('calls onSave and onCancel', () => {
    const onSave = vi.fn()
    const onCancel = vi.fn()
    render(<TransferForm {...baseProps} onSave={onSave} onCancel={onCancel} />)

    fireEvent.click(screen.getByRole('button', { name: 'Add Transfer' }))
    expect(onSave).toHaveBeenCalled()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onCancel).toHaveBeenCalled()
  })

  it('renders a backend error under the field named in saveErrorFields', () => {
    render(
      <TransferForm
        {...baseProps}
        saveError="Transfer amount must be greater than zero."
        saveErrorFields={{ amount: 'Transfer amount must be greater than zero.' }}
      />,
    )

    expect(screen.getByText('Transfer amount must be greater than zero.')).toBeInTheDocument()
  })

  it('renders a general error banner when no field claims the error', () => {
    render(<TransferForm {...baseProps} saveError="Network request failed" saveErrorFields={{}} />)

    expect(screen.getByText('Network request failed')).toBeInTheDocument()
  })

  it('shows Saving... and disables the button while isSaving', () => {
    render(<TransferForm {...baseProps} isSaving />)

    const button = screen.getByRole('button', { name: 'Saving...' })
    expect(button).toBeDisabled()
  })

  it('disables every field and Cancel while saving', () => {
    render(<TransferForm {...baseProps} isSaving />)

    expect(screen.getByLabelText(/^Date/)).toBeDisabled()
    expect(screen.getByLabelText(/^Amount/)).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })

  it('names the currency of both amounts when the currencies differ', () => {
    render(<TransferForm {...baseProps} sourceCurrency="GBP" destinationCurrency="EUR" />)

    expect(screen.getByLabelText(/^Amount sent \(GBP\)/)).toBeInTheDocument()
    expect(screen.getByLabelText(/^Amount received \(EUR\)/)).toBeInTheDocument()
  })

  it('shows the locked account as text on its side and lists the others on the open side', () => {
    render(
      <TransferForm
        {...baseProps}
        sourceBank=""
        destinationBank="bank-barclays"
        banks={[{ id: 'bank-chase', name: 'Chase' }]}
        lockedSide={{ side: 'destination', name: 'Barclays' }}
      />,
    )

    expect(screen.getByText('Barclays', { selector: 'span' })).toBeInTheDocument()
    expect(screen.queryByRole('combobox', { name: /^To/ })).not.toBeInTheDocument()
    const from = screen.getByRole('combobox', { name: /^From/ })
    expect(from).toHaveValue('')
    expect(screen.getByRole('option', { name: 'Select an account' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Chase' })).toBeInTheDocument()
  })

  it('shows the amount received only when a destination currency is given', () => {
    const onFieldChange = vi.fn()
    const { rerender } = render(<TransferForm {...baseProps} onFieldChange={onFieldChange} />)
    expect(screen.queryByLabelText(/^Amount received/)).not.toBeInTheDocument()

    rerender(<TransferForm {...baseProps} onFieldChange={onFieldChange} destinationCurrency="EUR" destinationAmount="" />)
    fireEvent.change(screen.getByLabelText(/^Amount received \(EUR\)/), { target: { value: '115.40' } })

    expect(onFieldChange).toHaveBeenCalledWith('destinationAmount', '115.40')
  })

  it('saves on Enter', () => {
    const onSave = vi.fn()
    render(<TransferForm {...baseProps} onSave={onSave} />)

    fireEvent.submit(screen.getByTestId('transfer-form-panel'))

    expect(onSave).toHaveBeenCalledOnce()
  })

  it('[AC P60-F06-admin-accounts-page-05] leaves an inactive bank out of From and To on a new transfer', () => {
    render(<TransferForm {...baseProps} banks={[...baseProps.banks, INACTIVE_BANK]} />)

    expect(optionNames(screen.getByLabelText(/^From/))).not.toContain('Monzo')
    expect(optionNames(screen.getByLabelText(/^To/))).not.toContain('Monzo')
  })

  it('[AC P60-F06-admin-accounts-page-05] keeps the inactive bank of a transfer being edited on its own side only', () => {
    render(<TransferForm {...baseProps} isEditing sourceBank="bank-monzo" banks={[...baseProps.banks, INACTIVE_BANK]} />)

    expect(screen.getByLabelText(/^From/)).toHaveValue('bank-monzo')
    expect(optionNames(screen.getByLabelText(/^To/))).not.toContain('Monzo')
  })

  it('[AC P60-F06-admin-accounts-page-05] keeps the inactive destination of a transfer being edited', () => {
    render(<TransferForm {...baseProps} isEditing destinationBank="bank-monzo" banks={[...baseProps.banks, INACTIVE_BANK]} />)

    expect(screen.getByLabelText(/^To/)).toHaveValue('bank-monzo')
    expect(optionNames(screen.getByLabelText(/^From/))).not.toContain('Monzo')
  })

  it("[AC P60-F06-admin-accounts-page-05] keeps the edited transfer's inactive bank after another bank is picked", () => {
    const props = { ...baseProps, isEditing: true, banks: [...baseProps.banks, INACTIVE_BANK] }
    const { rerender } = render(<TransferForm {...props} sourceBank="bank-monzo" />)

    rerender(<TransferForm {...props} sourceBank="bank-chase" />)

    expect(optionNames(screen.getByLabelText(/^From/))).toContain('Monzo')
  })
})
