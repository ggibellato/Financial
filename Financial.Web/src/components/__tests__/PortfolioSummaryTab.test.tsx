import { fireEvent, render, screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AggregatedSummaryData } from '../../hooks/useAggregatedSummary'
import type { PortfolioAssetSummaryData, RowPriceState } from '../../hooks/usePortfolioAssetSummary'
import type { AggregatedSummaryDto, InvestmentScope, PortfolioAssetSummaryItemDto } from '../../api/types'
import { SelectedNodeProvider } from '../../context/SelectedNodeContext'
import { formatN2 } from '../../utils/formatters'
import PortfolioSummaryTab from '../PortfolioSummaryTab'

function renderComponent(scope: InvestmentScope = 'active') {
  return render(
    <SelectedNodeProvider scope={scope}>
      <PortfolioSummaryTab />
    </SelectedNodeProvider>,
  )
}

const mockAggregatedRetry = vi.fn()
const mockPortfolioRetry = vi.fn()

const mockAggregatedHookValue: AggregatedSummaryData = {
  summary: null,
  isLoading: false,
  error: null,
  retry: mockAggregatedRetry,
}

const mockPortfolioHookValue: PortfolioAssetSummaryData = {
  items: null,
  rowPrices: [],
  isLoading: false,
  error: null,
  retry: mockPortfolioRetry,
}

vi.mock('../../hooks/useAggregatedSummary', () => ({
  useAggregatedSummary: () => mockAggregatedHookValue,
}))

