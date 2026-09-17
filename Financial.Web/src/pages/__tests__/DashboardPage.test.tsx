import React from 'react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, waitFor } from '../../test/renderWithFluent'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type {
  AllocationBreakdownDto,
  DataQualityReportDto,
  PortfolioDashboardDto,
  TreeNodeDto,
  UpcomingIncomeDto,
} from '../../api/types'
import DashboardPage from '../DashboardPage'
import Sidebar from '../../components/Sidebar'
import { NAV_TREE } from '../../navigation/navTree'

const {
  getDashboardMock,
  getAllocationBreakdownMock,
  getDataQualityReportMock,
  getUpcomingIncomeMock,
  getNavigationTreeMock,
} = vi.hoisted(() => ({
  getDashboardMock: vi.fn<FinancialApiClient['getDashboard']>(),
  getAllocationBreakdownMock: vi.fn<FinancialApiClient['getAllocationBreakdown']>(),
  getDataQualityReportMock: vi.fn<FinancialApiClient['getDataQualityReport']>(),
  getUpcomingIncomeMock: vi.fn<FinancialApiClient['getUpcomingIncome']>(),
  getNavigationTreeMock: vi.fn<FinancialApiClient['getNavigationTree']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getDashboard: getDashboardMock,
    getAllocationBreakdown: getAllocationBreakdownMock,
    getDataQualityReport: getDataQualityReportMock,
    getUpcomingIncome: getUpcomingIncomeMock,
    getNavigationTree: getNavigationTreeMock,
  } as Partial<FinancialApiClient>,
}))

vi.mock('recharts', () => ({
  PieChart: ({ children }: { children: React.ReactNode }) => <div data-testid="pie-chart">{children}</div>,
  Pie: ({ children }: { children?: React.ReactNode }) => <div data-testid="pie">{children}</div>,
  Cell: () => null,
  Tooltip: () => null,
  Legend: () => null,
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}))

const SUMMARY: PortfolioDashboardDto = {
  marketValue: 18000,
  invested: 12220.5,
  unrealisedGainLoss: 5779.5,
  realisedGainLoss: 1200,
  incomeYtd: 340.25,
  incomeLifetime: 2840.75,
  grossXirr: 0.1234,
  netXirr: 0.0987,
  isPartial: false,
  unvaluedHoldingCount: 0,
  reportingCurrency: 'GBP',
  isReportingCurrencyEnabled: false,
  isReportingCurrencyPartial: false,
  isReportingCurrencyUnavailable: false,
  convertedMarketValue: null,
  convertedInvested: null,
  convertedUnrealisedGainLoss: null,
  convertedRealisedGainLoss: null,
  convertedIncomeYtd: null,
  convertedIncomeLifetime: null,
  convertedGrossXirr: null,
  convertedNetXirr: null,
}

const BREAKDOWN: AllocationBreakdownDto = {
  byClass: [
    { class: 'Equity', marketValue: 12000, percentage: 60 },
    { class: 'Bond', marketValue: 8000, percentage: 40 },
  ],
  byCurrency: [{ currency: 'GBP', marketValue: 20000, percentage: 100 }],
  byCountry: [{ country: 'UK', marketValue: 20000, percentage: 100 }],
  byBroker: [{ brokerName: 'Trading212', marketValue: 20000, percentage: 100 }],
}

const REPORT: DataQualityReportDto = {
  salesExceedPurchases: [],
  unpricedOpenHoldings: [{ brokerName: 'Trading212', portfolioName: 'ISA', assetName: 'VUSA' }],
  openHoldingsMissingCostBasis: [],
  unresolvedTaxClassifications: [
    { brokerName: 'XPI', portfolioName: 'Acoes', assetName: 'KLBN4', taxYear: '2024/25', eventCategory: 'Dividend' },
  ],
  staleValuationCount: 0,
  historicHoldingsStillOpen: [],
  unclassifiedHoldings: [],
  unclassifiedAndUnpricedOpenHoldings: [],
}

function makeTree(brokerName: string, portfolioName: string, assetName: string): TreeNodeDto {
  return {
    nodeType: 'Investments',
    displayName: 'Investments',
    metadata: {},
    children: [
      {
        nodeType: 'Broker',
        displayName: brokerName,
        metadata: { BrokerName: brokerName },
        children: [
          {
            nodeType: 'Portfolio',
            displayName: portfolioName,
            metadata: { PortfolioName: portfolioName },
            children: [
              { nodeType: 'Asset', displayName: assetName, metadata: { AssetName: assetName }, children: [] },
            ],
          },
        ],
      },
    ],
  }
}

