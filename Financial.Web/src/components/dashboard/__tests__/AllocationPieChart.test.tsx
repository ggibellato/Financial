import { render, screen, within } from '@testing-library/react'
import React from 'react'
import { describe, expect, it, vi } from 'vitest'
import AllocationPieChart, { type AllocationChartEntry } from '../AllocationPieChart'

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
        <span key={d.name} data-testid="pie-slice" data-percentage={d.percentage}>
          {d.name}
        </span>
      ))}
      {children}
    </div>
  ),
  Cell: () => null,
  Tooltip: ({
    content,
  }: {
    content?: (props: {
      active: boolean
      payload: { payload: MockPieDatum }[]
    }) => React.ReactNode
  }) => (
    <div data-testid="tooltip">
      {content
        ? content({
            active: true,
            payload: [{ payload: { name: 'Equity', value: 1234.5, percentage: 25.6 } }],
          })
        : null}
    </div>
  ),
  Legend: () => <div data-testid="legend" />,
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
}))

const ENTRIES: AllocationChartEntry[] = [
  { label: 'Equity', marketValue: 12000, percentage: 60 },
  { label: 'Bond', marketValue: 5000, percentage: 25 },
  { label: 'Cash', marketValue: 3000, percentage: 15 },
]

function renderChart(entries: AllocationChartEntry[] = ENTRIES) {
  return render(<AllocationPieChart title="Allocation by asset class" entries={entries} />)
}

describe('AllocationPieChart', () => {
  it('renders_the_legend_table_with_label_market_value_and_percentage_columns', () => {
    renderChart()

    const headers = screen.getAllByRole('columnheader').map((header) => header.textContent)
    expect(headers).toEqual(['Label', 'Market Value', 'Percentage'])
  })

  it('renders_the_chart_title_as_the_legend_table_caption', () => {
    renderChart()

    expect(screen.getByText('Allocation by asset class')).toBeInTheDocument()
  })

  it('renders_legend_rows_in_the_order_the_props_provide_without_re_sorting', () => {
    renderChart([
      { label: 'Cash', marketValue: 3000, percentage: 15 },
      { label: 'Equity', marketValue: 12000, percentage: 60 },
    ])

    const rows = screen.getAllByRole('row').slice(1)
    expect(rows.map((row) => within(row).getAllByRole('cell')[0].textContent)).toEqual(['Cash', 'Equity'])
  })

  it('right_aligns_the_numeric_legend_columns', () => {
    renderChart()

    const rows = screen.getAllByRole('row').slice(1)
    const cells = within(rows[0]).getAllByRole('cell')
    expect(cells[0]).not.toHaveClass('data-table__col--numeric')
    expect(cells[1]).toHaveClass('data-table__col--numeric')
    expect(cells[2]).toHaveClass('data-table__col--numeric')

    const headers = screen.getAllByRole('columnheader')
    expect(headers[1]).toHaveClass('data-table__col--numeric')
    expect(headers[2]).toHaveClass('data-table__col--numeric')
  })

  it('formats_market_value_and_percentage_in_the_legend_rows', () => {
    renderChart()

    const rows = screen.getAllByRole('row').slice(1)
    const cells = within(rows[0]).getAllByRole('cell')
    expect(cells[1].textContent).toMatch(/12[.,]000[.,]00/)
    expect(cells[2].textContent).toBe('60.0%')
  })

  it('renders_one_pie_slice_per_entry_in_the_same_order', () => {
    renderChart()

    const slices = screen.getAllByTestId('pie-slice')
    expect(slices.map((slice) => slice.textContent)).toEqual(['Equity', 'Bond', 'Cash'])
  })

  it('renders_tooltip_content_with_name_value_and_formatted_percentage', () => {
    renderChart()

    const tooltip = screen.getByTestId('tooltip')
    expect(within(tooltip).getByText('Equity')).toBeInTheDocument()
    expect(within(tooltip).getByText(/1[.,]234[.,]50/)).toBeInTheDocument()
    expect(within(tooltip).getByText('25.6%')).toBeInTheDocument()
  })

  it('renders_an_empty_legend_body_when_there_are_no_entries', () => {
    renderChart([])

    expect(screen.getAllByRole('row')).toHaveLength(1)
  })
})
