import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import IncomeForm from '../IncomeForm'
import type { BankDto } from '../../api/types'

const BANKS: BankDto[] = [
  { name: 'Barclays', roundUpEnabled: false },
  { name: 'Trading212', roundUpEnabled: true },
]

const baseProps = {
  isEditing: false,
  date: '',
  incomeSource: 'Gleison',
  grossValue: '',
  netValue: '',
  bank: 'Barclays',
  banks: BANKS,
  isSaving: false,
  saveError: null,
  onFieldChange: vi.fn(),
  onSave: vi.fn(),
  onCancel: vi.fn(),
}

describe('IncomeForm', () => {
  it('renders the create form with empty date/net-value fields', () => {
    render(<IncomeForm {...baseProps} />)

    expect(screen.getByText('New Income')).toBeInTheDocument()
    expect(screen.getByLabelText('Date')).toHaveValue('')
    expect(screen.getByLabelText('Net Value')).toHaveValue(null)
    expect(screen.getByRole('button', { name: 'Add Income' })).toBeInTheDocument()
  })

  it('shows the gross value field only for sources that require it', () => {
    const { rerender } = render(<IncomeForm {...baseProps} incomeSource="Gleison" />)
    expect(screen.getByLabelText('Gross Value')).toBeInTheDocument()

    rerender(<IncomeForm {...baseProps} incomeSource="Ariana" />)
    expect(screen.getByLabelText('Gross Value')).toBeInTheDocument()

    rerender(<IncomeForm {...baseProps} incomeSource="Lottery" />)
    expect(screen.queryByLabelText('Gross Value')).not.toBeInTheDocument()

    rerender(<IncomeForm {...baseProps} incomeSource="DividendoJuros" />)
    expect(screen.queryByLabelText('Gross Value')).not.toBeInTheDocument()
  })

  it('calls onSave and onCancel', () => {
    const onSave = vi.fn()
    const onCancel = vi.fn()
    render(<IncomeForm {...baseProps} onSave={onSave} onCancel={onCancel} />)

    fireEvent.click(screen.getByRole('button', { name: 'Add Income' }))
    expect(onSave).toHaveBeenCalled()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onCancel).toHaveBeenCalled()
  })

  it('shows Edit Income and Save when editing', () => {
    render(<IncomeForm {...baseProps} isEditing />)

    expect(screen.getByText('Edit Income')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
  })
})
