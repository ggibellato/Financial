import { render, screen } from '@testing-library/react'
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
  percent: number
}

vi.mock('recharts', () => ({
  PieChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="pie-chart">{children}</div>
  ),
  Pie: ({ data, children }: { data: MockPieDatum[]; children?: React.ReactNode }) => (
    <div data-testid="pie">
      {data.map((d) => (
        <span key={d.name} data-testid="pie-slice" data-percent={d.percent}>
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
      payload: { name: string; value: number; payload: { name: string; value: number; percent: number } }[]
    }) => React.ReactNode
  }) => (
    <div data-testid="tooltip">
      {content
        ? content({
            active: true,
            payload: [
              {
                name: 'Test Slice',
                value: 1234.5,
                payload: { name: 'Test Slice', value: 1234.5, percent: 0.256 },
              },
            ],
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
    expect(Number(overviewSlices[0].getAttribute('data-percent'))).toBeCloseTo(38639.49 / total, 5)
    expect(Number(overviewSlices[1].getAttribute('data-percent'))).toBeCloseTo(20000 / total, 5)
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

  it('renders_nothing_extra_when_breakdown_is_null', () => {
    setMock({ breakdown: null })
    const { container } = render(<BrokerBreakdownCharts />)
    expect(container).toBeEmptyDOMElement()
  })
})
