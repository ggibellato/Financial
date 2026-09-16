import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen } from '../../test/renderWithFluent'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { PortfolioDashboardDto } from '../../api/types'
import DashboardPage from '../DashboardPage'
import Sidebar from '../../components/Sidebar'
import { NAV_TREE } from '../../navigation/navTree'

const { getDashboardMock } = vi.hoisted(() => ({
  getDashboardMock: vi.fn<FinancialApiClient['getDashboard']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getDashboard: getDashboardMock,
  } as Partial<FinancialApiClient>,
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
  })

  afterEach(() => {
    localStorage.clear()
  })

  it('renders_the_dashboard_route_with_its_four_panels', () => {
    renderDashboardRoute()

    expect(screen.getByRole('heading', { name: 'Dashboard', level: 2 })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Portfolio Summary' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Allocation Breakdown' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Data-Quality Warnings' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Upcoming Income' })).toBeInTheDocument()
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
})
