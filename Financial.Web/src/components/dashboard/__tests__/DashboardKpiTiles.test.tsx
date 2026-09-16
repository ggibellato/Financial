import type { ComponentProps } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen } from '../../../test/renderWithFluent'
import type { PortfolioDashboardDto } from '../../../api/types'
import DashboardKpiTiles from '../DashboardKpiTiles'

const TILE_ORDER = [
  'Market Value',
  'Invested',
  'Unrealised Gain/Loss',
  'Realised Gain/Loss (Lifetime)',
  'Income YTD',
  'Income Lifetime',
  'Gross XIRR',
  'Net XIRR (of Tax)',
]

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

const CONVERTED_SUMMARY: PortfolioDashboardDto = {
  ...SUMMARY,
  isReportingCurrencyEnabled: true,
  convertedMarketValue: 20880,
  convertedInvested: 14175.78,
  convertedUnrealisedGainLoss: 6704.22,
  convertedRealisedGainLoss: 1392,
  convertedIncomeYtd: 394.69,
  convertedIncomeLifetime: 3295.27,
  convertedGrossXirr: 0.1102,
  convertedNetXirr: 0.0891,
}

const retry = vi.fn()
const onViewMissingPriceHoldings = vi.fn()

function renderTiles(props: Partial<ComponentProps<typeof DashboardKpiTiles>> = {}) {
  return render(
    <DashboardKpiTiles summary={SUMMARY} isLoading={false} error={null} retry={retry} {...props} />,
  )
}

function gridLabels(container: HTMLElement, gridIndex: number): (string | null)[] {
  const grid = container.querySelectorAll('.dashboard-kpi-tiles__grid')[gridIndex]
  return Array.from(grid.querySelectorAll('.dashboard-kpi-tiles__label')).map((label) => label.textContent)
}

function valueOf(label: string): Element | null {
  return screen.getByText(label).nextElementSibling
}