vi.mock('../../hooks/usePortfolioAssetSummary', () => ({
  usePortfolioAssetSummary: () => mockPortfolioHookValue,
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
}

const ITEM_1: PortfolioAssetSummaryItemDto = {
  assetName: 'ALZR11',
  ticker: 'ALZR11',
  exchange: 'BVMF',
  class: 'RealEstate',
  firstInvestmentDate: '2021-03-01T00:00:00',
  currentQuantity: 25,
  averagePrice: 100,
  averageSellPrice: null,
  totalBought: 2500,
  totalSold: 0,
  totalInvested: 2500,
  realizedGainLoss: 0,
  portfolioWeight: 23.4,
  marketValue: null,
  costOfUnitsHeld: 2500,
  unrealisedGain: null,
  priceAsOfDate: null,
  isPriceStale: false,
  priceOnlyReturn: null,
  totalReturn: null,
  totalReturnNetOfTax: null,
  totalCredits: 0,
  cashFlows: [],
  lastMonthCredits: 0,
  lastCreditMonth: null,
  lastMonthCreditsPercent: null,
  creditFrequencyPerYear: null,
  estimatedAnnualCredits: null,
  estimatedAnnualPercent: null,
  currentMonthCredits: 0,
}

const LOADING_ROW_PRICE: RowPriceState = { isLoading: true, currentPrice: null, fetchFailed: false, isManual: false }
const FAILED_ROW_PRICE: RowPriceState = { isLoading: false, currentPrice: null, fetchFailed: true, isManual: false }
const IDLE_ROW_PRICE: RowPriceState = { isLoading: false, currentPrice: null, fetchFailed: false, isManual: false }

function setAggregatedMock(overrides: Partial<AggregatedSummaryData>) {
  Object.assign(mockAggregatedHookValue, overrides)
}

function setPortfolioMock(overrides: Partial<PortfolioAssetSummaryData>) {
  Object.assign(mockPortfolioHookValue, overrides)
}

describe('PortfolioSummaryTab', () => {
  beforeEach(() => {
    mockAggregatedRetry.mockReset()
    mockPortfolioRetry.mockReset()
    Object.assign(mockAggregatedHookValue, {
      summary: null,
      isLoading: false,
      error: null,
    })
    Object.assign(mockPortfolioHookValue, {
      items: null,
      rowPrices: [],
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

  it('renders_loading_state_in_table_section_while_items_load', () => {
    setPortfolioMock({ isLoading: true })
    renderComponent()
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_in_table_section_on_items_fetch_failure', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ error: 'Unable to load portfolio assets' })
    renderComponent()
    expect(screen.getByText('Unable to load portfolio assets')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
    expect(screen.getByText('Total Bought')).toBeInTheDocument()
  })

  it('renders_table_with_correct_column_headers_for_active_scope', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent('active')
    expect(screen.getByText('Asset Name')).toBeInTheDocument()
    expect(screen.getByText('First Investment')).toBeInTheDocument()
    expect(screen.getByText('Quantity')).toBeInTheDocument()
    expect(screen.getAllByText('Total Invested').length).toBeGreaterThanOrEqual(1)
    expect(screen.queryByText('Realized Gain/Loss')).not.toBeInTheDocument()
    expect(screen.getByText('% Portfolio')).toBeInTheDocument()
    expect(screen.getAllByText('Total Credits').length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText('Current Value')).toBeInTheDocument()
    expect(screen.getByText('Average Price')).toBeInTheDocument()
    expect(screen.getByText('Current Price')).toBeInTheDocument()
    expect(screen.getByText('Profit')).toBeInTheDocument()
    expect(screen.getAllByText('%').length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText('w/ Credits')).toBeInTheDocument()
    expect(screen.getByText('XIRR')).toBeInTheDocument()
  })

  it('renders_table_with_correct_column_headers_for_historic_scope', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent('historic')
    expect(screen.getByText('Realized Gain/Loss')).toBeInTheDocument()
    expect(screen.getByText('Sold Price')).toBeInTheDocument()
    expect(screen.queryByText('Current Value')).not.toBeInTheDocument()
    expect(screen.queryByText('Current Price')).not.toBeInTheDocument()
  })

  it('renders_asset_row_with_correctly_formatted_values', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, totalCredits: 75.5 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText('ALZR11')).toBeInTheDocument()
    expect(screen.getByText('01/03/2021')).toBeInTheDocument()
    expect(screen.getByText(/23\.40%/)).toBeInTheDocument()
    expect(screen.getByText(/75[.,]50/)).toBeInTheDocument()
  })

  it('renders_dash_for_null_portfolio_weight', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, portfolioWeight: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent()
    const row = screen.getByText('ALZR11').closest('tr')!
    const weightCell = within(row).getAllByRole('cell')[3]
    expect(weightCell).toHaveTextContent('—')
  })

  it('renders_portfolio_weight_at_rounding_boundary_matching_wpf_f2', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, portfolioWeight: 23.995 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/24\.00%/)).toBeInTheDocument()
  })

  it('renders_realized_gain_loss_for_historic_asset', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, realizedGainLoss: -50, totalBought: 300, totalSold: 250 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    const { container } = renderComponent('historic')
    expect(container.textContent).toContain('-50')
  })

  it('renders_footer_realized_gain_loss_sum', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, realizedGainLoss: -50 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11', realizedGainLoss: -20 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [IDLE_ROW_PRICE, IDLE_ROW_PRICE] })
    const { container } = renderComponent('historic')
    const input = container.querySelector('[data-label="Realized Gain/Loss"] + input') as HTMLInputElement
    expect(input.value).toBe('-70.00')
  })

  it('renders_sold_price_for_historic_asset', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, averageSellPrice: 115.5 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent('historic')
    expect(screen.getByText(/115[.,]50/)).toBeInTheDocument()
  })

  it('renders_dash_for_sold_price_when_never_sold', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, averageSellPrice: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent('historic')
    // rows[0]/[1] are the two header rows; rows[2] is the single data row.
    const dataRow = screen.getAllByRole('row')[2]
    const cell = within(dataRow).getAllByRole('cell')[8] // "Sold Price" column
    expect(cell.textContent).toBe('—')
  })

  it('computes_historic_profit_percent_from_realized_gain_loss_excluding_credits', () => {
    // capital-gain-only % = (realizedGainLoss - totalCredits) / totalBought x 100
    // = (280 - 30) / 2500 x 100 = 10.00%
    const item: PortfolioAssetSummaryItemDto = {
      ...ITEM_1,
      totalBought: 2500,
      totalCredits: 30,
      realizedGainLoss: 280,
    }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent('historic')
    expect(within(screen.getByRole('table')).getByText(/10[.,]00%/)).toBeInTheDocument()
  })

  it('computes_historic_profit_with_credits_percent_from_full_realized_gain_loss', () => {
    // w/ credits % = realizedGainLoss / totalBought x 100 = 280 / 2500 x 100 = 11.20%
    const item: PortfolioAssetSummaryItemDto = {
      ...ITEM_1,
      totalBought: 2500,
      totalCredits: 30,
      realizedGainLoss: 280,
    }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent('historic')
    expect(screen.getByText(/11[.,]20%/)).toBeInTheDocument()
  })

  it('renders_dash_for_historic_profit_when_total_bought_is_zero', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, totalBought: 0, realizedGainLoss: 0 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent('historic')
    // rows[0]/[1] are the two header rows; rows[2] is the single data row.
    const cells = within(screen.getAllByRole('row')[2]).getAllByRole('cell')
    expect(cells[9].textContent).toBe('—') // "Profit %" column
    expect(cells[10].textContent).toBe('—') // "Profit % w/ Credits" column
  })

  it('renders_historic_xirr_from_the_resolved_row_rate', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, priceOnlyReturn: 0.15 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent('historic')
    expect(screen.getByText(/15[.,]00%/)).toBeInTheDocument()
  })

  it('renders_dash_for_footer_current_value_when_no_row_has_price_data', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [IDLE_ROW_PRICE] })
    const { container } = renderComponent()
    const input = container.querySelector('[data-label="Current Value"] + input') as HTMLInputElement
    expect(input.value).toBe('—')
  })

  it('renders_average_price_with_formatted_value', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, averagePrice: 123.456 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/123[.,]46/)).toBeInTheDocument()
  })

  it('renders_total_credits_immediately_before_price_resolves', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, totalCredits: 75.5 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/75[.,]50/)).toBeInTheDocument()
    const loadingCells = screen.getAllByText('...')
    expect(loadingCells.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_per_cell_loading_indicator_while_price_loads', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const loadingCells = screen.getAllByText('...')
    expect(loadingCells).toHaveLength(5)
  })

  it('renders_current_value_when_price_resolves', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, marketValue: 262.5 }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    expect(screen.getByText(/262[.,]50/)).toBeInTheDocument()
  })

  it('renders_manual_badge_when_row_price_is_manual', () => {
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: true }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [rowPrice] })
    renderComponent()
    expect(screen.getByText('(M)')).toBeInTheDocument()
  })

  it('does_not_render_manual_badge_when_row_price_is_not_manual', () => {
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [rowPrice] })
    renderComponent()
    expect(screen.queryByText('(M)')).not.toBeInTheDocument()
  })

  it('renders_stale_badge_when_row_price_is_stale', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, isPriceStale: true }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    expect(screen.getByText('(S)')).toBeInTheDocument()
  })

  it('does_not_render_stale_badge_when_row_price_is_not_stale', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, isPriceStale: false }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    expect(screen.queryByText('(S)')).not.toBeInTheDocument()
  })

  it('does_not_render_stale_badge_in_historic_scope_even_when_flagged_stale', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, isPriceStale: true }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent('historic')
    expect(screen.queryByText('(S)')).not.toBeInTheDocument()
  })

  it('renders_current_price_when_price_resolves', () => {
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [rowPrice] })
    renderComponent()
    expect(screen.getByText(/10[.,]50/)).toBeInTheDocument()
  })

  it('renders_dash_in_current_price_when_price_fetch_fails', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [FAILED_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(5)
  })

  it('renders_correct_profit_percent', () => {
    // costOfUnitsHeld = 250; unrealisedGain = currentValue(262.50) - costOfUnitsHeld = 12.50
    const item: PortfolioAssetSummaryItemDto = {
      ...ITEM_1,
      currentQuantity: 25,
      averagePrice: 10,
      totalInvested: 250,
      costOfUnitsHeld: 250,
      unrealisedGain: 12.5,
    }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    // Both % Profit and % Profit w/ Credits show 5.00% when totalCredits is 0
    const profitElements = screen.getAllByText(/5[.,]00%/)
    expect(profitElements.length).toBeGreaterThanOrEqual(1)
    expect(profitElements[0]).toHaveClass('portfolio-summary__profit--green')
  })

  it('renders_correct_profit_with_credits_percent', () => {
    // unrealisedGain = currentValue(262.50) - costOfUnitsHeld(250) = 12.50
    // profitWithCreditsPercent = (12.50 + 12.50) / 250 * 100 = 10.00%
    const item: PortfolioAssetSummaryItemDto = {
      ...ITEM_1,
      currentQuantity: 25,
      averagePrice: 10,
      totalInvested: 250,
      costOfUnitsHeld: 250,
      unrealisedGain: 12.5,
      totalCredits: 12.5,
    }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    expect(within(screen.getByRole('table')).getByText(/10[.,]00%/)).toBeInTheDocument()
  })

  it('renders_xirr_when_the_row_rate_resolves', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, priceOnlyReturn: 0.1234 }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    expect(screen.getByText(/12[.,]34%/)).toBeInTheDocument()
  })

  it('renders_loading_in_xirr_while_the_row_price_is_outstanding', () => {
    const rowPrice: RowPriceState = { isLoading: true, currentPrice: null, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [rowPrice] })
    renderComponent()
    const cells = within(screen.getAllByRole('row')[2]).getAllByRole('cell')
    expect(cells[11].textContent).toBe('...')
  })

  it('renders_dash_in_current_value_and_price_dependent_columns_on_price_failure', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [FAILED_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(5)
  })

  it('renders_dash_in_profit_when_total_invested_is_zero', () => {
    const item: PortfolioAssetSummaryItemDto = {
      ...ITEM_1,
      averagePrice: 0,
      totalInvested: 0,
      costOfUnitsHeld: 0,
      marketValue: 262.5,
    }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(2) // % Profit and % Profit w/ Credits
    expect(screen.getByText(/262[.,]50/)).toBeInTheDocument()
  })

  it('renders_dash_in_xirr_when_the_series_admits_no_rate', () => {
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [rowPrice] })
    renderComponent()
    const cells = within(screen.getAllByRole('row')[2]).getAllByRole('cell')
    expect(cells[11].textContent).toBe('—')
  })

  const PROFIT_CLASS_CASES: {
    name: string
    setup: () => void
    // computed via the same profit formula the component uses, not the component's own code
    expectedPercent: number
    expectedClass: 'portfolio-summary__profit--green' | 'portfolio-summary__profit--red'
  }[] = [
    {
      name: 'positive profit',
      // costOfUnitsHeld = 25 x 8 = 200; currentValue = 10.5 x 25 = 262.50; unrealisedGain = 62.50
      // totalCredits is non-zero so "Profit %" and "Profit % w/ Credits" render different text
      // (both would otherwise show 31.25% and make getByText ambiguous).
      setup: () => {
        const item: PortfolioAssetSummaryItemDto = {
          ...ITEM_1,
          currentQuantity: 25,
          averagePrice: 8,
          totalInvested: 200,
          totalCredits: 10,
          costOfUnitsHeld: 200,
          unrealisedGain: 62.5,
        }
        setPortfolioMock({ items: [item], rowPrices: [{ isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }] })
      },
      expectedPercent: 31.25,
      expectedClass: 'portfolio-summary__profit--green',
    },
    {
      name: 'negative profit',
      // costOfUnitsHeld = 25 x 12 = 300; currentValue = 262.50; unrealisedGain = -37.50
      // totalCredits is non-zero for the same reason as the "positive profit" case above.
      setup: () => {
        const item: PortfolioAssetSummaryItemDto = {
          ...ITEM_1,
          currentQuantity: 25,
          averagePrice: 12,
          totalInvested: 300,
          totalCredits: 10,
          costOfUnitsHeld: 300,
          unrealisedGain: -37.5,
        }
        setPortfolioMock({ items: [item], rowPrices: [{ isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }] })
      },
      expectedPercent: -12.5,
      expectedClass: 'portfolio-summary__profit--red',
    },
    {
      name: 'positive profit with credits',
      // costOfUnitsHeld = 25 x 12 = 300; currentValue = 262.50; unrealisedGain = -37.50; totalCredits = 50
      // profitWithCredits% = (-37.50 + 50) / 300 * 100
      setup: () => {
        const item: PortfolioAssetSummaryItemDto = {
          ...ITEM_1,
          currentQuantity: 25,
          averagePrice: 12,
          totalInvested: 300,
          totalCredits: 50,
          costOfUnitsHeld: 300,
          unrealisedGain: -37.5,
        }
        setPortfolioMock({ items: [item], rowPrices: [{ isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }] })
      },
      expectedPercent: (262.5 + 50 - 300) / 300 * 100,
      expectedClass: 'portfolio-summary__profit--green',
    },
    {
      name: 'negative profit with credits',
      // averagePrice is NOT overridden here, so costOfUnitsHeld uses ITEM_1's default (2500), not totalInvested:
      // currentValue = 262.50; unrealisedGain = 262.50 - 2500 = -2237.50; totalCredits = 10
      // profitWithCredits% = (-2237.50 + 10) / 2500 * 100
      setup: () => {
        const item: PortfolioAssetSummaryItemDto = {
          ...ITEM_1,
          currentQuantity: 25,
          totalInvested: 400,
          totalCredits: 10,
          unrealisedGain: -2237.5,
        }
        setPortfolioMock({ items: [item], rowPrices: [{ isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }] })
      },
      expectedPercent: (262.5 + 10 - 2500) / 2500 * 100,
      expectedClass: 'portfolio-summary__profit--red',
    },
    {
      name: 'positive xirr',
      setup: () => {
        const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, priceOnlyReturn: 0.1234 }
        setPortfolioMock({ items: [item], rowPrices: [{ isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }] })
      },
      expectedPercent: 12.34,
      expectedClass: 'portfolio-summary__profit--green',
    },
    {
      name: 'negative xirr',
      setup: () => {
        const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, priceOnlyReturn: -0.05 }
        setPortfolioMock({ items: [item], rowPrices: [{ isLoading: false, currentPrice: 10.5, fetchFailed: false, isManual: false }] })
      },
      expectedPercent: -5,
      expectedClass: 'portfolio-summary__profit--red',
    },
  ]

  it.each(PROFIT_CLASS_CASES)('applies the correct color class to $name', ({ setup, expectedPercent, expectedClass }) => {
    setAggregatedMock({ summary: SUMMARY })
    setup()
    renderComponent()
    const percentEl = screen.getByText(`${formatN2(expectedPercent)}%`)
    expect(percentEl).toHaveClass(expectedClass)
  })

  it('renders_empty_string_for_null_first_investment_date', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, firstInvestmentDate: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent()
    expect(screen.queryByText('01/03/2021')).not.toBeInTheDocument()
  })

  it('totals_section_is_unaffected_when_table_section_errors', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ error: 'F01 fetch failed' })
    renderComponent()
    expect(screen.getByText('Total Bought')).toBeInTheDocument()
    expect(screen.getByText('Total Sold')).toBeInTheDocument()
    expect(screen.getByText('Total Credits')).toBeInTheDocument()
    expect(screen.getByText('F01 fetch failed')).toBeInTheDocument()
  })

  it('renders_grouped_credits_analysis_column_headers_after_xirr', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText('Last Month')).toBeInTheDocument()
    expect(screen.getByText('Month')).toBeInTheDocument()
    expect(screen.getByText('Est. Annual')).toBeInTheDocument()
    // "Credits" sub-header appears under both the Last Month and Est. Annual groups
    expect(screen.getAllByText('Credits').length).toBeGreaterThanOrEqual(2)
    // "%" sub-header appears under Profit, Last Month, and Est. Annual groups
    expect(screen.getAllByText('%').length).toBeGreaterThanOrEqual(3)
  })

  it('renders_last_month_credits_with_formatted_value', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCredits: 12.50, lastCreditMonth: '2026-06' }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/12[.,]50/)).toBeInTheDocument()
  })

  it('renders_last_month_credits_as_dash_when_no_credits', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCredits: 0, lastCreditMonth: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_last_credit_month_in_mmm_yyyy_format', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCredits: 12.50, lastCreditMonth: '2026-06' }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText('Jun 2026')).toBeInTheDocument()
  })

  it('renders_last_credit_month_as_dash_when_null', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastCreditMonth: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_last_month_percent_with_percent_suffix', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCredits: 12.50, lastCreditMonth: '2026-06', lastMonthCreditsPercent: 1.25 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/1[.,]25%/)).toBeInTheDocument()
  })

  it('renders_last_month_percent_as_dash_when_null', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCreditsPercent: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_estimated_annual_credits_with_formatted_value', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCredits: 12.50, lastCreditMonth: '2026-06', estimatedAnnualCredits: 150.00 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/150[.,]00/)).toBeInTheDocument()
  })

  it('renders_estimated_annual_credits_as_dash_when_null', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, estimatedAnnualCredits: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_estimated_annual_percent_with_percent_suffix', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCredits: 12.50, lastCreditMonth: '2026-06', estimatedAnnualCredits: 150.00, estimatedAnnualPercent: 6.00 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/6[.,]00%/)).toBeInTheDocument()
  })

  it('renders_estimated_annual_percent_as_dash_when_null', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, estimatedAnnualPercent: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_credits_separator_class_on_last_month_group_header', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const header = screen.getByText('Last Month')
    expect(header).toHaveClass('portfolio-summary__credits-separator')
  })

  it('renders_footer_total_invested_from_the_server_summary_not_a_client_sum', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, totalInvested: 1000 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11', totalInvested: 2000, totalCredits: 0 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [LOADING_ROW_PRICE, LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue(formatN2(SUMMARY.totalInvested))).toBeInTheDocument()
    expect(screen.queryByDisplayValue(/3[,.]000[.,]00/)).not.toBeInTheDocument()
  })

  it('renders_footer_total_credits_from_the_server_summary_not_a_client_sum', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, totalCredits: 50, totalInvested: 1000 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11', totalCredits: 75, totalInvested: 2000 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [LOADING_ROW_PRICE, LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue(formatN2(SUMMARY.totalCredits))).toBeInTheDocument()
    expect(screen.queryByDisplayValue(/125[.,]00/)).not.toBeInTheDocument()
  })

  it('renders_footer_total_invested_and_header_total_invested_as_the_same_value', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const formatted = formatN2(SUMMARY.totalInvested)
    expect(screen.getAllByText(formatted).length + screen.getAllByDisplayValue(formatted).length).toBeGreaterThanOrEqual(2)
  })

  it('renders_footer_credits_label_with_current_month_and_year', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-07-15'))
    try {
      setAggregatedMock({ summary: SUMMARY })
      setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
      renderComponent()
      expect(screen.getByText('Credits Jul 2026')).toBeInTheDocument()
    } finally {
      vi.useRealTimers()
    }
  })

  it('renders_footer_current_month_credits_sum', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentMonthCredits: 10, totalInvested: 1000 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11', currentMonthCredits: 20, totalInvested: 2000 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [LOADING_ROW_PRICE, LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue(/30[.,]00/)).toBeInTheDocument()
  })

  it('renders_footer_estimated_annual_credits_sum_of_non_null', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, estimatedAnnualCredits: 600, totalInvested: 1000 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11', estimatedAnnualCredits: null, totalInvested: 2000 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [LOADING_ROW_PRICE, LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue(/600[.,]00/)).toBeInTheDocument()
  })

  it('renders_footer_estimated_annual_credits_as_dash_when_all_null', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, estimatedAnnualCredits: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    const { container } = renderComponent()
    const input = container.querySelector('[data-label="Est. Annual Credits"] + input') as HTMLInputElement
    expect(input.value).toBe('—')
  })

  it('renders_footer_current_value_as_calculating_when_all_prices_pending', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue('Calculating…')).toBeInTheDocument()
  })

  it('renders_footer_current_value_as_partial_sum_with_asterisk_while_prices_loading', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentQuantity: 5, marketValue: 50 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11' }
    const resolvedPrice: RowPriceState = { isLoading: false, currentPrice: 10, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [resolvedPrice, LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue(/50[.,]00 \*/)).toBeInTheDocument()
    expect(screen.getByText('excludes assets with pending prices')).toBeInTheDocument()
  })

  it('renders_footer_current_value_as_clean_sum_when_all_prices_resolved', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentQuantity: 5, marketValue: 50 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11', currentQuantity: 10, marketValue: 50 }
    const price1: RowPriceState = { isLoading: false, currentPrice: 10, fetchFailed: false, isManual: false }
    const price2: RowPriceState = { isLoading: false, currentPrice: 5, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [price1, price2] })
    renderComponent()
    expect(screen.getByDisplayValue(/100[.,]00/)).toBeInTheDocument()
    expect(screen.queryByText('excludes assets with pending prices')).not.toBeInTheDocument()
  })

  it('renders_footer_current_value_including_manually_priced_rows', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentQuantity: 5, marketValue: 50 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11', currentQuantity: 10, marketValue: 50 }
    const livePrice: RowPriceState = { isLoading: false, currentPrice: 10, fetchFailed: false, isManual: false }
    const manualPrice: RowPriceState = { isLoading: false, currentPrice: 5, fetchFailed: false, isManual: true }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [livePrice, manualPrice] })
    renderComponent()
    expect(screen.getByDisplayValue(/100[.,]00/)).toBeInTheDocument()
    expect(screen.queryByText('excludes assets with pending prices')).not.toBeInTheDocument()
  })

  it('sorts_rows_by_quantity_ascending_when_the_quantity_header_button_is_clicked', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'AAA11', currentQuantity: 25 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'BBB11', currentQuantity: 5 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [IDLE_ROW_PRICE, IDLE_ROW_PRICE] })
    renderComponent()

    fireEvent.click(screen.getByRole('button', { name: 'Quantity' }))

    // rows[0]/[1] are the two header rows; rows[2]/[3] are the data rows.
    const rows = screen.getAllByRole('row')
    expect(within(rows[2]).getAllByRole('cell')[0].textContent).toBe('BBB11')
    expect(within(rows[3]).getAllByRole('cell')[0].textContent).toBe('AAA11')
  })

  it('sorts_rows_by_quantity_descending_on_a_second_click_of_the_same_header', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'AAA11', currentQuantity: 25 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'BBB11', currentQuantity: 5 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [IDLE_ROW_PRICE, IDLE_ROW_PRICE] })
    renderComponent()

    const quantityHeaderButton = screen.getByRole('button', { name: 'Quantity' })
    fireEvent.click(quantityHeaderButton)
    fireEvent.click(quantityHeaderButton)

    const rows = screen.getAllByRole('row')
    expect(within(rows[2]).getAllByRole('cell')[0].textContent).toBe('AAA11')
    expect(within(rows[3]).getAllByRole('cell')[0].textContent).toBe('BBB11')
  })

  it('sorts_rows_by_a_derived_column_using_the_underlying_current_value_not_display_text', () => {
    // currentValue = item.marketValue: item1 = 50; item2 = 200.
    // Sorting ascending by Current Value must put item1 first even though its formatted
    // string ("50.00") would sort after item2's ("200.00") under a naive string compare.
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'AAA11', currentQuantity: 5, marketValue: 50 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'BBB11', currentQuantity: 100, marketValue: 200 }
    const price1: RowPriceState = { isLoading: false, currentPrice: 10, fetchFailed: false, isManual: false }
    const price2: RowPriceState = { isLoading: false, currentPrice: 2, fetchFailed: false, isManual: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [price1, price2] })
    renderComponent()

    fireEvent.click(screen.getByRole('button', { name: 'Current Value' }))

    const rows = screen.getAllByRole('row')
    expect(within(rows[2]).getAllByRole('cell')[0].textContent).toBe('AAA11')
    expect(within(rows[3]).getAllByRole('cell')[0].textContent).toBe('BBB11')
  })

  it('clicking_the_realized_gain_loss_header_engages_that_columns_sort_in_historic_scope', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'AAA11' }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'BBB11' }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [IDLE_ROW_PRICE, IDLE_ROW_PRICE] })
    renderComponent('historic')

    fireEvent.click(screen.getByRole('button', { name: 'Realized Gain/Loss' }))
    expect(screen.getByRole('columnheader', { name: 'Realized Gain/Loss' })).toHaveAttribute('aria-sort', 'ascending')
  })

  it('clicking_each_remaining_sortable_header_engages_that_columns_sort', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'AAA11' }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'BBB11' }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [IDLE_ROW_PRICE, IDLE_ROW_PRICE] })
    renderComponent()

    fireEvent.click(screen.getByRole('button', { name: 'Total Credits' }))
    expect(screen.getByRole('columnheader', { name: 'Total Credits' })).toHaveAttribute('aria-sort', 'ascending')

    fireEvent.click(screen.getByRole('button', { name: 'Average Price' }))
    expect(screen.getByRole('columnheader', { name: 'Average Price' })).toHaveAttribute('aria-sort', 'ascending')

    fireEvent.click(screen.getByRole('button', { name: 'w/ Credits' }))
    expect(screen.getByRole('columnheader', { name: 'w/ Credits' })).toHaveAttribute('aria-sort', 'ascending')

    const creditsHeaders = screen.getAllByRole('columnheader', { name: 'Credits' })
    fireEvent.click(within(creditsHeaders[0]).getByRole('button'))
    expect(creditsHeaders[0]).toHaveAttribute('aria-sort', 'ascending')

    fireEvent.click(screen.getByRole('button', { name: 'Month' }))
    expect(screen.getByRole('columnheader', { name: 'Month' })).toHaveAttribute('aria-sort', 'ascending')

    fireEvent.click(within(creditsHeaders[1]).getByRole('button'))
    expect(creditsHeaders[1]).toHaveAttribute('aria-sort', 'ascending')

    const percentHeaders = screen.getAllByRole('columnheader', { name: '%' })
    fireEvent.click(within(percentHeaders[2]).getByRole('button'))
    expect(percentHeaders[2]).toHaveAttribute('aria-sort', 'ascending')
  })

  it('footer_panel_is_not_inside_table_element', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(document.querySelector('.portfolio-summary__footer')).not.toBeNull()
    expect(document.querySelector('table tfoot')).toBeNull()
  })

  it('does_not_render_share_notice_when_fully_valued', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent()
    expect(document.querySelector('.portfolio-summary__share-notice')).toBeNull()
  })

  it('renders_share_notice_when_portfolio_partially_valued', () => {
    setAggregatedMock({ summary: { ...SUMMARY, holdingCount: 4, unvaluedHoldingCount: 1 } })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent()
    expect(document.querySelector('.portfolio-summary__share-notice')).toHaveTextContent(
      'Portfolio shares also do not total 100% for the same reason.',
    )
  })

  it('renders_share_notice_for_nothing_valuable_distinct_from_partial', () => {
    setAggregatedMock({ summary: { ...SUMMARY, holdingCount: 2, unvaluedHoldingCount: 2, marketValue: null } })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent()
    expect(document.querySelector('.portfolio-summary__share-notice')).toHaveTextContent(
      'No portfolio share can be computed for the same reason.',
    )
  })

  it('does_not_render_share_notice_in_historic_scope', () => {
    setAggregatedMock({ summary: { ...SUMMARY, holdingCount: 2, unvaluedHoldingCount: 2, marketValue: null } })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent('historic')
    expect(document.querySelector('.portfolio-summary__share-notice')).toBeNull()
  })
})
