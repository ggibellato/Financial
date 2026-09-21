import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, within } from '../../../test/renderWithFluent'
import type { UpcomingIncomeDto } from '../../../api/types'
import UpcomingIncomePanel from '../UpcomingIncomePanel'

function inDays(days: number): string {
  const date = new Date()
  date.setDate(date.getDate() + days)
  return date.toISOString()
}

function entry(assetName: string, days: number, projectedAmount: number): UpcomingIncomeDto {
  return {
    assetName,
    brokerName: `${assetName} Broker`,
    lastCreditDate: inDays(days - 90),
    projectedAmount,
    projectedNextDate: inDays(days),
  }
}

const ENTRIES: UpcomingIncomeDto[] = [
  entry('VUSA', 10, 42.5),
  entry('VWRL', 60, 18.25),
  entry('KLBN4', 150, 96),
]

const renderPanel = (entries: UpcomingIncomeDto[] | null = ENTRIES) =>
  render(<UpcomingIncomePanel entries={entries} isLoading={false} error={null} retry={vi.fn()} />)

function visibleAssets(): string[] {
  return screen
    .getAllByRole('row')
    .slice(1)
    .map((row) => (within(row).getAllByRole('cell')[0].textContent ?? '').replace('Asset:', ''))
}

describe('UpcomingIncomePanel', () => {
  it('defaults_to_the_ninety_day_window', () => {
    renderPanel()

    expect(screen.getByRole('tab', { name: '90 days' })).toHaveAttribute('aria-selected', 'true')
    expect(visibleAssets()).toEqual(['VUSA', 'VWRL'])
  })

  it('renders_the_asset_broker_projected_date_and_amount_columns', () => {
    renderPanel([entry('VUSA', 10, 42.5)])

    const headers = screen.getAllByRole('columnheader').map((header) => header.textContent)
    expect(headers).toEqual(['Asset', 'Broker', 'Projected Date', 'Projected Amount'])

    const cells = within(screen.getAllByRole('row')[1])
      .getAllByRole('cell')
      .map((cell) => cell.textContent)
    expect(cells[0]).toBe('Asset:VUSA')
    expect(cells[1]).toBe('Broker:VUSA Broker')
    expect(cells[3]).toBe('Projected Amount:42.50')
  })

  it('re_filters_the_already_held_list_when_the_window_changes_keeping_the_backend_order', () => {
    renderPanel()

    fireEvent.click(screen.getByRole('tab', { name: '30 days' }))
    expect(visibleAssets()).toEqual(['VUSA'])

    fireEvent.click(screen.getByRole('tab', { name: '180 days' }))
    expect(visibleAssets()).toEqual(['VUSA', 'VWRL', 'KLBN4'])
  })

  it('keeps_an_already_due_projection_visible_in_every_window', () => {
    renderPanel([entry('OVERDUE', -20, 5)])

    expect(visibleAssets()).toEqual(['OVERDUE'])

    fireEvent.click(screen.getByRole('tab', { name: '30 days' }))
    expect(visibleAssets()).toEqual(['OVERDUE'])
  })

  it('P52-F04-upcoming-income-04: the empty state shows when no projection falls inside the selected window', () => {
    renderPanel([entry('KLBN4', 150, 96)])

    expect(screen.getByText('No upcoming payments detected in the next 90 days')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('tab', { name: '30 days' }))
    expect(screen.getByText('No upcoming payments detected in the next 30 days')).toBeInTheDocument()
  })

  it('shows_the_loading_state_while_the_request_is_in_flight', () => {
    render(<UpcomingIncomePanel entries={null} isLoading error={null} retry={vi.fn()} />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    expect(screen.queryByRole('tab')).not.toBeInTheDocument()
  })

  it('shows_its_own_error_state_with_a_retry_action', () => {
    const retry = vi.fn()
    render(<UpcomingIncomePanel entries={null} isLoading={false} error="Income service unavailable" retry={retry} />)

    expect(screen.getByRole('alert')).toHaveTextContent('Income service unavailable')

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(retry).toHaveBeenCalledTimes(1)
  })
})
