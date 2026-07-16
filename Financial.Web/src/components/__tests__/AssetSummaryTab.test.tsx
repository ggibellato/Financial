import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AssetSummaryData } from '../../hooks/useAssetSummary'
import type { AssetDetailsDto, AssetPriceDto, InvestmentScope } from '../../api/types'
import { SelectedNodeProvider } from '../../context/SelectedNodeContext'
import AssetSummaryTab from '../AssetSummaryTab'

function renderAssetSummaryTab(scope: InvestmentScope = 'active') {
  return render(
    <SelectedNodeProvider scope={scope}>
      <AssetSummaryTab />
    </SelectedNodeProvider>,
  )
}

const mockRefresh = vi.fn()
const mockRetryAsset = vi.fn()

const mockHookValue: AssetSummaryData = {
  asset: null,
  isLoadingAsset: false,
  assetError: null,
  retryAsset: mockRetryAsset,
  price: null,
  isLoadingPrice: false,
  priceError: null,
  canRefresh: true,
  refresh: mockRefresh,
  showCurrentSection: false,
  totalCurrentValue: 0,
  resultPercent: 0,
  totalCurrentPlusCredits: 0,
  resultWithCreditsPercent: 0,
  xirr: null,
  xirrWithCredits: null,
  realizedGainLoss: null,
  portfolioWeight: null,
}

vi.mock('../../hooks/useAssetSummary', () => ({
  useAssetSummary: () => mockHookValue,
}))

const ASSET: AssetDetailsDto = {
  name: 'KLBN4',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  ticker: 'KLBN4',
  isin: 'BRKLBN',
  exchange: 'BVMF',
  country: 'BR',
  localTypeCode: 'ON',
  class: 'Equity',
  quantity: 100,
  averagePrice: 20,
  isActive: true,
  positionType: 'Long',
  totalBought: 2000,
  totalSold: 500,
  totalCredits: 50,
  realizedGainLoss: 75,
  transactions: [],
  credits: [],
  cashFlowsWithCredits: [],
  cashFlowsWithoutCredits: [],
}

const PRICE: AssetPriceDto = {
  exchange: 'BVMF',
  ticker: 'KLBN4',
  name: 'Klabin',
  price: 25,
  asOf: '2026-06-26T10:00:00',
}

function setMock(overrides: Partial<AssetSummaryData>) {
  Object.assign(mockHookValue, overrides)
}

