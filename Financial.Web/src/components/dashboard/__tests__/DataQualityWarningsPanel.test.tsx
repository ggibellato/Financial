import { createRef } from 'react'
import { act } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, waitFor } from '../../../test/renderWithFluent'
import type { DataQualityReportDto } from '../../../api/types'
import DataQualityWarningsPanel from '../DataQualityWarningsPanel'
import type { DataQualityWarningsPanelHandle } from '../DataQualityWarningsPanel'

const { navigateToHoldingMock } = vi.hoisted(() => ({
  navigateToHoldingMock: vi.fn(),
}))

vi.mock('../../../hooks/useHoldingNavigation', () => ({
  useHoldingNavigation: () => ({ navigateToHolding: navigateToHoldingMock }),
}))

const EMPTY_REPORT: DataQualityReportDto = {
  salesExceedPurchases: [],
  unpricedOpenHoldings: [],
  openHoldingsMissingCostBasis: [],
  unresolvedTaxClassifications: [],
  staleValuationCount: 0,
  historicHoldingsStillOpen: [],
  unclassifiedHoldings: [],
  unclassifiedAndUnpricedOpenHoldings: [],
  corporateActionsAwaitingTaxReview: [],
}

const FULL_REPORT: DataQualityReportDto = {
  ...EMPTY_REPORT,
  salesExceedPurchases: [
    {
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      assetName: 'KLBN4',
      offendingSaleDate: '2025-03-04T00:00:00Z',
      quantityHeld: 10,
      shortfall: 4,
    },
  ],
  unpricedOpenHoldings: [{ brokerName: 'Trading212', portfolioName: 'ISA', assetName: 'VUSA' }],
  openHoldingsMissingCostBasis: [{ brokerName: 'Chase', portfolioName: 'GIA', assetName: 'VWRL' }],
  unresolvedTaxClassifications: [
    { brokerName: 'XPI', portfolioName: 'Acoes', assetName: 'PETR4', taxYear: '2024/25', eventCategory: 'Dividend' },
  ],
  staleValuationCount: 3,
}

const REPORT_WITH_CORPORATE_ACTION: DataQualityReportDto = {
  ...FULL_REPORT,
  corporateActionsAwaitingTaxReview: [
    {
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      assetName: 'PETR4',
      taxYear: '2025/26',
      type: 'SpinOff',
      corporateActionId: 'ca-123',
      effectiveDate: '2025-11-01T00:00:00Z',
    },
  ],
}

const noop = () => {}

function renderPanel(report: DataQualityReportDto | null, ref?: React.RefObject<DataQualityWarningsPanelHandle | null>) {
  return render(
    <DataQualityWarningsPanel ref={ref} report={report} isLoading={false} error={null} retry={noop} />,
  )
}

