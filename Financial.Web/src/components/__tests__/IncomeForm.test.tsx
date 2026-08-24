import { fireEvent, screen } from '@testing-library/react'
import { render } from '../../test/renderWithFluent'
import { describe, expect, it, vi } from 'vitest'
import IncomeForm from '../IncomeForm'
import type { BankDto, IncomeSourceDto } from '../../api/types'

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01' },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01' },
]

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: '1', name: 'Gleison', isActive: true, group: 'Salary', autoSplitToReserve: false },
  { id: '2', name: 'Ariana', isActive: true, group: 'Salary', autoSplitToReserve: true },
  { id: '3', name: 'Lottery', isActive: true, group: 'NonReportable', autoSplitToReserve: false },
  { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros', autoSplitToReserve: false },
]

const baseProps = {
  isEditing: false,
  date: '',
  incomeSource: '1',
  grossValue: '',
  netValue: '',
  bank: 'bank-barclays',
  description: '',
  splitToReserve: false,
  banks: BANKS,
  incomeSources: INCOME_SOURCES,
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
    const { rerender } = render(<IncomeForm {...baseProps} incomeSource="1" />)
    expect(screen.getByLabelText('Gross Value')).toBeInTheDocument()

    rerender(<IncomeForm {...baseProps} incomeSource="2" />)
    expect(screen.getByLabelText('Gross Value')).toBeInTheDocument()

    rerender(<IncomeForm {...baseProps} incomeSource="3" />)
    expect(screen.queryByLabelText('Gross Value')).not.toBeInTheDocument()

    rerender(<IncomeForm {...baseProps} incomeSource="4" />)
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

  it('renders the source dropdown options from the fetched, active income sources', () => {
    render(<IncomeForm {...baseProps} />)

    const select = screen.getByLabelText('Source') as HTMLSelectElement
    const optionValues = Array.from(select.options).map((o) => o.value)
    expect(optionValues).toEqual(['1', '2', '3', '4'])
    const optionLabels = Array.from(select.options).map((o) => o.text)
    expect(optionLabels).toEqual(['Gleison', 'Ariana', 'Lottery', 'DividendoJuros'])
  })

  it('excludes an inactive income source from the dropdown', () => {
    const sources: IncomeSourceDto[] = [
      ...INCOME_SOURCES,
      { id: '5', name: 'RetiredSource', isActive: false, group: 'NonReportable', autoSplitToReserve: false },
    ]
    render(<IncomeForm {...baseProps} incomeSources={sources} />)

    const select = screen.getByLabelText('Source') as HTMLSelectElement
    const optionLabels = Array.from(select.options).map((o) => o.text)
    expect(optionLabels).not.toContain('RetiredSource')
    expect(optionLabels).toHaveLength(4)
  })

  it('renders no options when the income sources list is empty', () => {
    render(<IncomeForm {...baseProps} incomeSources={[]} />)

    const select = screen.getByLabelText('Source') as HTMLSelectElement
    expect(select.options).toHaveLength(0)
  })

  it('offers a "no bank" option alongside the fetched banks', () => {
    render(<IncomeForm {...baseProps} />)

    const select = screen.getByLabelText('Bank') as HTMLSelectElement
    const optionLabels = Array.from(select.options).map((o) => o.text)
    expect(optionLabels).toEqual(['— No bank —', 'Barclays', 'Trading212'])
  })

  it('renders the description field and reports changes', () => {
    const onFieldChange = vi.fn()
    render(<IncomeForm {...baseProps} description="Chip ISA dividend" onFieldChange={onFieldChange} />)

    expect(screen.getByLabelText('Description')).toHaveValue('Chip ISA dividend')

    fireEvent.change(screen.getByLabelText('Description'), { target: { value: 'Updated note' } })
    expect(onFieldChange).toHaveBeenCalledWith('description', 'Updated note')
  })
})
