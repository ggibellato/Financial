import { fireEvent, render, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import BanksGrid from '../BanksGrid'
import type { BankHistoryEntry } from '../../hooks/useBankHistory'
import type { BankTotal } from '../../hooks/useMonthly'

const BANK_TOTALS: BankTotal[] = [
  { bank: 'Barclays', balance: 42.5, roundUpTotal: 0 },
  { bank: 'Trading212', balance: 8.8, roundUpTotal: 0.6 },
]

const TRANSFER_ENTRY: BankHistoryEntry = {
  kind: 'transferOut',
  id: 't1',
  date: '2026-07-05',
  counterpartBank: 'Trading212',
  amount: 500,
  note: 'Top-up',
  transfer: { id: 't1', date: '2026-07-05', sourceBank: 'Barclays', destinationBank: 'Trading212', amount: 500, note: 'Top-up' },
}

const ADJUSTMENT_ENTRY: BankHistoryEntry = {
  kind: 'adjustment',
  id: 'a1',
  date: '2026-07-10',
  delta: -4.2,
  note: 'Matched statement',
  adjustment: { id: 'a1', date: '2026-07-10', bank: 'Barclays', targetBalance: 38.3, delta: -4.2, note: 'Matched statement' },
}

const baseProps = {
  bankTotals: BANK_TOTALS,
  bankTotalsSum: 51.3,
  roundUpTotalsSum: 0.6,
  historyByBank: {},
  expandedBank: null,
  onToggleExpand: vi.fn(),
  onMoveMoney: vi.fn(),
  onCorrectBalance: vi.fn(),
  onEditTransfer: vi.fn(),
  onEditAdjustment: vi.fn(),
  onDeleteTransfer: vi.fn(),
  onDeleteAdjustment: vi.fn(),
}

describe('BanksGrid', () => {
  it('renders a row per bank with balance and round-up columns, plus footer totals', () => {
    render(<BanksGrid {...baseProps} />)

    const barclaysRow = screen.getByRole('row', { name: /Barclays/ })
    expect(within(barclaysRow).getByRole('cell', { name: '42.50' })).toBeInTheDocument()

    const trading212Row = screen.getByRole('row', { name: /Trading212/ })
    expect(within(trading212Row).getByRole('cell', { name: '8.80' })).toBeInTheDocument()
    expect(within(trading212Row).getByRole('cell', { name: '0.60' })).toBeInTheDocument()

    expect(screen.getByText('51.30')).toBeInTheDocument()
  })

  it('calls onMoveMoney and onCorrectBalance with the row bank (and balance)', () => {
    const onMoveMoney = vi.fn()
    const onCorrectBalance = vi.fn()
    render(<BanksGrid {...baseProps} onMoveMoney={onMoveMoney} onCorrectBalance={onCorrectBalance} />)

    const barclaysRow = screen.getByRole('row', { name: /Barclays/ })
    fireEvent.click(within(barclaysRow).getByRole('button', { name: 'Move Money' }))
    fireEvent.click(within(barclaysRow).getByRole('button', { name: 'Correct Balance' }))

    expect(onMoveMoney).toHaveBeenCalledWith('Barclays')
    expect(onCorrectBalance).toHaveBeenCalledWith('Barclays', 42.5)
  })

  it('calls onToggleExpand with the row bank when the expand button is clicked', () => {
    const onToggleExpand = vi.fn()
    render(<BanksGrid {...baseProps} onToggleExpand={onToggleExpand} />)

    fireEvent.click(screen.getByRole('button', { name: 'Expand history for Barclays' }))

    expect(onToggleExpand).toHaveBeenCalledWith('Barclays')
  })

  it('shows the history table only for the expanded bank', () => {
    render(
      <BanksGrid
        {...baseProps}
        historyByBank={{ Barclays: [TRANSFER_ENTRY, ADJUSTMENT_ENTRY] }}
        expandedBank="Barclays"
      />,
    )

    expect(screen.getByText('Transfer Out')).toBeInTheDocument()
    expect(screen.getByText('Adjustment')).toBeInTheDocument()
    const transferRow = screen.getByText('Transfer Out').closest('tr')!
    expect(within(transferRow).getByText('Trading212')).toBeInTheDocument()
    const adjustmentRow = screen.getByText('Adjustment').closest('tr')!
    expect(within(adjustmentRow).getByText('-4.20')).toBeInTheDocument()
  })

  it('shows an empty state when the expanded bank has no history', () => {
    render(<BanksGrid {...baseProps} historyByBank={{ Barclays: [] }} expandedBank="Barclays" />)

    expect(screen.getByText('No transfers or adjustments this month.')).toBeInTheDocument()
  })

  it('does not render history when no bank is expanded', () => {
    render(<BanksGrid {...baseProps} historyByBank={{ Barclays: [TRANSFER_ENTRY] }} expandedBank={null} />)

    expect(screen.queryByText('Transfer Out')).not.toBeInTheDocument()
  })

  it('calls onEditTransfer and onDeleteTransfer for a transfer history row', () => {
    const onEditTransfer = vi.fn()
    const onDeleteTransfer = vi.fn()
    render(
      <BanksGrid
        {...baseProps}
        historyByBank={{ Barclays: [TRANSFER_ENTRY] }}
        expandedBank="Barclays"
        onEditTransfer={onEditTransfer}
        onDeleteTransfer={onDeleteTransfer}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Edit transfer' }))
    fireEvent.click(screen.getByRole('button', { name: 'Delete transfer' }))

    expect(onEditTransfer).toHaveBeenCalledWith(TRANSFER_ENTRY.kind === 'transferOut' ? TRANSFER_ENTRY.transfer : undefined)
    expect(onDeleteTransfer).toHaveBeenCalledWith('t1')
  })

  it('calls onEditAdjustment and onDeleteAdjustment for an adjustment history row', () => {
    const onEditAdjustment = vi.fn()
    const onDeleteAdjustment = vi.fn()
    render(
      <BanksGrid
        {...baseProps}
        historyByBank={{ Barclays: [ADJUSTMENT_ENTRY] }}
        expandedBank="Barclays"
        onEditAdjustment={onEditAdjustment}
        onDeleteAdjustment={onDeleteAdjustment}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Edit balance adjustment' }))
    fireEvent.click(screen.getByRole('button', { name: 'Delete balance adjustment' }))

    expect(onEditAdjustment).toHaveBeenCalledWith(
      'Barclays',
      42.5,
      ADJUSTMENT_ENTRY.kind === 'adjustment' ? ADJUSTMENT_ENTRY.adjustment : undefined,
    )
    expect(onDeleteAdjustment).toHaveBeenCalledWith('Barclays', 'a1')
  })
})
