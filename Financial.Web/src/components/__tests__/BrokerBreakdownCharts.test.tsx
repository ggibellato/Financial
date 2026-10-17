import { render, screen, within } from '@testing-library/react'
import React from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { BrokerBreakdownData } from '../../hooks/useBrokerBreakdown'
import type { PortfolioBreakdownItemDto } from '../../api/types'
import BrokerBreakdownCharts from '../BrokerBreakdownCharts'

const mockRetry = vi.fn()

const mockHookValue: BrokerBreakdownData = {
  breakdown: null,
  isLoading: false,
  error: null,
  retry: mockRetry,
}

vi.mock('../../hooks/useBrokerBreakdown', () => ({
  useBrokerBreakdown: () => mockHookValue,
}))

interface MockPieDatum {
  name: string
  value: number
  percentage: number
}

vi.mock('recharts', () => ({
  PieChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="pie-chart">{children}</div>
  ),
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
            payload: [{ payload: { name: 'Test Slice', value: 1234.5, percentage: 25.6 } }],
          })
        : null}
    </div>
  ),
  Legend: () => <div data-testid="legend" />,
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
}))

function setMock(overrides: Partial<BrokerBreakdownData>) {
  Object.assign(mockHookValue, overrides)
}

const BREAKDOWN: PortfolioBreakdownItemDto[] = [
  {
    portfolioName: 'Acoes',
    totalInvested: 38639.49,
    assets: [
      { assetName: 'BBAS3', totalInvested: 9850.4 },
      { assetName: 'KLBN4', totalInvested: 3737.48 },
    ],
  },
  {
    portfolioName: 'FII',
    totalInvested: 20000,
    assets: [{ assetName: 'MXRF11', totalInvested: 20000 }],
  },
]

describe('BrokerBreakdownCharts', () => {
  beforeEach(() => {
    mockRetry.mockReset()
    Object.assign(mockHookValue, { breakdown: null, isLoading: false, error: null })
  })

  it('renders_loading_state_independently_of_totals', () => {
    setMock({ isLoading: true })
    render(<BrokerBreakdownCharts />)
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry_on_failure', () => {
    setMock({ error: 'Unable to load breakdown' })
    render(<BrokerBreakdownCharts />)
    expect(screen.getByText('Unable to load breakdown')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders_empty_state_when_breakdown_is_empty_array', () => {
    setMock({ breakdown: [] })
    render(<BrokerBreakdownCharts />)
    expect(screen.getByText('No active portfolios to display')).toBeInTheDocument()
    expect(screen.queryAllByTestId('pie-chart')).toHaveLength(0)
  })

  it('renders_portfolio_breakdown_pie_with_one_slice_per_portfolio', () => {
    setMock({ breakdown: BREAKDOWN })
    render(<BrokerBreakdownCharts />)
    expect(screen.getByText('Portfolio Breakdown')).toBeInTheDocument()
    const pies = screen.getAllByTestId('pie')
    const overviewSlices = pies[0].querySelectorAll('[data-testid="pie-slice"]')
    expect(overviewSlices).toHaveLength(2)
    expect(overviewSlices[0].textContent).toBe('Acoes')
    expect(overviewSlices[1].textContent).toBe('FII')
  })

  it('computes_correct_percentage_per_slice', () => {
    setMock({ breakdown: BREAKDOWN })
    render(<BrokerBreakdownCharts />)
    const pies = screen.getAllByTestId('pie')
    const overviewSlices = pies[0].querySelectorAll('[data-testid="pie-slice"]')
    const total = 38639.49 + 20000
    expect(Number(overviewSlices[0].getAttribute('data-percentage'))).toBeCloseTo((38639.49 / total) * 100, 5)
    expect(Number(overviewSlices[1].getAttribute('data-percentage'))).toBeCloseTo((20000 / total) * 100, 5)
  })

  it('renders_one_additional_pie_per_portfolio', () => {
    setMock({ breakdown: BREAKDOWN })
    render(<BrokerBreakdownCharts />)
    expect(screen.getByRole('heading', { name: 'Acoes' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'FII' })).toBeInTheDocument()
    const pieCharts = screen.getAllByTestId('pie-chart')
    expect(pieCharts).toHaveLength(3)
  })

  it('renders_tooltip_content_with_name_value_and_formatted_percentage', () => {
    setMock({ breakdown: BREAKDOWN })
    render(<BrokerBreakdownCharts />)
    expect(screen.getAllByText('Test Slice').length).toBeGreaterThan(0)
    expect(screen.getAllByText(/1,234\.50|1234[.,]50/).length).toBeGreaterThan(0)
    expect(screen.getAllByText('25.6%').length).toBeGreaterThan(0)
  })

  it('renders_an_accessible_table_equivalent_for_every_pie', () => {
    setMock({ breakdown: BREAKDOWN })
    render(<BrokerBreakdownCharts />)
    expect(screen.getAllByRole('table')).toHaveLength(3)
    expect(screen.getByRole('table', { name: 'Portfolio Breakdown' })).toBeInTheDocument()
    expect(screen.getByRole('table', { name: 'Acoes' })).toBeInTheDocument()
    expect(screen.getByRole('table', { name: 'FII' })).toBeInTheDocument()
  })

  it('lists_each_portfolio_with_total_invested_and_share_in_the_overview_table', () => {
    setMock({ breakdown: BREAKDOWN })
    render(<BrokerBreakdownCharts />)
    const table = screen.getByRole('table', { name: 'Portfolio Breakdown' })
    const headers = within(table).getAllByRole('columnheader').map((header) => header.textContent)
    expect(headers).toEqual(['Label', 'Total Invested', 'Percentage'])
    const rows = within(table).getAllByRole('row').slice(1)
    expect(rows).toHaveLength(2)
    expect(within(rows[0]).getByText('Acoes')).toBeInTheDocument()
    expect(within(rows[0]).getByText(/38,639\.49|38639[.,]49/)).toBeInTheDocument()
    expect(within(rows[0]).getByText('65.9%')).toBeInTheDocument()
    expect(within(rows[1]).getByText('FII')).toBeInTheDocument()
    expect(within(rows[1]).getByText('34.1%')).toBeInTheDocument()
  })

  it('lists_each_asset_of_a_portfolio_in_its_table', () => {
    setMock({ breakdown: BREAKDOWN })
    render(<BrokerBreakdownCharts />)
    const table = screen.getByRole('table', { name: 'Acoes' })
    const rows = within(table).getAllByRole('row').slice(1)
    expect(rows).toHaveLength(2)
    expect(within(rows[0]).getByText('BBAS3')).toBeInTheDocument()
    expect(within(rows[1]).getByText('KLBN4')).toBeInTheDocument()
    expect(within(rows[0]).getByText('72.5%')).toBeInTheDocument()
    expect(within(rows[1]).getByText('27.5%')).toBeInTheDocument()
  })

  it('renders_nothing_extra_when_breakdown_is_null', () => {
    setMock({ breakdown: null })
    const { container } = render(<BrokerBreakdownCharts />)
    expect(container).toBeEmptyDOMElement()
  })
})
