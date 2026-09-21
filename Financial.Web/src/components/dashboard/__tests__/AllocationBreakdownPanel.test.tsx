import React from 'react'
import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, within } from '../../../test/renderWithFluent'
import type { AllocationBreakdownDto } from '../../../api/types'
import AllocationBreakdownPanel from '../AllocationBreakdownPanel'

interface MockPieDatum {
  name: string
  value: number
  percentage: number
}

vi.mock('recharts', () => ({
  PieChart: ({ children }: { children: React.ReactNode }) => <div data-testid="pie-chart">{children}</div>,
  Pie: ({ data, children }: { data: MockPieDatum[]; children?: React.ReactNode }) => (
    <div data-testid="pie">
      {data.map((d) => (
        <span key={d.name} data-testid="pie-slice">
          {d.name}
        </span>
      ))}
      {children}
    </div>
  ),
  Cell: () => null,
  Tooltip: () => null,
  Legend: () => <div data-testid="legend" />,
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
}))

const BREAKDOWN: AllocationBreakdownDto = {
  byClass: [
    { class: 'Equity', marketValue: 12000, percentage: 60 },
    { class: 'Bond', marketValue: 8000, percentage: 40 },
  ],
  byCurrency: [{ currency: 'GBP', marketValue: 20000, percentage: 100 }],
  byCountry: [{ country: 'UK', marketValue: 20000, percentage: 100 }],
  byBroker: [{ brokerName: 'Trading212', marketValue: 20000, percentage: 100 }],
}

function renderPanel(overrides: Partial<React.ComponentProps<typeof AllocationBreakdownPanel>> = {}) {
  return render(
    <AllocationBreakdownPanel
      breakdown={BREAKDOWN}
      isLoading={false}
      error={null}
      retry={vi.fn()}
      {...overrides}
    />,
  )
}

function legendLabels(): string[] {
  return screen
    .getAllByRole('row')
    .slice(1)
    .map((row) => within(row).getAllByRole('cell')[0].textContent?.replace('Label:', '') ?? '')
}

describe('AllocationBreakdownPanel', () => {
  it('renders_the_four_dimension_tabs', () => {
    renderPanel()

    const tabs = screen.getAllByRole('tab')
    expect(tabs).toHaveLength(4)
    tabs.forEach((tab, index) => {
      expect(tab).toHaveAccessibleName(['Class', 'Currency', 'Country', 'Broker'][index])
    })
  })

  it('defaults_to_the_class_dimension_on_first_load', () => {
    renderPanel()

    expect(screen.getByRole('tab', { name: 'Class' })).toHaveAttribute('aria-selected', 'true')
    expect(legendLabels()).toEqual(['Equity', 'Bond'])
  })

  it('switching_tabs_swaps_the_rendered_chart_data', () => {
    renderPanel()

    fireEvent.click(screen.getByRole('tab', { name: 'Broker' }))

    expect(legendLabels()).toEqual(['Trading212'])
    expect(screen.getAllByTestId('pie-slice').map((slice) => slice.textContent)).toEqual(['Trading212'])

    fireEvent.click(screen.getByRole('tab', { name: 'Country' }))
    expect(legendLabels()).toEqual(['UK'])

    fireEvent.click(screen.getByRole('tab', { name: 'Currency' }))
    expect(legendLabels()).toEqual(['GBP'])
  })

  it('shows_the_empty_message_when_the_selected_dimension_has_no_entries', () => {
    renderPanel({ breakdown: { ...BREAKDOWN, byCountry: [] } })

    fireEvent.click(screen.getByRole('tab', { name: 'Country' }))

    expect(screen.getByText('No priced holdings to display for this view.')).toBeInTheDocument()
    expect(screen.queryByTestId('pie-chart')).not.toBeInTheDocument()
  })

  it('renders_no_click_or_selection_affordance_on_any_slice_or_legend_row', () => {
    renderPanel()

    const legendRows = screen.getAllByRole('row').slice(1)
    legendRows.forEach((row) => {
      expect(within(row).queryByRole('button')).not.toBeInTheDocument()
      expect(within(row).queryByRole('link')).not.toBeInTheDocument()
      expect(row).not.toHaveAttribute('onclick')
      expect(row.tabIndex).toBeLessThan(0)
    })
    screen.getAllByTestId('pie-slice').forEach((slice) => {
      expect(slice).not.toHaveAttribute('onclick')
    })
  })

  it('renders_the_loading_state_while_the_breakdown_is_loading', () => {
    renderPanel({ breakdown: null, isLoading: true })

    expect(screen.getByText('Loading...')).toBeInTheDocument()
    expect(screen.queryByRole('tab')).not.toBeInTheDocument()
  })

  it('renders_the_error_state_with_retry_on_failure', () => {
    const retry = vi.fn()
    renderPanel({ breakdown: null, error: 'Unable to load allocation breakdown', retry })

    expect(screen.getByText('Unable to load allocation breakdown')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(retry).toHaveBeenCalledTimes(1)
  })

  it('renders_nothing_when_there_is_no_breakdown_and_no_error', () => {
    const { container } = renderPanel({ breakdown: null })

    expect(container.querySelector('.allocation-breakdown')).toBeNull()
  })
})
