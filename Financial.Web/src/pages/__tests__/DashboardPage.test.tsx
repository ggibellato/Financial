import React from 'react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AllocationBreakdownDto, PortfolioDashboardDto } from '../../api/types'
import DashboardPage from '../DashboardPage'
import Sidebar from '../../components/Sidebar'
import { NAV_TREE } from '../../navigation/navTree'

const { getDashboardMock, getAllocationBreakdownMock } = vi.hoisted(() => ({
  getDashboardMock: vi.fn<FinancialApiClient['getDashboard']>(),
  getAllocationBreakdownMock: vi.fn<FinancialApiClient['getAllocationBreakdown']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getDashboard: getDashboardMock,
    getAllocationBreakdown: getAllocationBreakdownMock,
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

const renderDashboardRoute = () =>
  render(
    <MemoryRouter initialEntries={['/investments/dashboard']}>
      <Sidebar />
      <Routes>
        <Route path="/investments/dashboard" element={<DashboardPage />} />
      </Routes>
    </MemoryRouter>,
  )

describe('DashboardPage', () => {
  beforeEach(() => {
    getDashboardMock.mockReset()
    getDashboardMock.mockResolvedValue(SUMMARY)
    getAllocationBreakdownMock.mockReset()
    getAllocationBreakdownMock.mockResolvedValue(BREAKDOWN)
  })

  afterEach(() => {
    localStorage.clear()
  })

  it('renders_the_dashboard_route_with_the_kpi_panel', () => {
    renderDashboardRoute()

    expect(screen.getByRole('heading', { name: 'Dashboard', level: 2 })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Portfolio Summary' })).toBeInTheDocument()
  })

  it('is_reachable_from_the_sidebar_as_the_first_investments_entry', () => {
    renderDashboardRoute()

    const investments = NAV_TREE.find((category) => category.id === 'investments')!
    expect(investments.children[0]).toEqual({
      id: 'dashboard',
      label: 'Dashboard',
      route: '/investments/dashboard',
    })
    expect(screen.getByRole('link', { name: 'Dashboard' })).toHaveAttribute('href', '/investments/dashboard')
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
    expect(headings).toEqual(['Portfolio Summary', 'Allocation Breakdown'])
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
})
