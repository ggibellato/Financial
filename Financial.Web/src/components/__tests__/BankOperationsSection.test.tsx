import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import BankOperationsSection from '../BankOperationsSection'
import type { BankDto } from '../../api/types'
import { ALL_BANKS_FILTER, type BankOperationEntry } from '../../hooks/useBankOperations'

const BANKS: BankDto[] = [
  { name: 'Barclays', roundUpEnabled: false },
  { name: 'Trading212', roundUpEnabled: true },
]

const TRANSFER_ENTRY: BankOperationEntry = {
  kind: 'transfer',
  id: 't1',
  date: '2026-07-05',
  sourceBank: 'Barclays',
  destinationBank: 'Trading212',
  amount: 500,
  note: 'Top-up',
  transfer: { id: 't1', date: '2026-07-05', sourceBank: 'Barclays', destinationBank: 'Trading212', amount: 500, note: 'Top-up' },
}

const ADJUSTMENT_ENTRY: BankOperationEntry = {
  kind: 'adjustment',
  id: 'a1',
  date: '2026-07-10',
  bank: 'Barclays',
  delta: -4.2,
  note: 'Matched statement',
  adjustment: { id: 'a1', date: '2026-07-10', bank: 'Barclays', targetBalance: 38.3, delta: -4.2, note: 'Matched statement' },
}

const baseProps = {
  operations: [] as BankOperationEntry[],
  bankFilter: ALL_BANKS_FILTER,
  banks: BANKS,
  onBankFilterChange: vi.fn(),
  onNewTransfer: vi.fn(),
  onNewBalanceCorrection: vi.fn(),
  onEditTransfer: vi.fn(),
  onEditAdjustment: vi.fn(),
  onDeleteTransfer: vi.fn(),
  onDeleteAdjustment: vi.fn(),
}

describe('BankOperationsSection', () => {
  it('renders both entry-point buttons and the filter dropdown with All Banks default', () => {
    render(<BankOperationsSection {...baseProps} />)

    expect(screen.getByRole('button', { name: '+ New Transfer' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '+ New Balance Correction' })).toBeInTheDocument()
    expect(screen.getByLabelText('Filter by Bank')).toHaveValue(ALL_BANKS_FILTER)
  })

  it('calls onNewTransfer and onNewBalanceCorrection', () => {
    const onNewTransfer = vi.fn()
    const onNewBalanceCorrection = vi.fn()
    render(
      <BankOperationsSection {...baseProps} onNewTransfer={onNewTransfer} onNewBalanceCorrection={onNewBalanceCorrection} />,
    )

    fireEvent.click(screen.getByRole('button', { name: '+ New Transfer' }))
    fireEvent.click(screen.getByRole('button', { name: '+ New Balance Correction' }))

    expect(onNewTransfer).toHaveBeenCalledOnce()
    expect(onNewBalanceCorrection).toHaveBeenCalledOnce()
  })

  it('calls onBankFilterChange with the selected bank', () => {
    const onBankFilterChange = vi.fn()
    render(<BankOperationsSection {...baseProps} onBankFilterChange={onBankFilterChange} />)

    fireEvent.change(screen.getByLabelText('Filter by Bank'), { target: { value: 'Trading212' } })

    expect(onBankFilterChange).toHaveBeenCalledWith('Trading212')
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
    expect(onDeleteAdjustment).toHaveBeenCalledWith('Barclays', 'a1')
  })

  it('shows the unfiltered empty state message when there are no operations and no filter', () => {
    render(<BankOperationsSection {...baseProps} operations={[]} bankFilter={ALL_BANKS_FILTER} />)

    expect(screen.getByText('No transfers or balance corrections this month.')).toBeInTheDocument()
  })

  it('shows the filtered-by-bank empty state message when a bank filter is active', () => {
    render(<BankOperationsSection {...baseProps} operations={[]} bankFilter="Barclays" />)

    expect(screen.getByText('No transfers or balance corrections this month for Barclays.')).toBeInTheDocument()
  })
})