function inDays(days: number): string {
  const date = new Date()
  date.setDate(date.getDate() + days)
  return date.toISOString()
}

const UPCOMING_INCOME: UpcomingIncomeDto[] = [
  {
    assetName: 'VHYL',
    brokerName: 'Freetrade',
    lastCreditDate: inDays(-80),
    projectedAmount: 42.5,
    projectedNextDate: inDays(10),
  },
  {
    assetName: 'ITSA4',
    brokerName: 'Clear',
    lastCreditDate: inDays(30),
    projectedAmount: 96,
    projectedNextDate: inDays(150),
  },
]

const EMPTY_TREE: TreeNodeDto ={ nodeType: 'Investments', displayName: 'Investments', metadata: {}, children: [] }

function LocationProbe() {
  const location = useLocation()
  return (
    <div data-testid="location">
      {location.pathname}|{JSON.stringify(location.state)}
    </div>
  )
}

const renderDashboardRoute = () =>
  render(
    <MemoryRouter initialEntries={['/investments/dashboard']}>
      <Sidebar />
      <LocationProbe />
      <Routes>
        <Route path="/investments/dashboard" element={<DashboardPage />} />
        <Route path="/investments/active-investments" element={<div>Active Investments tree</div>} />
        <Route path="/investments/historic-investments" element={<div>Historic Investments tree</div>} />
      </Routes>
    </MemoryRouter>,
  )

