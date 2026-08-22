import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import TransferForm from '../TransferForm'
import type { BankDto } from '../../api/types'

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01' },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01' },
  { id: 'bank-chase', name: 'Chase', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01' },
]

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
  saveErrorField: null,
  onFieldChange: vi.fn(),
  onSave: vi.fn(),
  onCancel: vi.fn(),
}

describe('TransferForm', () => {
  it('renders the create form title and pre-filled values', () => {
    const { container } = render(<TransferForm {...baseProps} />)

    expect(container.querySelector('.monthly-page__form-title')).toHaveTextContent('Move Money')
    expect(screen.getByLabelText('Date')).toHaveValue('2026-07-25')
    expect(screen.getByLabelText('From')).toHaveValue('bank-barclays')
    expect(screen.getByLabelText('To')).toHaveValue('bank-trading212')
  })

  it('renders the edit form title', () => {
    render(<TransferForm {...baseProps} isEditing />)

    expect(screen.getByText('Edit Transfer')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
  })

  it('excludes the selected source bank from the destination dropdown', () => {
    render(<TransferForm {...baseProps} sourceBank="bank-barclays" destinationBank="bank-trading212" />)

    const destinationOptions = screen.getByLabelText('To').querySelectorAll('option')
    const optionValues = Array.from(destinationOptions).map((o) => o.textContent)
    expect(optionValues).not.toContain('Barclays')
    expect(optionValues).toContain('Trading212')
    expect(optionValues).toContain('Chase')
  })

  it('shows an inline error and disables submit when source and destination match', () => {
    render(<TransferForm {...baseProps} sourceBank="bank-barclays" destinationBank="bank-barclays" amount="100" />)

    expect(screen.getByText('Source and destination must be different banks.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Move Money' })).toBeDisabled()
  })

  it('does not show the same-bank error when source and destination differ', () => {
    render(<TransferForm {...baseProps} sourceBank="bank-barclays" destinationBank="bank-trading212" />)

    expect(screen.queryByText('Source and destination must be different banks.')).not.toBeInTheDocument()
  })

  it('calls onSave and onCancel', () => {
    const onSave = vi.fn()
    const onCancel = vi.fn()
    render(<TransferForm {...baseProps} onSave={onSave} onCancel={onCancel} />)

    fireEvent.click(screen.getByRole('button', { name: 'Move Money' }))
    expect(onSave).toHaveBeenCalled()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onCancel).toHaveBeenCalled()
  })

  it('renders a backend error under the field named by saveErrorField', () => {
    render(<TransferForm {...baseProps} saveError="Transfer amount must be greater than zero." saveErrorField="amount" />)

    const amountField = screen.getByLabelText('Amount').closest('.monthly-page__form-field')
    expect(amountField).toHaveTextContent('Transfer amount must be greater than zero.')
  })

  it('renders a general error banner when saveErrorField is null', () => {
    render(<TransferForm {...baseProps} saveError="Network request failed" saveErrorField={null} />)

    expect(screen.getByText('Network request failed')).toBeInTheDocument()
  })

  it('shows Saving... and disables the button while isSaving', () => {
    render(<TransferForm {...baseProps} isSaving />)

    const button = screen.getByRole('button', { name: 'Saving...' })
    expect(button).toBeDisabled()
  })
})
