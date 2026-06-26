import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AssetSummaryData } from '../../hooks/useAssetSummary'
import type { AssetDetailsDto, AssetPriceDto } from '../../api/types'
import AssetSummaryTab from '../AssetSummaryTab'

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
  totalBought: 2000,
  totalSold: 500,
  totalCredits: 50,
  transactions: [],
  credits: [],
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
    })
  })

  it('renders_loading_indicator_while_asset_loads', () => {
    setMock({ isLoadingAsset: true })
    render(<AssetSummaryTab />)
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry_on_asset_failure', () => {
    setMock({ assetError: 'Unable to load asset details' })
    render(<AssetSummaryTab />)
    expect(screen.getByText('Unable to load asset details')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders_all_metadata_fields', () => {
    setMock({ asset: ASSET, showCurrentSection: false })
    render(<AssetSummaryTab />)
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
    const { container } = render(<AssetSummaryTab />)
    const totalBoughtLabel = screen.getByText('Total Bought')
    const valueEl = totalBoughtLabel.nextElementSibling
    expect(valueEl).toHaveClass('asset-summary__value--green')
    expect(container).toBeTruthy()
  })

  it('renders_total_sold_in_red', () => {
    setMock({ asset: ASSET })
    render(<AssetSummaryTab />)
    const totalSoldLabel = screen.getByText('Total Sold')
    const valueEl = totalSoldLabel.nextElementSibling
    expect(valueEl).toHaveClass('asset-summary__value--red')
  })

  it('renders_total_credits_in_blue', () => {
    setMock({ asset: ASSET })
    render(<AssetSummaryTab />)
    const totalCreditsLabel = screen.getByText('Total Credits')
    const valueEl = totalCreditsLabel.nextElementSibling
    expect(valueEl).toHaveClass('asset-summary__value--blue')
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
    render(<AssetSummaryTab />)
    expect(screen.getByText('Current')).toBeInTheDocument()
    expect(screen.getByText('Current Value')).toBeInTheDocument()
    expect(screen.getByText('As of')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Refresh' })).toBeInTheDocument()
  })

  it('hides_current_section_when_quantity_is_zero', () => {
    setMock({ asset: { ...ASSET, quantity: 0 }, showCurrentSection: false })
    render(<AssetSummaryTab />)
    expect(screen.queryByText('Current')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Refresh' })).not.toBeInTheDocument()
  })

  it('hides_current_section_when_average_price_is_zero', () => {
    setMock({ asset: { ...ASSET, averagePrice: 0 }, showCurrentSection: false })
    render(<AssetSummaryTab />)
    expect(screen.queryByText('Current')).not.toBeInTheDocument()
  })

  it('renders_dash_for_current_value_while_price_loads', () => {
    setMock({ asset: ASSET, showCurrentSection: true, isLoadingPrice: true, price: null })
    render(<AssetSummaryTab />)
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
    render(<AssetSummaryTab />)
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
    render(<AssetSummaryTab />)
    const resultLabel = screen.getByText('Result %')
    const valueEl = resultLabel.nextElementSibling
    expect(valueEl).toHaveClass('asset-summary__value--red')
  })

  it('renders_price_error_in_status_field', () => {
    setMock({ asset: ASSET, showCurrentSection: true, priceError: 'Price unavailable' })
    render(<AssetSummaryTab />)
    expect(screen.getByText('Status')).toBeInTheDocument()
    const statusLabel = screen.getByText('Status')
    expect(statusLabel.nextElementSibling?.textContent).toBe('Price unavailable')
    expect(statusLabel.nextElementSibling).toHaveClass('asset-summary__value--error')
  })

  it('refresh_button_enabled_after_price_load', () => {
    setMock({ asset: ASSET, price: PRICE, showCurrentSection: true, canRefresh: true, totalCurrentValue: 2500, resultPercent: 0.25, totalCurrentPlusCredits: 2550, resultWithCreditsPercent: 0.275 })
    render(<AssetSummaryTab />)
    expect(screen.getByRole('button', { name: 'Refresh' })).not.toBeDisabled()
  })

  it('disables_refresh_button_while_price_is_loading', () => {
    setMock({ asset: ASSET, showCurrentSection: true, isLoadingPrice: true, canRefresh: false })
    render(<AssetSummaryTab />)
    expect(screen.getByRole('button', { name: 'Refresh' })).toBeDisabled()
  })

  it('calls_refresh_on_button_click', () => {
    setMock({ asset: ASSET, price: PRICE, showCurrentSection: true, canRefresh: true, totalCurrentValue: 2500, resultPercent: 0.25, totalCurrentPlusCredits: 2550, resultWithCreditsPercent: 0.275 })
    render(<AssetSummaryTab />)
    fireEvent.click(screen.getByRole('button', { name: 'Refresh' }))
    expect(mockRefresh).toHaveBeenCalledTimes(1)
  })
})
