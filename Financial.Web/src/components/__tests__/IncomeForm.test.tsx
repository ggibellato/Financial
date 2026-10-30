import { fireEvent, screen } from '@testing-library/react'
import { render } from '../../test/renderWithFluent'
import { describe, expect, it, vi } from 'vitest'
import IncomeForm from '../IncomeForm'
import type { BankDto, IncomeSourceDto } from '../../api/types'
import { optionNames } from '../../test-utils/optionNames'
import { potsAccount } from '../../test/account'

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
]

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: '1', name: 'Gleison', isActive: true, group: 'Salary', potsAccountId: null, hasReferences: false },
  { id: '2', name: 'Ariana', isActive: true, group: 'Salary', potsAccountId: 'p1', hasReferences: false },
  { id: '3', name: 'Lottery', isActive: true, group: 'NonReportable', potsAccountId: null, hasReferences: false },
  { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros', potsAccountId: null, hasReferences: false },
]

const RESERVA = potsAccount({ id: 'p1', name: 'Reserva' })
const HOLIDAYS = potsAccount({ id: 'p2', name: 'Holidays' })

const INACTIVE_BANK: BankDto = { ...BANKS[0], id: 'bank-chase', name: 'Chase', hasReferences: true, isActive: false }

const baseProps = {
  isEditing: false,
  date: '',
  incomeSource: '1',
  grossValue: '',
  netValue: '',
  bank: 'bank-barclays',
  description: '',
  splitToReserve: false,
  potsAccountId: '',
  potsAccounts: [HOLIDAYS, RESERVA],
  potsAccountsStatus: 'ready' as const,
  onRetryPotsAccounts: vi.fn(),
  banks: BANKS,
  incomeSources: INCOME_SOURCES,
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
  onFieldChange: vi.fn(),
  onSave: vi.fn(),
  onCancel: vi.fn(),
}