describe('DashboardPage', () => {
  beforeEach(() => {
    getDashboardMock.mockReset()
    getDashboardMock.mockResolvedValue(SUMMARY)
    getAllocationBreakdownMock.mockReset()
    getAllocationBreakdownMock.mockResolvedValue(BREAKDOWN)
    getDataQualityReportMock.mockReset()
    getDataQualityReportMock.mockResolvedValue(REPORT)
    getUpcomingIncomeMock.mockReset()
    getUpcomingIncomeMock.mockResolvedValue(UPCOMING_INCOME)
    getNavigationTreeMock.mockReset()
    getNavigationTreeMock.mockResolvedValue(EMPTY_TREE)
    Element.prototype.scrollIntoView = vi.fn()
  })

  afterEach(() => {
    localStorage.clear()
  })

  it('renders_the_dashboard_route_with_the_kpi_panel', () => {
    renderDashboardRoute()

    expect(screen.getByRole('heading', { name: 'Dashboard', level: 2 })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Portfolio Summary' })).toBeInTheDocument()
  })

  it('P52-F05-react-portfolio-dashboard-01: a Dashboard nav entry appears first under Investments and opens the new page', () => {
    renderDashboardRoute()

    const investments = NAV_TREE.find((category) => category.id === 'investments')!
    expect(investments.children[0]).toEqual({
      id: 'dashboard',
      label: 'Dashboard',
      route: '/investments/dashboard',
    })
    expect(screen.getByRole('link', { name: 'Dashboard' })).toHaveAttribute('href', '/investments/dashboard')
    expect(screen.getByRole('heading', { name: 'Dashboard', level: 2 })).toBeInTheDocument()
  })

  it('renders_the_kpi_tiles_from_the_dashboard_endpoint', async () => {
    renderDashboardRoute()

    expect(await screen.findByText('Market Value')).toBeInTheDocument()
    expect(screen.getByText('Net XIRR (of Tax)')).toBeInTheDocument()
    expect(getDashboardMock).toHaveBeenCalledTimes(1)
  })

  it('renders_the_allocation_breakdown_panel_below_the_kpi_panel', async () => {
    renderDashboardRoute()

    expect(await screen.findByRole('heading', { name: 'Allocation Breakdown' })).toBeInTheDocument()
    expect(await screen.findByRole('tab', { name: 'Class' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByText('Equity')).toBeInTheDocument()
    expect(getAllocationBreakdownMock).toHaveBeenCalledTimes(1)

    const headings = screen.getAllByRole('heading', { level: 3 }).map((heading) => heading.textContent)
    expect(headings).toEqual(['Portfolio Summary', 'Allocation Breakdown', 'Data Quality Warnings', 'Upcoming Income'])
  })

  it('keeps_the_kpi_panel_intact_when_only_the_allocation_request_fails', async () => {
    getAllocationBreakdownMock.mockRejectedValue(new Error('Allocation service unavailable'))

    renderDashboardRoute()

    expect(await screen.findByText('Allocation service unavailable')).toBeInTheDocument()
    expect(screen.getByText('Market Value')).toBeInTheDocument()
    expect(screen.queryByRole('tab', { name: 'Class' })).not.toBeInTheDocument()
  })

  it('switching_allocation_dimension_does_not_re_fetch_the_breakdown', async () => {
    renderDashboardRoute()

    fireEvent.click(await screen.findByRole('tab', { name: 'Broker' }))

    expect(screen.getByText('Trading212')).toBeInTheDocument()
    expect(getAllocationBreakdownMock).toHaveBeenCalledTimes(1)
  })
  it('renders_the_data_quality_warnings_panel_from_the_report_endpoint', async () => {
    renderDashboardRoute()

    expect(await screen.findByRole('heading', { name: 'Data Quality Warnings' })).toBeInTheDocument()
    expect(await screen.findByText('Missing price (1)')).toBeInTheDocument()
    expect(getDataQualityReportMock).toHaveBeenCalledTimes(1)
  })

  it('keeps_the_other_panels_intact_when_only_the_data_quality_request_fails', async () => {
    getDataQualityReportMock.mockRejectedValue(new Error('Data quality service unavailable'))

    renderDashboardRoute()

    expect(await screen.findByText('Data quality service unavailable')).toBeInTheDocument()
    expect(screen.getByText('Net XIRR (of Tax)')).toBeInTheDocument()
    expect(await screen.findByRole('tab', { name: 'Class' })).toBeInTheDocument()
  })

  it('clicking_a_warning_holding_navigates_to_the_active_investments_tree', async () => {
    getNavigationTreeMock.mockResolvedValue(makeTree('Trading212', 'ISA', 'VUSA'))

    renderDashboardRoute()

    fireEvent.click(await screen.findByText('Missing price (1)'))
    fireEvent.click(screen.getByText('VUSA'))

    expect(await screen.findByText('Active Investments tree')).toBeInTheDocument()
    expect(screen.getByTestId('location')).toHaveTextContent(
      '/investments/active-investments|{"pendingSelection":{"brokerName":"Trading212","portfolioName":"ISA","assetName":"VUSA"}}',
    )
  })

  it('clicking_a_historic_only_warning_holding_navigates_to_the_historic_investments_tree', async () => {
    getNavigationTreeMock.mockImplementation((scope) =>
      Promise.resolve(scope === 'historic' ? makeTree('XPI', 'Acoes', 'KLBN4') : EMPTY_TREE),
    )

    renderDashboardRoute()

    fireEvent.click(await screen.findByText('Unresolved tax classification (1)'))
    fireEvent.click(screen.getByText('KLBN4'))

    expect(await screen.findByText('Historic Investments tree')).toBeInTheDocument()
  })

  it('shows_an_inline_warning_when_the_holding_is_in_neither_tree', async () => {
    renderDashboardRoute()

    fireEvent.click(await screen.findByText('Missing price (1)'))
    fireEvent.click(screen.getByText('VUSA'))

    expect(
      await screen.findByText(
        'Unable to locate VUSA — it may have moved or been archived since this report was generated.',
      ),
    ).toBeInTheDocument()
    expect(screen.getByTestId('location')).toHaveTextContent('/investments/dashboard|')
  })

  it('the_partial_notice_cross_link_expands_and_scrolls_to_the_missing_price_category', async () => {
    getDashboardMock.mockResolvedValue({ ...SUMMARY, isPartial: true, unvaluedHoldingCount: 1 })

    renderDashboardRoute()

    fireEvent.click(await screen.findByRole('button', { name: 'View affected holdings' }))

    await waitFor(() => expect(screen.getByText('VUSA')).toBeInTheDocument())
    expect(Element.prototype.scrollIntoView).toHaveBeenCalledWith({ behavior: 'smooth', block: 'start' })
  })

  it('renders_the_upcoming_income_panel_from_the_upcoming_income_endpoint', async () => {
    renderDashboardRoute()

    expect(await screen.findByRole('heading', { name: 'Upcoming Income' })).toBeInTheDocument()
    expect(await screen.findByRole('tab', { name: '90 days' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('table', { name: 'Upcoming income' })).toBeInTheDocument()
    expect(getUpcomingIncomeMock).toHaveBeenCalledTimes(1)
  })

  it('P52-F04-upcoming-income-03: switching the income window re-filters the held list without re-fetching it', async () => {
    renderDashboardRoute()

    expect(await screen.findByText('VHYL')).toBeInTheDocument()
    expect(screen.queryByText('ITSA4')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('tab', { name: '180 days' }))

    expect(screen.getByText('ITSA4')).toBeInTheDocument()
    expect(getUpcomingIncomeMock).toHaveBeenCalledTimes(1)
  })

  it('P52-F05-react-portfolio-dashboard-02: all four panels render with their own independent states', async () => {
    getUpcomingIncomeMock.mockRejectedValue(new Error('Income service unavailable'))

    renderDashboardRoute()

    expect(await screen.findByText('Income service unavailable')).toBeInTheDocument()
    expect(screen.getByText('Net XIRR (of Tax)')).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Class' })).toBeInTheDocument()
    expect(screen.getByText('Missing price (1)')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Retry' })).not.toBeInTheDocument()
  })

  it('P52-F05-react-portfolio-dashboard-03: a page-level error state with a single Retry action shows only when every panel request fails', async () => {
    getDashboardMock.mockRejectedValue(new Error('Dashboard unavailable'))
    getAllocationBreakdownMock.mockRejectedValue(new Error('Allocation unavailable'))
    getDataQualityReportMock.mockRejectedValue(new Error('Data quality unavailable'))
    getUpcomingIncomeMock.mockRejectedValue(new Error('Income unavailable'))

    renderDashboardRoute()

    expect(
      await screen.findByText('Unable to load the dashboard — none of its data could be retrieved.'),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Portfolio Summary' })).not.toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Upcoming Income' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument()
  })

  it('P52-F05-react-portfolio-dashboard-03: the page-level Retry re-issues all four requests and restores the panels', async () => {
    getDashboardMock.mockRejectedValueOnce(new Error('Dashboard unavailable')).mockResolvedValue(SUMMARY)
    getAllocationBreakdownMock.mockRejectedValueOnce(new Error('Allocation unavailable')).mockResolvedValue(BREAKDOWN)
    getDataQualityReportMock.mockRejectedValueOnce(new Error('Data quality unavailable')).mockResolvedValue(REPORT)
    getUpcomingIncomeMock.mockRejectedValueOnce(new Error('Income unavailable')).mockResolvedValue(UPCOMING_INCOME)

    renderDashboardRoute()

    fireEvent.click(await screen.findByRole('button', { name: 'Retry' }))

    expect(await screen.findByRole('heading', { name: 'Upcoming Income' })).toBeInTheDocument()
    expect(getDashboardMock).toHaveBeenCalledTimes(2)
    expect(getAllocationBreakdownMock).toHaveBeenCalledTimes(2)
    expect(getDataQualityReportMock).toHaveBeenCalledTimes(2)
    expect(getUpcomingIncomeMock).toHaveBeenCalledTimes(2)
    // Focus only moves once every panel has finished loading (not merely stopped failing), so wait
    // for the last one to settle rather than asserting the instant the first panel's content lands.
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Dashboard', level: 2 })).toHaveFocus())
  })

  it('P52-F05-react-portfolio-dashboard-03: focus stays on Retry, not the page heading, when the retry does not recover any panel', async () => {
    getDashboardMock.mockRejectedValue(new Error('Dashboard unavailable'))
    getAllocationBreakdownMock.mockRejectedValue(new Error('Allocation unavailable'))
    getDataQualityReportMock.mockRejectedValue(new Error('Data quality unavailable'))
    getUpcomingIncomeMock.mockRejectedValue(new Error('Income unavailable'))

    renderDashboardRoute()

    const retryButton = await screen.findByRole('button', { name: 'Retry' })
    fireEvent.click(retryButton)

    await waitFor(() => expect(getDashboardMock).toHaveBeenCalledTimes(2))
    expect(
      await screen.findByText('Unable to load the dashboard — none of its data could be retrieved.'),
    ).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Dashboard', level: 2 })).not.toHaveFocus()
  })
})