describe('DataQualityWarningsPanel', () => {
  beforeEach(() => {
    navigateToHoldingMock.mockReset()
    navigateToHoldingMock.mockResolvedValue(true)
    Element.prototype.scrollIntoView = vi.fn()
  })

  it('renders_the_categories_in_the_prd_severity_order', () => {
    renderPanel(FULL_REPORT)

    const rendered = screen.getAllByRole('button').map((button) => button.textContent)
    expect(rendered).toEqual([
      'Impossible cash-flow sequence (1)',
      'Missing price (1)',
      'Missing cost basis (1)',
      'Unresolved tax classification (1)',
    ])
  })

  it('places_the_stale_valuation_count_between_missing_cost_basis_and_tax_classification', () => {
    const { container } = renderPanel(FULL_REPORT)

    const text = container.textContent ?? ''
    expect(text.indexOf('Missing cost basis')).toBeLessThan(text.indexOf('Stale valuation (3)'))
    expect(text.indexOf('Stale valuation (3)')).toBeLessThan(text.indexOf('Unresolved tax classification'))
  })

  it('renders_the_stale_valuation_count_without_an_expand_affordance', () => {
    renderPanel({ ...EMPTY_REPORT, staleValuationCount: 5 })

    expect(screen.getByText('Stale valuation (5)')).toBeInTheDocument()
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('renders_no_item_for_a_zero_count_category', () => {
    renderPanel({ ...FULL_REPORT, unpricedOpenHoldings: [] })

    expect(screen.queryByText(/Missing price/)).not.toBeInTheDocument()
    expect(screen.getByText('Missing cost basis (1)')).toBeInTheDocument()
  })

  it('shows_a_success_message_when_every_category_is_zero', () => {
    renderPanel(EMPTY_REPORT)

    expect(screen.getByText('No data-quality issues detected')).toBeInTheDocument()
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('clicking_a_finding_row_navigates_to_that_holding', async () => {
    renderPanel(FULL_REPORT)

    fireEvent.click(screen.getByText('Missing price (1)'))
    fireEvent.click(screen.getByText('VUSA'))

    await waitFor(() => expect(navigateToHoldingMock).toHaveBeenCalledWith('Trading212', 'ISA', 'VUSA', undefined))
  })

  it('closes the only open category when its header is clicked again', () => {
    renderPanel(FULL_REPORT)

    const header = screen.getByRole('button', { name: /Missing price/ })
    fireEvent.click(header)
    expect(header).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('VUSA')).toBeInTheDocument()

    fireEvent.click(header)
    expect(header).toHaveAttribute('aria-expanded', 'false')
    expect(screen.queryByText('VUSA')).not.toBeInTheDocument()
  })

  it('shows_a_dismissible_warning_when_the_holding_cannot_be_located', async () => {
    navigateToHoldingMock.mockResolvedValue(false)

    renderPanel(FULL_REPORT)

    fireEvent.click(screen.getByText('Missing price (1)'))
    fireEvent.click(screen.getByText('VUSA'))

    const warning = await screen.findByText(
      'Unable to locate VUSA — it may have moved or been archived since this report was generated.',
    )
    expect(warning).toBeInTheDocument()
    expect(screen.getByRole('alert')).toContainElement(warning)

    fireEvent.click(screen.getByRole('button', { name: 'Dismiss' }))

    await waitFor(() =>
      expect(
        screen.queryByText(
          'Unable to locate VUSA — it may have moved or been archived since this report was generated.',
        ),
      ).not.toBeInTheDocument(),
    )
  })

  it('shows_the_sale_detail_on_an_impossible_cash_flow_row', () => {
    renderPanel(FULL_REPORT)

    fireEvent.click(screen.getByText('Impossible cash-flow sequence (1)'))

    expect(screen.getByText(/sold 4.00 more than held/)).toBeInTheDocument()
  })

  it('shows_the_tax_year_and_event_category_on_a_tax_classification_row', () => {
    renderPanel(FULL_REPORT)

    fireEvent.click(screen.getByText('Unresolved tax classification (1)'))

    expect(screen.getByText(/Dividend, tax year 2024\/25/)).toBeInTheDocument()
  })

  it('expandCategory_opens_the_missing_price_item_and_scrolls_the_panel_into_view', async () => {
    const ref = createRef<DataQualityWarningsPanelHandle>()
    renderPanel(FULL_REPORT, ref)

    act(() => ref.current?.expandCategory('missingPrice'))

    await waitFor(() => expect(screen.getByText('VUSA')).toBeInTheDocument())
    expect(Element.prototype.scrollIntoView).toHaveBeenCalledWith({ behavior: 'smooth', block: 'start' })
  })

  it('expandCategory_moves_focus_to_the_opened_accordion_header', async () => {
    const ref = createRef<DataQualityWarningsPanelHandle>()
    renderPanel(FULL_REPORT, ref)

    act(() => ref.current?.expandCategory('missingPrice'))

    await waitFor(() =>
      expect(screen.getByRole('button', { name: /Missing price/ })).toHaveFocus(),
    )
  })

  it('accordion_headers_are_marked_up_as_headings', () => {
    renderPanel(FULL_REPORT)

    expect(screen.getByRole('heading', { name: /Missing price/, level: 4 })).toBeInTheDocument()
  })

  it('renders_the_corporate_action_awaiting_tax_review_category_with_the_correct_label_and_count', () => {
    renderPanel(REPORT_WITH_CORPORATE_ACTION)

    expect(screen.getByText('Corporate action awaiting tax review (1)')).toBeInTheDocument()
  })

  it('shows_the_humanized_type_and_tax_year_on_a_corporate_action_row', () => {
    renderPanel(REPORT_WITH_CORPORATE_ACTION)

    fireEvent.click(screen.getByText('Corporate action awaiting tax review (1)'))

    expect(screen.getByText(/Spin-off, tax year 2025\/26/)).toBeInTheDocument()
  })

  it('clicking_a_corporate_action_row_navigates_with_the_corporateActionId_as_the_4th_argument', async () => {
    renderPanel(REPORT_WITH_CORPORATE_ACTION)

    fireEvent.click(screen.getByText('Corporate action awaiting tax review (1)'))
    fireEvent.click(screen.getByText('PETR4'))

    await waitFor(() =>
      expect(navigateToHoldingMock).toHaveBeenCalledWith('XPI', 'Acoes', 'PETR4', 'ca-123'),
    )
  })

  it('shows_the_loading_state_while_the_report_is_pending', () => {
    render(<DataQualityWarningsPanel report={null} isLoading error={null} retry={noop} />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows_the_error_state_with_a_retry_action', () => {
    const retry = vi.fn()
    render(<DataQualityWarningsPanel report={null} isLoading={false} error="Service unavailable" retry={retry} />)

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(retry).toHaveBeenCalledTimes(1)
  })
})
