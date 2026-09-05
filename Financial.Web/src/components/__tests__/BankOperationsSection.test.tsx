import { fireEvent, screen } from '@testing-library/react'
import { render } from '../../test/renderWithFluent'
import { describe, expect, it, vi } from 'vitest'
import BankOperationsSection from '../BankOperationsSection'
import type { BankOperationEntry } from '../../hooks/useBankOperations'

const TRANSFER_ENTRY = {
  kind: 'transfer',
  id: 't1',
  date: '2026-07-05',
  sourceBank: 'Barclays',
  destinationBank: 'Trading212',
  amount: 500,
  note: 'Top-up',
  transfer: {
    id: 't1',
    date: '2026-07-05',
    sourceBankId: 'bank-barclays',
    sourceBankName: 'Barclays',
    destinationBankId: 'bank-trading212',
    destinationBankName: 'Trading212',
    amount: 500,
    note: 'Top-up',
  },
} satisfies BankOperationEntry

const ADJUSTMENT_ENTRY = {
  kind: 'adjustment',
  id: 'a1',
  date: '2026-07-10',
  bank: 'Barclays',
  bankId: 'bank-barclays',
  delta: -4.2,
  note: 'Matched statement',
  adjustment: {
    id: 'a1',
    date: '2026-07-10',
    bankId: 'bank-barclays',
    bankName: 'Barclays',
    targetBalance: 38.3,
    delta: -4.2,
    note: 'Matched statement',
  },
} satisfies BankOperationEntry

const baseProps = {
  operations: [] as BankOperationEntry[],
  onNewTransfer: vi.fn(),
  onNewBalanceCorrection: vi.fn(),
  onEditTransfer: vi.fn(),
  onEditAdjustment: vi.fn(),
  onDeleteTransfer: vi.fn(),
  onDeleteAdjustment: vi.fn(),
}

describe('BankOperationsSection', () => {
  it('renders both entry-point buttons', () => {
    render(<BankOperationsSection {...baseProps} />)

    expect(screen.getByRole('button', { name: 'New Transfer' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New Balance Correction' })).toBeInTheDocument()
  })

  it('no longer renders the old select-based Bank filter', () => {
    render(<BankOperationsSection {...baseProps} operations={[TRANSFER_ENTRY]} />)

    expect(screen.queryByRole('combobox')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Filter by Bank' })).toBeInTheDocument()
  })

  it('calls onNewTransfer and onNewBalanceCorrection', () => {
    const onNewTransfer = vi.fn()
    const onNewBalanceCorrection = vi.fn()
    render(
      <BankOperationsSection {...baseProps} onNewTransfer={onNewTransfer} onNewBalanceCorrection={onNewBalanceCorrection} />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'New Transfer' }))
    fireEvent.click(screen.getByRole('button', { name: 'New Balance Correction' }))

    expect(onNewTransfer).toHaveBeenCalledOnce()
    expect(onNewBalanceCorrection).toHaveBeenCalledOnce()
  })

  it('renders a row per operation with the correct columns', () => {
    render(<BankOperationsSection {...baseProps} operations={[TRANSFER_ENTRY, ADJUSTMENT_ENTRY]} />)

    const transferRow = screen.getByText('Transfer').closest('tr')!
    expect(transferRow).toHaveTextContent('Barclays → Trading212')
    expect(transferRow).toHaveTextContent('500.00')
    expect(transferRow).toHaveTextContent('Top-up')

    const adjustmentRow = screen.getByText('Adjustment').closest('tr')!
    expect(adjustmentRow).toHaveTextContent('Barclays')
    expect(adjustmentRow).toHaveTextContent('-4.20')
    expect(adjustmentRow).toHaveTextContent('Matched statement')
  })

  it('calls onEditTransfer and onDeleteTransfer for a transfer row', () => {
    const onEditTransfer = vi.fn()
    const onDeleteTransfer = vi.fn()
    render(
      <BankOperationsSection
        {...baseProps}
        operations={[TRANSFER_ENTRY]}
        onEditTransfer={onEditTransfer}
        onDeleteTransfer={onDeleteTransfer}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Edit transfer' }))
    fireEvent.click(screen.getByRole('button', { name: 'Delete transfer' }))

    expect(onEditTransfer).toHaveBeenCalledWith(TRANSFER_ENTRY.transfer)
    expect(onDeleteTransfer).toHaveBeenCalledWith('t1')
  })

  it('calls onEditAdjustment and onDeleteAdjustment for an adjustment row', () => {
    const onEditAdjustment = vi.fn()
    const onDeleteAdjustment = vi.fn()
    render(
      <BankOperationsSection
        {...baseProps}
        operations={[ADJUSTMENT_ENTRY]}
        onEditAdjustment={onEditAdjustment}
        onDeleteAdjustment={onDeleteAdjustment}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Edit balance adjustment' }))
    fireEvent.click(screen.getByRole('button', { name: 'Delete balance adjustment' }))

    expect(onEditAdjustment).toHaveBeenCalledWith(ADJUSTMENT_ENTRY.adjustment)
    expect(onDeleteAdjustment).toHaveBeenCalledWith('bank-barclays', 'a1')
  })

  it('shows the empty state message when there are no operations at all', () => {
    render(<BankOperationsSection {...baseProps} operations={[]} />)

    expect(screen.getByText('No transfers or balance corrections this month.')).toBeInTheDocument()
  })

  it('sorts rows by Amount/Delta when the column header is clicked', () => {
    render(<BankOperationsSection {...baseProps} operations={[TRANSFER_ENTRY, ADJUSTMENT_ENTRY]} />)

    fireEvent.click(screen.getByRole('button', { name: 'Amount/Delta' }))

    const typeCells = screen
      .getAllByRole('row')
      .slice(1)
      .map((row) => row.querySelectorAll('td')[1].textContent)
    expect(typeCells).toEqual(['Adjustment', 'Transfer'])

    fireEvent.click(screen.getByRole('button', { name: 'Amount/Delta' }))

    const typeCellsDescending = screen
      .getAllByRole('row')
      .slice(1)
      .map((row) => row.querySelectorAll('td')[1].textContent)
    expect(typeCellsDescending).toEqual(['Transfer', 'Adjustment'])
  })

  it('filters by Bank via the header checklist, matching a transfer by its other bank even when one is unchecked', () => {
    render(<BankOperationsSection {...baseProps} operations={[TRANSFER_ENTRY, ADJUSTMENT_ENTRY]} />)

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Bank' }))
    // Start all-checked; uncheck Barclays. The transfer (Barclays -> Trading212) stays visible
    // because Trading212 is still checked (OR within the column); the Barclays-only adjustment
    // is excluded.
    fireEvent.click(screen.getByRole('checkbox', { name: 'Barclays' }))

    expect(screen.getByText('Transfer').closest('tr')).toBeInTheDocument()
    expect(screen.queryByText('Adjustment')).not.toBeInTheDocument()
  })

  it('shows the "no rows match" message when the Bank filter excludes every operation', () => {
    render(<BankOperationsSection {...baseProps} operations={[ADJUSTMENT_ENTRY]} />)

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Bank' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Barclays' }))

    expect(screen.getByText('No rows match the current filters')).toBeInTheDocument()
  })
})