describe('IncomeForm', () => {
  it('renders the create form with empty date/net-value fields', () => {
    render(<IncomeForm {...baseProps} />)

    expect(screen.getByText('New Income')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('')
    expect(screen.getByLabelText(/^Net Value/)).toHaveValue(null)
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

    const select = screen.getByLabelText(/^Source/) as HTMLSelectElement
    const optionValues = Array.from(select.options).map((o) => o.value)
    expect(optionValues).toEqual(['1', '2', '3', '4'])
    const optionLabels = Array.from(select.options).map((o) => o.text)
    expect(optionLabels).toEqual(['Gleison', 'Ariana', 'Lottery', 'DividendoJuros'])
  })

  it('excludes an inactive income source from the dropdown', () => {
    const sources: IncomeSourceDto[] = [
      ...INCOME_SOURCES,
      { id: '5', name: 'RetiredSource', isActive: false, group: 'NonReportable', potsAccountId: null, hasReferences: false },
    ]
    render(<IncomeForm {...baseProps} incomeSources={sources} />)

    const select = screen.getByLabelText(/^Source/) as HTMLSelectElement
    const optionLabels = Array.from(select.options).map((o) => o.text)
    expect(optionLabels).not.toContain('RetiredSource')
    expect(optionLabels).toHaveLength(4)
  })

  it('renders no options when the income sources list is empty', () => {
    render(<IncomeForm {...baseProps} incomeSources={[]} />)

    const select = screen.getByLabelText(/^Source/) as HTMLSelectElement
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

  it('shows the split-to-reserve checkbox only for a source that allows it', () => {
    const { rerender } = render(<IncomeForm {...baseProps} incomeSource="1" />)
    expect(screen.queryByLabelText('Split to pots')).not.toBeInTheDocument()

    rerender(<IncomeForm {...baseProps} incomeSource="2" />)
    expect(screen.getByLabelText('Split to pots')).toBeInTheDocument()

    rerender(<IncomeForm {...baseProps} incomeSource="3" />)
    expect(screen.queryByLabelText('Split to pots')).not.toBeInTheDocument()
  })

  it('reflects the splitToReserve prop as the checkbox state', () => {
    const { rerender } = render(<IncomeForm {...baseProps} incomeSource="2" splitToReserve={true} />)
    expect(screen.getByLabelText('Split to pots')).toBeChecked()

    rerender(<IncomeForm {...baseProps} incomeSource="2" splitToReserve={false} />)
    expect(screen.getByLabelText('Split to pots')).not.toBeChecked()
  })

  it('reports toggling the split checkbox via onFieldChange', () => {
    const onFieldChange = vi.fn()
    render(<IncomeForm {...baseProps} incomeSource="2" splitToReserve={true} onFieldChange={onFieldChange} />)

    fireEvent.click(screen.getByLabelText('Split to pots'))

    expect(onFieldChange).toHaveBeenCalledWith('splitToReserve', 'false')
  })

  it('shows the Pots account picker only while the split is ticked', () => {
    const { rerender } = render(<IncomeForm {...baseProps} incomeSource="2" splitToReserve={true} potsAccountId="p1" />)
    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toHaveValue('p1')
    expect(optionNames(screen.getByRole('combobox', { name: /^Pots account/ }))).toEqual(['Select Pots account…', 'Holidays', 'Reserva'])
    expect(screen.getByText("The split goes into this account's active pots.")).toBeInTheDocument()

    rerender(<IncomeForm {...baseProps} incomeSource="2" splitToReserve={false} potsAccountId="p1" />)
    expect(screen.queryByRole('combobox', { name: /^Pots account/ })).not.toBeInTheDocument()
  })

  it('reports choosing another Pots account via onFieldChange', () => {
    const onFieldChange = vi.fn()
    render(<IncomeForm {...baseProps} incomeSource="2" splitToReserve={true} potsAccountId="p1" onFieldChange={onFieldChange} />)

    fireEvent.change(screen.getByRole('combobox', { name: /^Pots account/ }), { target: { value: 'p2' } })

    expect(onFieldChange).toHaveBeenCalledWith('potsAccountId', 'p2')
  })

  it('labels a stored Pots account that is now inactive', () => {
    const oldPots = potsAccount({ id: 'p3', name: 'Old pots', isActive: false })
    render(<IncomeForm {...baseProps} incomeSource="2" splitToReserve={true} potsAccountId="p3" potsAccounts={[HOLIDAYS, oldPots, RESERVA]} />)

    expect(optionNames(screen.getByRole('combobox', { name: /^Pots account/ }))).toEqual([
      'Select Pots account…',
      'Holidays',
      'Old pots (inactive)',
      'Reserva',
    ])
  })

  it('shows the "Choose a Pots account." validation message', () => {
    render(
      <IncomeForm
        {...baseProps}
        incomeSource="2"
        splitToReserve={true}
        potsAccountId=""
        saveError="Choose a Pots account."
        saveErrorFields={{ potsAccountId: 'Choose a Pots account.' }}
      />,
    )

    expect(screen.getByText('Choose a Pots account.')).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toHaveAttribute('aria-invalid', 'true')
  })

  it('says how to add a Pots account when there is no active one to choose', () => {
    render(<IncomeForm {...baseProps} incomeSource="2" splitToReserve={true} potsAccountId="" potsAccounts={[]} />)

    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toBeDisabled()
    expect(
      screen.getByText('No active Pots account. Add one in Admin → CashFlow → Accounts, or untick the split.'),
    ).toBeInTheDocument()
  })

  it('shows the Pots accounts as loading and keeps the chosen account', () => {
    render(<IncomeForm {...baseProps} incomeSource="2" splitToReserve={true} potsAccountId="p1" potsAccounts={[]} potsAccountsStatus="loading" />)

    const picker = screen.getByRole('combobox', { name: /^Pots account/ })
    expect(picker).toBeDisabled()
    expect(picker).toHaveValue('p1')
    expect(screen.getByText('Loading Pots accounts…')).toBeInTheDocument()
  })

  it('offers Retry when the Pots accounts failed to load', () => {
    const onRetryPotsAccounts = vi.fn()
    render(
      <IncomeForm
        {...baseProps}
        incomeSource="2"
        splitToReserve={true}
        potsAccountId="p1"
        potsAccounts={[]}
        potsAccountsStatus="failed"
        onRetryPotsAccounts={onRetryPotsAccounts}
      />,
    )

    expect(screen.getByText(/Pots accounts couldn.t be loaded/)).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toBeDisabled()
    fireEvent.click(screen.getByRole('button', { name: 'Retry loading Pots accounts' }))
    expect(onRetryPotsAccounts).toHaveBeenCalled()
  })

  it('disables the Pots account picker while saving', () => {
    render(<IncomeForm {...baseProps} incomeSource="2" splitToReserve={true} potsAccountId="p1" isSaving={true} />)

    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toBeDisabled()
  })
})

describe('IncomeForm with a locked bank', () => {
  it('shows the locked bank as read-only text instead of the bank picker', () => {
    render(<IncomeForm {...baseProps} bank="bank-chase" lockedBank={{ id: 'bank-chase', name: 'Chase' }} />)

    expect(screen.getByText('Chase')).toBeInTheDocument()
    expect(screen.queryByRole('combobox', { name: 'Bank' })).not.toBeInTheDocument()
    expect(screen.queryByRole('textbox', { name: 'Bank' })).not.toBeInTheDocument()
  })

  it('[AC P60-F06-admin-accounts-page-05] leaves an inactive bank out of Bank on a new income', () => {
    render(<IncomeForm {...baseProps} banks={[...BANKS, INACTIVE_BANK]} />)

    expect(optionNames(screen.getByLabelText('Bank'))).toEqual(['— No bank —', 'Barclays', 'Trading212'])
  })

  it('[AC P60-F06-admin-accounts-page-05] keeps the inactive bank of an income being edited', () => {
    render(<IncomeForm {...baseProps} isEditing bank="bank-chase" banks={[...BANKS, INACTIVE_BANK]} />)

    expect(screen.getByLabelText('Bank')).toHaveValue('bank-chase')
    expect(optionNames(screen.getByLabelText('Bank'))).toContain('Chase')
  })

  it("[AC P60-F06-admin-accounts-page-05] keeps the edited income's inactive bank after another bank is picked", () => {
    const props = { ...baseProps, isEditing: true, banks: [...BANKS, INACTIVE_BANK] }
    const { rerender } = render(<IncomeForm {...props} bank="bank-chase" />)

    rerender(<IncomeForm {...props} bank="bank-barclays" />)

    expect(optionNames(screen.getByLabelText('Bank'))).toContain('Chase')
  })
})
