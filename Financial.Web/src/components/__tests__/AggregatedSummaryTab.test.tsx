import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AggregatedSummaryData } from '../../hooks/useAggregatedSummary'
import type { AggregatedSummaryDto, SelectedNode } from '../../api/types'
import { SelectedNodeProvider } from '../../context/SelectedNodeContext'
import { createSelectedNodeWrapper } from '../../test-utils/selectedNodeTestWrapper'
import AggregatedSummaryTab from '../AggregatedSummaryTab'

vi.mock('../BrokerBreakdownCharts', () => ({
  default: () => <div data-testid="broker-breakdown-charts" />,
}))

function renderComponent() {
  return render(
    <SelectedNodeProvider>
      <AggregatedSummaryTab />
    </SelectedNodeProvider>,
  )
}

function renderComponentWithNode(node: SelectedNode) {
  const { wrapper, setNode } = createSelectedNodeWrapper()
  const result = render(<AggregatedSummaryTab />, { wrapper })
  setNode(node)
  return result
}

const mockRetry = vi.fn()

const mockHookValue: AggregatedSummaryData = {
  summary: null,
  isLoading: false,
  error: null,
  retry: mockRetry,
}

vi.mock('../../hooks/useAggregatedSummary', () => ({
  useAggregatedSummary: () => mockHookValue,
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
}

function setMock(overrides: Partial<AggregatedSummaryData>) {
  Object.assign(mockHookValue, overrides)
}

describe('AggregatedSummaryTab', () => {
  beforeEach(() => {
    mockRetry.mockReset()
    Object.assign(mockHookValue, {
      summary: null,
      isLoading: false,
      error: null,
    })
  })

  it('renders_loading_indicator_while_data_loads', () => {
    setMock({ isLoading: true })
    renderComponent()
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry_on_failure', () => {
    setMock({ error: 'Unable to load summary' })
    renderComponent()
    expect(screen.getByText('Unable to load summary')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders_total_bought_in_green', () => {
    setMock({ summary: SUMMARY })
    renderComponent()
    const label = screen.getByText('Total Bought')
    const valueEl = label.nextElementSibling
    expect(valueEl).toHaveClass('aggregated-summary__value--green')
  })

  it('renders_total_sold_in_red', () => {
    setMock({ summary: SUMMARY })
    renderComponent()
    const label = screen.getByText('Total Sold')
    const valueEl = label.nextElementSibling
    expect(valueEl).toHaveClass('aggregated-summary__value--red')
  })

  it('renders_total_credits_in_blue', () => {
    setMock({ summary: SUMMARY })
    renderComponent()
    const label = screen.getByText('Total Credits')
    const valueEl = label.nextElementSibling
    expect(valueEl).toHaveClass('aggregated-summary__value--blue')
  })

  it('renders_total_invested_after_total_credits', () => {
    setMock({ summary: SUMMARY })
    renderComponent()
    const labels = screen.getAllByText(/^Total /).map((el) => el.textContent)
    expect(labels).toEqual(['Total Bought', 'Total Sold', 'Total Credits', 'Total Invested', 'Total Return'])
  })

  it('renders_market_value', () => {
    setMock({ summary: SUMMARY })
    renderComponent()
    const label = screen.getByText('Market Value')
    expect(label.nextElementSibling?.textContent).toMatch(/18[.,]000[.,]00/)
  })

  it('renders_dash_for_unavailable_market_value', () => {
    setMock({ summary: { ...SUMMARY, marketValue: null, unvaluedHoldingCount: 3 } })
    renderComponent()
    const label = screen.getByText('Market Value')
    expect(label.nextElementSibling?.textContent).toBe('—')
  })

  it('renders_price_only_and_total_return_as_percentages', () => {
    setMock({ summary: SUMMARY })
    renderComponent()
    expect(screen.getByText('Price-Only Return').nextElementSibling?.textContent).toBe('8.00%')
    expect(screen.getByText('Total Return').nextElementSibling?.textContent).toBe('10.00%')
  })

  it('renders_dash_for_withheld_returns', () => {
    setMock({ summary: { ...SUMMARY, priceOnlyReturn: null, totalReturn: null, unvaluedHoldingCount: 1 } })
    renderComponent()
    expect(screen.getByText('Price-Only Return').nextElementSibling?.textContent).toBe('—')
    expect(screen.getByText('Total Return').nextElementSibling?.textContent).toBe('—')
  })

  it('does_not_render_incomplete_notice_when_fully_valued', () => {
    setMock({ summary: SUMMARY })
    renderComponent()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('renders_incomplete_notice_naming_shortfall_when_partially_valued', () => {
    setMock({ summary: { ...SUMMARY, holdingCount: 5, unvaluedHoldingCount: 2, priceOnlyReturn: null, totalReturn: null } })
    renderComponent()
    expect(screen.getByRole('status')).toHaveTextContent(
      '2 of 5 holdings could not be valued; the total is incomplete and returns are withheld.',
    )
  })

  it('renders_incomplete_notice_for_nothing_valuable_distinct_from_partial', () => {
    setMock({ summary: { ...SUMMARY, holdingCount: 4, unvaluedHoldingCount: 4, marketValue: null, priceOnlyReturn: null, totalReturn: null } })
    renderComponent()
    expect(screen.getByRole('status')).toHaveTextContent('None of the 4 holdings could be valued; returns are withheld.')
  })

  it('renders_total_invested_in_green_when_non_negative', () => {
    setMock({ summary: { ...SUMMARY, totalInvested: 0 } })
    renderComponent()
    const label = screen.getByText('Total Invested')
    const valueEl = label.nextElementSibling
    expect(valueEl).toHaveClass('aggregated-summary__value--green')
  })

  it('renders_total_invested_in_red_when_negative', () => {
    setMock({ summary: { ...SUMMARY, totalInvested: -125.5 } })
    renderComponent()
    const label = screen.getByText('Total Invested')
    const valueEl = label.nextElementSibling
    expect(valueEl).toHaveClass('aggregated-summary__value--red')
  })

  it('renders_values_formatted_to_two_decimal_places', () => {
    setMock({
      summary: {
        totalBought: 15420.5678,
        totalSold: 3200.1234,
        totalCredits: 842.9999,
        totalInvested: 12220.5678,
        marketValue: 18000.1234,
        holdingCount: 3,
        unvaluedHoldingCount: 0,
        priceOnlyReturn: 0.08,
        totalReturn: 0.1,
      },
    })
    renderComponent()
    const label = screen.getByText('Total Bought')
    expect(label.nextElementSibling?.textContent).toMatch(/\d+[.,]\d{2}$/)
    const investedLabel = screen.getByText('Total Invested')
    expect(investedLabel.nextElementSibling?.textContent).toMatch(/\d+[.,]\d{2}$/)
  })

  it('renders_zero_values_without_error', () => {
    setMock({
      summary: {
        totalBought: 0,
        totalSold: 0,
        totalCredits: 0,
        totalInvested: 0,
        marketValue: 0,
        holdingCount: 0,
        unvaluedHoldingCount: 0,
        priceOnlyReturn: null,
        totalReturn: null,
      },
    })
    renderComponent()
    expect(screen.getByText('Total Bought')).toBeInTheDocument()
    expect(screen.getByText('Total Sold')).toBeInTheDocument()
    expect(screen.getByText('Total Credits')).toBeInTheDocument()
    expect(screen.getByText('Total Invested')).toBeInTheDocument()
  })

  it('renders_broker_breakdown_charts_for_broker_node_selection', () => {
    setMock({ summary: SUMMARY })
    renderComponentWithNode({ nodeType: 'Broker', brokerName: 'XPI', currency: 'BRL' })
    expect(screen.getByTestId('broker-breakdown-charts')).toBeInTheDocument()
  })

  it('does_not_render_broker_breakdown_charts_for_portfolio_node_selection', () => {
    setMock({ summary: SUMMARY })
    renderComponentWithNode({ nodeType: 'Portfolio', brokerName: 'XPI', portfolioName: 'Acoes' })
    expect(screen.queryByTestId('broker-breakdown-charts')).not.toBeInTheDocument()
  })
})
