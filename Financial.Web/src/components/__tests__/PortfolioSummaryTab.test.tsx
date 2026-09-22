import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AggregatedSummaryData } from '../../hooks/useAggregatedSummary'
import type { AggregatedSummaryDto, InvestmentScope } from '../../api/types'
import { SelectedNodeProvider } from '../../context/SelectedNodeContext'
import PortfolioSummaryTab from '../PortfolioSummaryTab'

function renderComponent(scope: InvestmentScope = 'active') {
  return render(
    <SelectedNodeProvider scope={scope}>
      <PortfolioSummaryTab />
    </SelectedNodeProvider>,
  )
}

const mockAggregatedRetry = vi.fn()

const mockAggregatedHookValue: AggregatedSummaryData = {
  summary: null,
  isLoading: false,
  error: null,
  retry: mockAggregatedRetry,
}

vi.mock('../../hooks/useAggregatedSummary', () => ({
  useAggregatedSummary: () => mockAggregatedHookValue,
}))

const SUMMARY: AggregatedSummaryDto = {
  totalBought: 15420.5,
  totalSold: 3200.0,
  totalCredits: 842.3,
  totalInvested: 12220.5,
  marketValue: 18000.0,
  holdingCount: 3,
  unvaluedHoldingCount: 0,
  priceOnlyReturn: 0.08,
  totalReturn: 0.1,
  totalReturnNetOfTax: 0.09,
  reportingCurrency: 'GBP',
  isReportingCurrencyEnabled: true,
  convertedMarketValue: null,
  convertedInvested: null,
  convertedUnrealisedGainLoss: null,
  convertedTotalReturn: null,
  convertedTotalReturnNetOfTax: null,
  isPartial: false,
  isReportingCurrencyUnavailable: false,
}

function setAggregatedMock(overrides: Partial<AggregatedSummaryData>) {
  Object.assign(mockAggregatedHookValue, overrides)
}

describe('PortfolioSummaryTab', () => {
  beforeEach(() => {
    mockAggregatedRetry.mockReset()
    Object.assign(mockAggregatedHookValue, {
      summary: null,
      isLoading: false,
      error: null,
    })
  })

  it('renders_loading_state_in_totals_section_while_aggregated_summary_loads', () => {
    setAggregatedMock({ isLoading: true })
    renderComponent()
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_in_totals_section_on_aggregated_summary_failure', () => {
    setAggregatedMock({ error: 'Unable to load summary' })
    renderComponent()
    expect(screen.getByText('Unable to load summary')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders_total_invested_for_portfolio_node_selection', () => {
    setAggregatedMock({ summary: SUMMARY })
    renderComponent()
    const labels = screen.getAllByText(/^Total (Bought|Sold|Credits|Invested)$/, { selector: 'span.aggregated-summary__label' })
    expect(labels.map((el) => el.textContent)).toEqual(['Total Bought', 'Total Sold', 'Total Credits', 'Total Invested'])
  })

  it('does_not_render_the_assets_grid_or_footer', () => {
    setAggregatedMock({ summary: SUMMARY })
    renderComponent()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    expect(document.querySelector('.portfolio-holdings__footer')).toBeNull()
  })

  it('does_not_render_share_notice_when_fully_valued', () => {
    setAggregatedMock({ summary: SUMMARY })
    renderComponent()
    expect(document.querySelector('.portfolio-summary__share-notice')).toBeNull()
  })

  it('renders_share_notice_when_portfolio_partially_valued', () => {
    setAggregatedMock({ summary: { ...SUMMARY, holdingCount: 4, unvaluedHoldingCount: 1 } })
    renderComponent()
    expect(document.querySelector('.portfolio-summary__share-notice')).toHaveTextContent(
      'Portfolio shares also do not total 100% for the same reason.',
    )
  })

  it('renders_share_notice_for_nothing_valuable_distinct_from_partial', () => {
    setAggregatedMock({ summary: { ...SUMMARY, holdingCount: 2, unvaluedHoldingCount: 2, marketValue: null } })
    renderComponent()
    expect(document.querySelector('.portfolio-summary__share-notice')).toHaveTextContent(
      'No portfolio share can be computed for the same reason.',
    )
  })

  it('does_not_render_share_notice_in_historic_scope', () => {
    setAggregatedMock({ summary: { ...SUMMARY, holdingCount: 2, unvaluedHoldingCount: 2, marketValue: null } })
    renderComponent('historic')
    expect(document.querySelector('.portfolio-summary__share-notice')).toBeNull()
  })
})