describe('AssetSummaryTab', () => {
  beforeEach(() => {
    mockRefresh.mockReset()
    mockRetryAsset.mockReset()
    Object.assign(mockHookValue, {
      asset: null,
      isLoadingAsset: false,
      assetError: null,
      price: null,
      isLoadingPrice: false,
      priceError: null,
      canRefresh: true,
      showCurrentSection: false,
      totalCurrentValue: 0,
      resultPercent: 0,
      totalCurrentPlusCredits: 0,
      resultWithCreditsPercent: 0,
      xirr: null,
      xirrWithCredits: null,
    })
  })

  it('renders_loading_indicator_while_asset_loads', () => {
    setMock({ isLoadingAsset: true })
    renderAssetSummaryTab()
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry_on_asset_failure', () => {
    setMock({ assetError: 'Unable to load asset details' })
    renderAssetSummaryTab()
    expect(screen.getByText('Unable to load asset details')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders_all_metadata_fields', () => {
    setMock({ asset: ASSET, showCurrentSection: false })
    renderAssetSummaryTab()
    expect(screen.getByText('Quantity')).toBeInTheDocument()
    expect(screen.getByText('Average Price')).toBeInTheDocument()
    expect(screen.getByText('ISIN')).toBeInTheDocument()
    expect(screen.getByText('Country')).toBeInTheDocument()
    expect(screen.getByText('Local Type')).toBeInTheDocument()
    expect(screen.getByText('Asset Class')).toBeInTheDocument()
    expect(screen.getByText('BR')).toBeInTheDocument()
    expect(screen.getByText('Equity')).toBeInTheDocument()
  })

  it('renders_total_bought_in_green', () => {
    setMock({ asset: ASSET })
    renderAssetSummaryTab()
    const totalBoughtLabel = screen.getByText('Total Bought')
    const valueEl = totalBoughtLabel.nextElementSibling
    expect(valueEl).toHaveClass('asset-summary__value--green')
  })

  it('renders_total_sold_in_red', () => {
    setMock({ asset: ASSET })
    renderAssetSummaryTab()
    const totalSoldLabel = screen.getByText('Total Sold')
    const valueEl = totalSoldLabel.nextElementSibling
    expect(valueEl).toHaveClass('asset-summary__value--red')
  })

  it('renders_total_credits_in_blue', () => {
    setMock({ asset: ASSET })
    renderAssetSummaryTab()
    const totalCreditsLabel = screen.getByText('Total Credits')
    const valueEl = totalCreditsLabel.nextElementSibling
    expect(valueEl).toHaveClass('asset-summary__value--blue')
  })

  it('renders_realized_gain_loss_field_regardless_of_current_section', () => {
    setMock({ asset: ASSET, showCurrentSection: false })
    render(<AssetSummaryTab />)
    const label = screen.getByText('Realized Gain/Loss')
    expect(label.nextElementSibling?.textContent).toBe('75.00')
  })

  it('renders_positive_realized_gain_loss_in_green', () => {
    setMock({ asset: { ...ASSET, realizedGainLoss: 75 } })
    render(<AssetSummaryTab />)
    const label = screen.getByText('Realized Gain/Loss')
    expect(label.nextElementSibling).toHaveClass('asset-summary__value--green')
  })

  it('renders_negative_realized_gain_loss_in_red', () => {
    setMock({ asset: { ...ASSET, realizedGainLoss: -30 } })
    render(<AssetSummaryTab />)
    const label = screen.getByText('Realized Gain/Loss')
    expect(label.nextElementSibling).toHaveClass('asset-summary__value--red')
  })

  it('renders_current_section_when_quantity_and_price_nonzero', () => {
    setMock({
      asset: ASSET,
      price: PRICE,
      showCurrentSection: true,
      totalCurrentValue: 2500,
      resultPercent: 0.25,
      totalCurrentPlusCredits: 2550,
      resultWithCreditsPercent: 0.275,
    })
    renderAssetSummaryTab()
    expect(screen.getByText('Current')).toBeInTheDocument()
    expect(screen.getByText('Current Value')).toBeInTheDocument()
    expect(screen.getByText('As of')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Refresh' })).toBeInTheDocument()
  })

  it('hides_current_section_when_quantity_is_zero', () => {
    setMock({ asset: { ...ASSET, quantity: 0 }, showCurrentSection: false })
    renderAssetSummaryTab()
    expect(screen.queryByText('Current')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Refresh' })).not.toBeInTheDocument()
  })

  it('renders_realized_totals_section_for_historic_scope', () => {
    setMock({
      asset: { ...ASSET, quantity: 0 },
      showCurrentSection: false,
      realizedGainLoss: -50,
      portfolioWeight: 100,
    })
    renderAssetSummaryTab('historic')
    expect(screen.getByText('Realized')).toBeInTheDocument()
    expect(screen.getByText('Realized Gain/Loss')).toBeInTheDocument()
    expect(screen.getByText('Portfolio Weight')).toBeInTheDocument()
    expect(screen.queryByText('Current')).not.toBeInTheDocument()
    expect(screen.queryByText('XIRR')).not.toBeInTheDocument()
  })

  it('hides_realized_totals_section_for_active_scope', () => {
    setMock({ asset: ASSET, showCurrentSection: true, price: PRICE })
    renderAssetSummaryTab('active')
    expect(screen.queryByText('Realized')).not.toBeInTheDocument()
    expect(screen.getByText('Current')).toBeInTheDocument()
  })

  it('hides_current_section_for_historic_scope_even_when_hook_reports_it_true', () => {
    setMock({ asset: ASSET, showCurrentSection: true, price: PRICE })
    renderAssetSummaryTab('historic')
    expect(screen.queryByText('Current')).not.toBeInTheDocument()
  })

  it('hides_current_section_when_average_price_is_zero', () => {
    setMock({ asset: { ...ASSET, averagePrice: 0 }, showCurrentSection: false })
    renderAssetSummaryTab()
    expect(screen.queryByText('Current')).not.toBeInTheDocument()
  })

  it('renders_dash_for_current_value_while_price_loads', () => {
    setMock({ asset: ASSET, showCurrentSection: true, isLoadingPrice: true, price: null })
    renderAssetSummaryTab()
    const currentValueLabel = screen.getByText('Current Value')
    expect(currentValueLabel.nextElementSibling?.textContent).toBe('—')
  })

  it('renders_positive_result_percent_in_green', () => {
    setMock({
      asset: ASSET,
      price: PRICE,
      showCurrentSection: true,
      totalCurrentValue: 2500,
      resultPercent: 0.25,
      totalCurrentPlusCredits: 2550,
      resultWithCreditsPercent: 0.275,
    })
    renderAssetSummaryTab()
    const resultLabel = screen.getByText('Result %')
    const valueEl = resultLabel.nextElementSibling
    expect(valueEl).toHaveClass('asset-summary__value--green')
  })

  it('renders_negative_result_percent_in_red', () => {
    setMock({
      asset: ASSET,
      price: PRICE,
      showCurrentSection: true,
      totalCurrentValue: 1500,
      resultPercent: -0.25,
      totalCurrentPlusCredits: 1550,
      resultWithCreditsPercent: -0.225,
    })
    renderAssetSummaryTab()
    const resultLabel = screen.getByText('Result %')
    const valueEl = resultLabel.nextElementSibling
    expect(valueEl).toHaveClass('asset-summary__value--red')
  })

  it('renders_price_error_in_status_field', () => {
    setMock({ asset: ASSET, showCurrentSection: true, priceError: 'Price unavailable' })
    renderAssetSummaryTab()
    expect(screen.getByText('Status')).toBeInTheDocument()
    const statusLabel = screen.getByText('Status')
    expect(statusLabel.nextElementSibling?.textContent).toBe('Price unavailable')
    expect(statusLabel.nextElementSibling).toHaveClass('asset-summary__value--error')
  })

  it('refresh_button_enabled_after_price_load', () => {
    setMock({ asset: ASSET, price: PRICE, showCurrentSection: true, canRefresh: true, totalCurrentValue: 2500, resultPercent: 0.25, totalCurrentPlusCredits: 2550, resultWithCreditsPercent: 0.275 })
    renderAssetSummaryTab()
    expect(screen.getByRole('button', { name: 'Refresh' })).not.toBeDisabled()
  })

  it('disables_refresh_button_while_price_is_loading', () => {
    setMock({ asset: ASSET, showCurrentSection: true, isLoadingPrice: true, canRefresh: false })
    renderAssetSummaryTab()
    expect(screen.getByRole('button', { name: 'Refresh' })).toBeDisabled()
  })

  it('calls_refresh_on_button_click', () => {
    setMock({ asset: ASSET, price: PRICE, showCurrentSection: true, canRefresh: true, totalCurrentValue: 2500, resultPercent: 0.25, totalCurrentPlusCredits: 2550, resultWithCreditsPercent: 0.275 })
    renderAssetSummaryTab()
    fireEvent.click(screen.getByRole('button', { name: 'Refresh' }))
    expect(mockRefresh).toHaveBeenCalledTimes(1)
  })

  it('renders_dash_for_xirr_while_not_yet_computed', () => {
    setMock({
      asset: ASSET,
      price: PRICE,
      showCurrentSection: true,
      totalCurrentValue: 2500,
      totalCurrentPlusCredits: 2550,
      xirr: null,
      xirrWithCredits: null,
    })
    renderAssetSummaryTab()
    const xirrLabel = screen.getByText('XIRR')
    expect(xirrLabel.nextElementSibling?.textContent).toBe('—')
    const xirrWithCreditsLabel = screen.getByText('XIRR w/ Credits')
    expect(xirrWithCreditsLabel.nextElementSibling?.textContent).toBe('—')
  })

  it('renders_positive_xirr_in_green', () => {
    setMock({
      asset: ASSET,
      price: PRICE,
      showCurrentSection: true,
      totalCurrentValue: 2500,
      totalCurrentPlusCredits: 2550,
      xirr: 0.1234,
      xirrWithCredits: 0.15,
    })
    renderAssetSummaryTab()
    const xirrLabel = screen.getByText('XIRR')
    expect(xirrLabel.nextElementSibling).toHaveClass('asset-summary__value--green')
  })

  it('renders_negative_xirr_in_red', () => {
    setMock({
      asset: ASSET,
      price: PRICE,
      showCurrentSection: true,
      totalCurrentValue: 1500,
      totalCurrentPlusCredits: 1550,
      xirr: -0.1234,
      xirrWithCredits: -0.1,
    })
    renderAssetSummaryTab()
    const xirrLabel = screen.getByText('XIRR')
    expect(xirrLabel.nextElementSibling).toHaveClass('asset-summary__value--red')
  })
})