describe('DashboardKpiTiles', () => {
  beforeEach(() => {
    retry.mockReset()
    onViewMissingPriceHoldings.mockReset()
  })

  it('renders_the_eight_tiles_in_the_prescribed_order', () => {
    const { container } = renderTiles()

    expect(gridLabels(container, 0)).toEqual(TILE_ORDER)
  })

  it('renders_a_skeleton_in_every_tile_while_loading_keeping_the_grid_shape', () => {
    const { container } = renderTiles({ summary: null, isLoading: true })

    expect(gridLabels(container, 0)).toEqual(TILE_ORDER)
    expect(screen.getAllByRole('progressbar')).toHaveLength(8)
    expect(container.querySelectorAll('.dashboard-kpi-tiles__value')).toHaveLength(0)
  })

  it('renders_skeletons_before_the_first_response_arrives', () => {
    renderTiles({ summary: null, isLoading: false })

    expect(screen.getAllByRole('progressbar')).toHaveLength(8)
  })

  it('formats_money_tiles_to_two_decimals_and_xirr_tiles_as_percentages', () => {
    renderTiles()

    expect(valueOf('Market Value')?.textContent).toMatch(/18[.,]000[.,]00/)
    expect(valueOf('Invested')?.textContent).toMatch(/12[.,]220[.,]50/)
    expect(valueOf('Income YTD')?.textContent).toMatch(/340[.,]25/)
    expect(valueOf('Gross XIRR')?.textContent).toBe('12.34%')
    expect(valueOf('Net XIRR (of Tax)')?.textContent).toBe('9.87%')
  })

  it('leaves_unsigned_money_tiles_uncoloured', () => {
    renderTiles()

    expect(valueOf('Market Value')?.className).toBe('dashboard-kpi-tiles__value')
    expect(valueOf('Invested')?.className).toBe('dashboard-kpi-tiles__value')
    expect(valueOf('Income YTD')?.className).toBe('dashboard-kpi-tiles__value')
    expect(valueOf('Income Lifetime')?.className).toBe('dashboard-kpi-tiles__value')
  })

  it('colours_gain_loss_and_xirr_tiles_green_when_not_negative', () => {
    renderTiles()

    expect(valueOf('Unrealised Gain/Loss')).toHaveClass('dashboard-kpi-tiles__value--green')
    expect(valueOf('Realised Gain/Loss (Lifetime)')).toHaveClass('dashboard-kpi-tiles__value--green')
    expect(valueOf('Gross XIRR')).toHaveClass('dashboard-kpi-tiles__value--green')
    expect(valueOf('Net XIRR (of Tax)')).toHaveClass('dashboard-kpi-tiles__value--green')
  })

  it('colours_gain_loss_and_xirr_tiles_red_when_negative', () => {
    renderTiles({
      summary: { ...SUMMARY, unrealisedGainLoss: -420.5, realisedGainLoss: -50, grossXirr: -0.02, netXirr: -0.03 },
    })

    expect(valueOf('Unrealised Gain/Loss')).toHaveClass('dashboard-kpi-tiles__value--red')
    expect(valueOf('Realised Gain/Loss (Lifetime)')).toHaveClass('dashboard-kpi-tiles__value--red')
    expect(valueOf('Gross XIRR')).toHaveClass('dashboard-kpi-tiles__value--red')
    expect(valueOf('Net XIRR (of Tax)')).toHaveClass('dashboard-kpi-tiles__value--red')
  })

  it('renders_a_dash_without_colour_when_an_xirr_is_null', () => {
    renderTiles({ summary: { ...SUMMARY, grossXirr: null, netXirr: null } })

    expect(valueOf('Gross XIRR')?.textContent).toBe('—')
    expect(valueOf('Net XIRR (of Tax)')?.textContent).toBe('—')
    expect(valueOf('Gross XIRR')?.className).toBe('dashboard-kpi-tiles__value')
    expect(valueOf('Net XIRR (of Tax)')?.className).toBe('dashboard-kpi-tiles__value')
  })

  it('does_not_render_the_partial_notice_when_every_holding_is_valued', () => {
    renderTiles()

    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('renders_the_partial_notice_naming_the_unvalued_holding_count', () => {
    renderTiles({ summary: { ...SUMMARY, isPartial: true, unvaluedHoldingCount: 3 } })

    expect(screen.getByRole('status')).toHaveTextContent(
      '3 holdings could not be valued; Market Value, Unrealised Gain/Loss and both XIRR figures are incomplete.',
    )
  })

  it('renders_the_partial_notice_in_the_singular_for_one_unvalued_holding', () => {
    renderTiles({ summary: { ...SUMMARY, isPartial: true, unvaluedHoldingCount: 1 } })

    expect(screen.getByRole('status')).toHaveTextContent('1 holding could not be valued;')
  })

  it('view_affected_holdings_invokes_the_cross_panel_callback', () => {
    renderTiles({
      summary: { ...SUMMARY, isPartial: true, unvaluedHoldingCount: 3 },
      onViewMissingPriceHoldings,
    })

    screen.getByRole('button', { name: 'View affected holdings' }).click()

    expect(onViewMissingPriceHoldings).toHaveBeenCalledTimes(1)
  })

  it('omits_the_cross_panel_link_when_no_callback_is_wired', () => {
    renderTiles({ summary: { ...SUMMARY, isPartial: true, unvaluedHoldingCount: 3 } })

    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'View affected holdings' })).not.toBeInTheDocument()
  })

  it('hides_the_converted_block_entirely_when_reporting_currency_is_disabled', () => {
    const { container } = renderTiles()

    expect(container.querySelectorAll('.dashboard-kpi-tiles__grid')).toHaveLength(1)
    expect(screen.queryByRole('heading', { name: /Converted to/ })).not.toBeInTheDocument()
  })

  it('renders_the_converted_block_with_the_same_eight_tiles_when_reporting_currency_is_enabled', () => {
    const { container } = renderTiles({ summary: CONVERTED_SUMMARY })

    expect(screen.getByRole('heading', { name: 'Converted to GBP', level: 4 })).toBeInTheDocument()
    expect(gridLabels(container, 1)).toEqual(TILE_ORDER.map((label) => `${label} (converted to GBP)`))
    expect(valueOf('Market Value (converted to GBP)')?.textContent).toMatch(/20[.,]880[.,]00/)
    expect(valueOf('Gross XIRR (converted to GBP)')?.textContent).toBe('11.02%')
  })

  it('renders_a_dash_for_a_converted_figure_that_could_not_be_converted', () => {
    renderTiles({ summary: { ...CONVERTED_SUMMARY, convertedMarketValue: null } })

    expect(valueOf('Market Value (converted to GBP)')?.textContent).toBe('—')
  })

  it('warns_above_the_converted_grid_when_the_conversion_is_partial', () => {
    const { container } = renderTiles({ summary: { ...CONVERTED_SUMMARY, isReportingCurrencyPartial: true } })

    expect(screen.getByRole('status')).toHaveTextContent(
      'Some figures could not be converted to GBP — showing partial totals.',
    )
    expect(container.querySelectorAll('.dashboard-kpi-tiles__grid')).toHaveLength(2)
  })

  it('replaces_the_converted_block_with_a_retryable_error_when_conversion_is_unavailable', () => {
    const { container } = renderTiles({ summary: { ...CONVERTED_SUMMARY, isReportingCurrencyUnavailable: true } })

    expect(screen.getByRole('alert')).toHaveTextContent('Converted totals unavailable')
    expect(container.querySelectorAll('.dashboard-kpi-tiles__grid')).toHaveLength(1)
    expect(valueOf('Market Value')?.textContent).toMatch(/18[.,]000[.,]00/)
  })

  it('retrying_the_unavailable_conversion_refetches_the_dashboard', () => {
    renderTiles({ summary: { ...CONVERTED_SUMMARY, isReportingCurrencyUnavailable: true } })

    screen.getByRole('button', { name: 'Try again' }).click()

    expect(retry).toHaveBeenCalledTimes(1)
  })

  it('replaces_the_panel_with_a_retryable_error_state_when_the_request_failed', () => {
    renderTiles({ summary: null, error: 'Unable to load dashboard summary' })

    expect(screen.getByRole('alert')).toHaveTextContent('Unable to load dashboard summary')
    expect(screen.queryByText('Market Value')).not.toBeInTheDocument()

    screen.getByRole('button', { name: 'Try again' }).click()

    expect(retry).toHaveBeenCalledTimes(1)
  })
})
