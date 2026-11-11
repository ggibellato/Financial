import { describe, expect, it, vi } from 'vitest'
import type { PotsSummaryDto } from '../../api/types'
import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import PotsTab from '../PotsTab'

const SUMMARY: PotsSummaryDto = {
  potsAccountId: 'reserva',
  currency: 'GBP',
  total: 190.25,
  pots: [
    { id: 'p1', name: 'Home', splitPercentage: 66.67, isActive: true, balance: 210.4 },
    { id: 'p2', name: 'Trips', splitPercentage: 33.33, isActive: true, balance: -20.15 },
    { id: 'p3', name: 'Old car', splitPercentage: 0, isActive: false, balance: 0 },
  ],
}

function renderTab(props: Partial<Parameters<typeof PotsTab>[0]> = {}) {
  return render(<PotsTab summary={SUMMARY} hasError={false} onRetry={vi.fn()} year={2026} month={10} {...props} />)
}

function bodyRows() {
  const [, ...rows] = screen.getAllByRole('row')
  return rows
}

describe('PotsTab', () => {
  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-15] lists each pot with its split and balance, and the total for the month', () => {
    renderTab()

    expect(screen.getByRole('heading', { level: 2, name: 'Pots' })).toBeInTheDocument()
    expect(screen.getByText('Balances at end of October 2026')).toBeInTheDocument()
    expect(screen.getAllByRole('columnheader').map((header) => header.textContent)).toEqual(['Pot', 'Split %', 'Balance'])
    expect(within(bodyRows()[0]).getByText('66.67%')).toBeInTheDocument()
    expect(within(bodyRows()[0]).getByText('210.40')).toBeInTheDocument()
    expect(within(bodyRows()[1]).getByText('-20.15')).toBeInTheDocument()
    expect(screen.getByText('Pots total GBP:', { exact: false })).toHaveTextContent('Pots total GBP: 190.25')
  })

  it('names an inactive pot in text, not by colour', () => {
    renderTab()

    expect(within(bodyRows()[2]).getByText('Inactive')).toBeInTheDocument()
    expect(within(bodyRows()[0]).queryByText('Inactive')).not.toBeInTheDocument()
  })

  it('keeps the server order until a column is sorted', () => {
    renderTab()

    expect(bodyRows().map((row) => within(row).getAllByRole('cell')[0].textContent)).toEqual(['Pot:Home', 'Pot:Trips', 'Pot:Old carInactive'])

    fireEvent.click(screen.getByRole('button', { name: 'Balance' }))

    expect(within(bodyRows()[0]).getByText('-20.15')).toBeInTheDocument()
  })

  it('shows a skeleton while there are no figures to show', () => {
    renderTab({ summary: null })

    expect(screen.getByLabelText('Loading pots')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('shows the error with a retry instead of stale figures', () => {
    const onRetry = vi.fn()
    renderTab({ hasError: true, onRetry })

    expect(screen.getByText('Unable to load the pots.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(onRetry).toHaveBeenCalledOnce()
  })

  it('says when there are no pots, with no total', () => {
    renderTab({ summary: { ...SUMMARY, total: 0, pots: [] } })

    expect(screen.getByText('This account has no pots yet.')).toBeInTheDocument()
    expect(screen.queryByText(/Pots total/)).not.toBeInTheDocument()
  })
})
