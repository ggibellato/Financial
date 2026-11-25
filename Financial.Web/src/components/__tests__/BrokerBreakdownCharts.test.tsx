import { render, screen, within } from '../../test/renderWithFluent'
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

vi.mock('recharts', () => import('../../test/rechartsPieMock'))

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
    expect(screen.getByText('Loading…')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry_on_failure', () => {
    setMock({ error: 'Unable to load breakdown.' })
    render(<BrokerBreakdownCharts />)
    expect(screen.getByText('Unable to load breakdown.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders_empty_state_when_breakdown_is_empty_array', () => {
    setMock({ breakdown: [] })
    render(<BrokerBreakdownCharts />)
    expect(screen.getByText('No active portfolios to display')).toBeInTheDocument()
    expect(screen.queryAllByTestId('pie-chart')).toHaveLength(0)
  })

  it('renders_an_accessible_table_equivalent_for_every_pie', () => {
    setMock({ breakdown: BREAKDOWN })
    render(<BrokerBreakdownCharts />)
    expect(screen.getAllByRole('table')).toHaveLength(4)
    expect(screen.getByRole('table', { name: 'All assets' })).toBeInTheDocument()
    expect(screen.getByRole('table', { name: 'Portfolio breakdown' })).toBeInTheDocument()
    expect(screen.getByRole('table', { name: 'Acoes' })).toBeInTheDocument()
    expect(screen.getByRole('table', { name: 'FII' })).toBeInTheDocument()
  })

  it('lists_every_asset_of_the_broker_by_invested_amount_in_the_all_assets_pie_above_the_portfolio_breakdown', () => {
    setMock({ breakdown: BREAKDOWN })
    render(<BrokerBreakdownCharts />)
    const tables = screen.getAllByRole('table')
    expect(tables[0]).toHaveAccessibleName('All assets')
    expect(tables[1]).toHaveAccessibleName('Portfolio breakdown')
    const assetCount = BREAKDOWN.reduce((count, portfolio) => count + portfolio.assets.length, 0)
    expect(within(tables[0]).getAllByRole('row').slice(1)).toHaveLength(assetCount)
  })

  it('merges_an_asset_held_in_two_portfolios_into_one_all_assets_slice', () => {
    setMock({
      breakdown: [
        { portfolioName: 'A', totalInvested: 100, assets: [{ assetName: 'SAME11', totalInvested: 100 }] },
        { portfolioName: 'B', totalInvested: 50, assets: [{ assetName: 'SAME11', totalInvested: 50 }] },
      ],
    })
    render(<BrokerBreakdownCharts />)
    const rows = within(screen.getByRole('table', { name: 'All assets' })).getAllByRole('row').slice(1)
    expect(rows).toHaveLength(1)
    expect(within(rows[0]).getByText(/150[.,]00/)).toBeInTheDocument()
  })

  it('lists_each_portfolio_with_total_invested_and_share_in_the_overview_table', () => {
    setMock({ breakdown: BREAKDOWN })
    render(<BrokerBreakdownCharts />)
    const table = screen.getByRole('table', { name: 'Portfolio breakdown' })
    const headers = within(table).getAllByRole('columnheader').map((header) => header.textContent)
    expect(headers).toEqual(['Label', 'Total invested', 'Percentage'])
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
    expect(container.firstElementChild).toBeEmptyDOMElement()
  })
})
