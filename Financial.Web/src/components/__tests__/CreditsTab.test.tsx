import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import React from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetDetailsDto, CreditDto, SelectedNode } from '../../api/types'
import { pinDate } from '../../test-utils/pinDate'
import { renderWithSelectedNode } from '../../test-utils/renderWithSelectedNode'
import CreditsTab from '../CreditsTab'

const { getAssetDetailsMock, getCreditsByBrokerMock, addCreditMock, updateCreditMock, deleteCreditMock } = vi.hoisted(() => ({
  getAssetDetailsMock: vi.fn<FinancialApiClient['getAssetDetails']>(),
  getCreditsByBrokerMock: vi.fn<FinancialApiClient['getCreditsByBroker']>(),
  addCreditMock: vi.fn<FinancialApiClient['addCredit']>(),
  updateCreditMock: vi.fn<FinancialApiClient['updateCredit']>(),
  deleteCreditMock: vi.fn<FinancialApiClient['deleteCredit']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAssetDetails: getAssetDetailsMock,
    getCreditsByBroker: getCreditsByBrokerMock,
    addCredit: addCreditMock,
    updateCredit: updateCreditMock,
    deleteCredit: deleteCreditMock,
  } as Partial<FinancialApiClient>,
}))

vi.mock('recharts', () => ({
  BarChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="bar-chart">{children}</div>
  ),
  Bar: ({ name, dataKey, children }: { name?: string; dataKey: BucketReader | string; children?: React.ReactNode }) => (
    <div
      data-testid="bar"
      data-name={name}
      data-own={typeof dataKey === 'function' ? String(dataKey(bucketWith(name))) : dataKey}
      data-other={typeof dataKey === 'function' ? String(dataKey(bucketWith(undefined))) : dataKey}
    >
      {children}
    </div>
  ),
  LineChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="line-chart">{children}</div>
  ),
  Line: ({ name, dataKey }: { name?: string; dataKey: BucketReader | string }) => (
    <div
      data-testid="line"
      data-name={name}
      data-own={typeof dataKey === 'function' ? String(dataKey(bucketWith(name))) : dataKey}
    />
  ),
  XAxis: () => null,
  YAxis: () => null,
  CartesianGrid: () => null,
  Tooltip: ({ formatter }: { formatter: (value: unknown) => unknown }) => (
    <div data-testid="tooltip" data-number={String(formatter(12.5))} data-text={String(formatter('n/a'))} />
  ),
  Legend: () => null,
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
  LabelList: ({ formatter }: { formatter: (value: unknown) => string }) => (
    <div
      data-testid="label"
      data-positive={formatter(7)}
      data-zero={formatter(0)}
      data-text={formatter('n/a')}
    />
  ),
}))

type BucketReader = (bucket: { byType: Record<string, number> }) => number

function bucketWith(type: string | undefined) {
  return { byType: type ? { [type]: 7 } : {} }
}

const ASSET_NODE: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetName: 'KLBN4',
}

const BROKER_NODE: SelectedNode = { nodeType: 'Broker', brokerName: 'XPI' }

const NO_DIVIDEND_ATTRIBUTION = {
  sharesForDividend: null,
  attributedShares: null,
  averageCostPerShare: null,
  investedAmount: null,
  priceOnDate: null,
  marketValueOnDate: null,
  yieldOnInvested: null,
  yieldOnMarket: null,
}

const CREDIT_DIVIDEND: CreditDto = {
  id: 'aaa',
  date: '2024-03-15T00:00:00',
  type: 'Dividend',
  value: 120.5,
  withheld: 0,
  intermediationFee: 0,
  netAmount: 120.5,
  currency: 'GBP',
  fxRateSnapshot: null,
  ...NO_DIVIDEND_ATTRIBUTION,
}

const CREDIT_SECURITIES_LENDING_INCOME: CreditDto = {
  id: 'bbb',
  date: '2024-01-10T00:00:00',
  type: 'SecuritiesLendingIncome',
  value: 350.0,
  withheld: 0,
  intermediationFee: 0,
  netAmount: 350.0,
  currency: 'GBP',
  fxRateSnapshot: null,
  ...NO_DIVIDEND_ATTRIBUTION,
}

const CREDIT_JCP: CreditDto = {
  id: 'ccc',
  date: '2024-02-20T00:00:00',
  type: 'JCP',
  value: 75.0,
  withheld: 0,
  intermediationFee: 0,
  netAmount: 75.0,
  currency: 'GBP',
  fxRateSnapshot: null,
  ...NO_DIVIDEND_ATTRIBUTION,
}

function assetDetails(credits: CreditDto[]): AssetDetailsDto {
  return { credits } as AssetDetailsDto
}

async function renderAssetTab(credits: CreditDto[] = []) {
  getAssetDetailsMock.mockResolvedValue(assetDetails(credits))
  const result = renderWithSelectedNode(<CreditsTab />, ASSET_NODE)
  await screen.findByRole('table')
  return result
}

async function renderBrokerTab(credits: CreditDto[] = []) {
  getCreditsByBrokerMock.mockResolvedValue(credits)
  const result = renderWithSelectedNode(<CreditsTab />, BROKER_NODE)
  await screen.findByTestId('responsive-container')
  return result
}

async function openNewForm() {
  fireEvent.click(await screen.findByRole('button', { name: 'New credit' }))
  await screen.findByRole('heading', { name: 'New credit' })
}

function dataRows() {
  return within(screen.getByRole('table')).getAllByRole('row').slice(1)
}

describe('CreditsTab', () => {
  beforeEach(() => {
    pinDate('2024-06-01T12:00:00+01:00')
    sessionStorage.clear()
    getAssetDetailsMock.mockReset()
    getCreditsByBrokerMock.mockReset()
    addCreditMock.mockReset()
    updateCreditMock.mockReset()
    deleteCreditMock.mockReset()
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.useRealTimers()
  })

  it('renders_loading_state', () => {
    getAssetDetailsMock.mockReturnValue(new Promise(() => {}))
    renderWithSelectedNode(<CreditsTab />, ASSET_NODE)
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry', async () => {
    getAssetDetailsMock.mockRejectedValueOnce(new Error('Network error'))
    getAssetDetailsMock.mockResolvedValueOnce(assetDetails([CREDIT_DIVIDEND]))
    renderWithSelectedNode(<CreditsTab />, ASSET_NODE)

    expect(await screen.findByText('Network error')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('table')).toBeInTheDocument()
    expect(screen.queryByText('Network error')).not.toBeInTheDocument()
    expect(getAssetDetailsMock).toHaveBeenCalledTimes(2)
  })

  it('renders_chart_only_for_broker_node', async () => {
    await renderBrokerTab([CREDIT_DIVIDEND])
    expect(screen.getByTestId('responsive-container')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'New credit' })).not.toBeInTheDocument()
  })

  it('renders_chart_above_the_grid_for_asset_node_matching_transactions_tab_layout', async () => {
    const { container } = await renderAssetTab([CREDIT_DIVIDEND])
    const chartPanel = container.querySelector('.credits-tab__chart-panel')
    const table = screen.getByRole('table')
    expect(chartPanel).toBeInTheDocument()
    expect(chartPanel?.compareDocumentPosition(table) ?? 0).toBeGreaterThanOrEqual(Node.DOCUMENT_POSITION_FOLLOWING)
    expect(chartPanel).toHaveClass('credits-tab__chart-panel--compact')
  })

  it('renders_table_columns_date_type_value', async () => {
    await renderAssetTab([CREDIT_DIVIDEND])
    expect(screen.getByText('Date')).toBeInTheDocument()
    expect(screen.getByText('Type')).toBeInTheDocument()
    expect(screen.getByText('Value')).toBeInTheDocument()
  })

  it('renders_date_in_dd_MM_yyyy_format', async () => {
    await renderAssetTab([CREDIT_DIVIDEND])
    expect(screen.getByText('15/03/2024')).toBeInTheDocument()
  })

  it.each([
    [CREDIT_DIVIDEND, 'Dividend', 'credits-tab__type--dividend'],
    [CREDIT_SECURITIES_LENDING_INCOME, 'Securities Lending Income', 'credits-tab__type--securities-lending-income'],
    [CREDIT_JCP, 'JCP', 'credits-tab__type--jcp'],
  ])('renders_%#_credit_type_%s_with_its_class', async (credit, label, typeClass) => {
    await renderAssetTab([credit])
    expect(screen.getByText(label)).toHaveClass(typeClass)
  })

  it('renders_value_in_n2_bold', async () => {
    await renderAssetTab([CREDIT_DIVIDEND])
    const [valueCell] = screen.getAllByText('120.50')
    expect(valueCell).toHaveClass('credits-tab__value')
  })

  it('new_button_shows_the_new_credit_form', async () => {
    await renderAssetTab()
    expect(screen.queryByRole('heading', { name: 'New credit' })).not.toBeInTheDocument()
    await openNewForm()
    expect(screen.getByRole('heading', { name: 'New credit' })).toBeInTheDocument()
  })

  it('renders_form_when_form_visible', async () => {
    await renderAssetTab()
    await openNewForm()
    expect(screen.getByLabelText(/^Date/)).toBeInTheDocument()
    expect(screen.getByLabelText('Type')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Value/)).toBeInTheDocument()
    expect(screen.getByLabelText('Shares for this dividend')).toBeInTheDocument()
  })

  it('type_select_includes_jcp_option', async () => {
    await renderAssetTab()
    await openNewForm()
    const select = screen.getByLabelText('Type') as HTMLSelectElement
    const optionValues = Array.from(select.options).map((o) => o.value)
    expect(optionValues).toEqual(['Dividend', 'SecuritiesLendingIncome', 'JCP', 'Coupon'])
  })

  it('new_form_defaults_the_date_to_today', async () => {
    await renderAssetTab()
    await openNewForm()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2024-06-01')
    expect(screen.getByLabelText('Type')).toHaveValue('Dividend')
  })

  it('cancel_hides_the_form', async () => {
    await renderAssetTab()
    await openNewForm()
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(screen.queryByRole('heading', { name: 'New credit' })).not.toBeInTheDocument()
  })

  it('edit_icon_shows_the_edit_form_prefilled_with_the_credit', async () => {
    await renderAssetTab([CREDIT_DIVIDEND])
    fireEvent.click(screen.getByRole('button', { name: 'Edit credit' }))

    expect(await screen.findByRole('heading', { name: 'Edit credit' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2024-03-15')
    expect(screen.getByLabelText('Type')).toHaveValue('Dividend')
    expect(screen.getByLabelText(/^Value/)).toHaveValue(120.5)
  })

  it('editing_each_form_field_shows_the_typed_value', async () => {
    await renderAssetTab()
    await openNewForm()

    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2024-05-01' } })
    fireEvent.change(screen.getByLabelText('Type'), { target: { value: 'SecuritiesLendingIncome' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '99.5' } })
    fireEvent.change(screen.getByLabelText('Shares for this dividend'), { target: { value: '800' } })

    expect(screen.getByLabelText(/^Date/)).toHaveValue('2024-05-01')
    expect(screen.getByLabelText('Type')).toHaveValue('SecuritiesLendingIncome')
    expect(screen.getByLabelText(/^Value/)).toHaveValue(99.5)
    expect(screen.getByLabelText('Shares for this dividend')).toHaveValue(800)
  })

  it('saving_a_new_credit_posts_the_payload_and_shows_the_new_row', async () => {
    const created: CreditDto = { ...CREDIT_JCP, id: 'new', date: '2024-05-01T00:00:00', value: 99.5, netAmount: 99.5 }
    await renderAssetTab([CREDIT_DIVIDEND])
    addCreditMock.mockResolvedValue(assetDetails([CREDIT_DIVIDEND, created]))
    await openNewForm()

    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2024-05-01' } })
    fireEvent.change(screen.getByLabelText('Type'), { target: { value: 'JCP' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '99.5' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add credit' }))

    expect(await screen.findByText('01/05/2024')).toBeInTheDocument()
    expect(addCreditMock).toHaveBeenCalledWith({
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      assetName: 'KLBN4',
      date: '2024-05-01',
      type: 'JCP',
      value: 99.5,
      withheld: 0,
      intermediationFee: 0,
      sharesForDividend: null,
    })
    expect(screen.queryByRole('heading', { name: 'New credit' })).not.toBeInTheDocument()
    expect(dataRows()).toHaveLength(2)
  })

  it('saving_an_edited_credit_puts_the_payload_and_shows_the_updated_row', async () => {
    await renderAssetTab([CREDIT_DIVIDEND])
    updateCreditMock.mockResolvedValue(
      assetDetails([{ ...CREDIT_DIVIDEND, value: 130, netAmount: 130 }]),
    )
    fireEvent.click(screen.getByRole('button', { name: 'Edit credit' }))
    await screen.findByRole('heading', { name: 'Edit credit' })

    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '130' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect((await screen.findAllByText('130.00'))[0]).toBeInTheDocument()
    expect(updateCreditMock).toHaveBeenCalledWith({
      id: 'aaa',
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      assetName: 'KLBN4',
      date: '2024-03-15',
      type: 'Dividend',
      value: 130,
      withheld: 0,
      intermediationFee: 0,
      sharesForDividend: null,
    })
    expect(screen.queryByRole('heading', { name: 'Edit credit' })).not.toBeInTheDocument()
    expect(screen.queryByText('120.50')).not.toBeInTheDocument()
  })

  it('save_button_is_disabled_and_reads_saving_while_the_save_is_pending', async () => {
    await renderAssetTab()
    addCreditMock.mockReturnValue(new Promise(() => {}))
    await openNewForm()

    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '10' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add credit' }))

    expect(await screen.findByRole('button', { name: 'Saving...' })).toBeDisabled()
  })

  it('renders_save_error_below_form', async () => {
    await renderAssetTab()
    addCreditMock.mockRejectedValue(new Error('Failed'))
    await openNewForm()

    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '10' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add credit' }))

    expect(await screen.findByText('Failed')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'New credit' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add credit' })).toBeEnabled()
  })

  it('saving_without_a_value_shows_the_validation_message_and_does_not_call_the_api', async () => {
    await renderAssetTab()
    await openNewForm()

    fireEvent.click(screen.getByRole('button', { name: 'Add credit' }))

    expect(await screen.findByText('Value must not be zero (negative is a correction)')).toBeInTheDocument()
    expect(addCreditMock).not.toHaveBeenCalled()
  })

  it('delete_icon_deletes_the_credit_and_removes_its_row_after_the_user_confirms', async () => {
    await renderAssetTab([CREDIT_DIVIDEND, CREDIT_JCP])
    deleteCreditMock.mockResolvedValue(assetDetails([CREDIT_JCP]))

    const dividendRow = screen.getByText('15/03/2024').closest('tr, [role="row"]') as HTMLElement
    fireEvent.click(within(dividendRow).getByRole('button', { name: 'Delete credit' }))

    await waitFor(() => expect(screen.queryByText('15/03/2024')).not.toBeInTheDocument())
    expect(deleteCreditMock).toHaveBeenCalledWith({
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      assetName: 'KLBN4',
      id: 'aaa',
    })
    expect(screen.getByText('20/02/2024')).toBeInTheDocument()
  })

  it('delete_icon_does_not_delete_when_the_user_cancels_the_confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    await renderAssetTab([CREDIT_DIVIDEND])

    fireEvent.click(screen.getByRole('button', { name: 'Delete credit' }))

    expect(deleteCreditMock).not.toHaveBeenCalled()
    expect(screen.getByText('15/03/2024')).toBeInTheDocument()
  })

  it('renders_delete_error_below_table', async () => {
    await renderAssetTab([CREDIT_DIVIDEND])
    deleteCreditMock.mockRejectedValue(new Error('Failed to delete'))

    fireEvent.click(screen.getByRole('button', { name: 'Delete credit' }))

    expect(await screen.findByText('Failed to delete')).toBeInTheDocument()
    expect(screen.getByText('15/03/2024')).toBeInTheDocument()
  })

  it('renders_all_filter_buttons', async () => {
    await renderAssetTab()
    expect(screen.getByRole('tab', { name: 'This month' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Last 3 months' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Last 6 months' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Last 12 months' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'YTD' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'All time' })).toBeInTheDocument()
  })

  it('active_filter_has_active_class', async () => {
    await renderAssetTab()
    expect(screen.getByRole('tab', { name: 'Last 12 months' })).toHaveAttribute('aria-selected', 'true')
  })

  it('clicking_filter_selects_that_filter', async () => {
    await renderAssetTab()
    fireEvent.click(screen.getByRole('tab', { name: 'Last 3 months' }))
    expect(screen.getByRole('tab', { name: 'Last 3 months' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Last 12 months' })).toHaveAttribute('aria-selected', 'false')
  })

  it('active_mode_has_active_class', async () => {
    await renderAssetTab()
    expect(screen.getByRole('tab', { name: 'Stacked' })).toHaveAttribute('aria-selected', 'true')
  })

  it('clicking_mode_selects_that_mode', async () => {
    await renderAssetTab()
    fireEvent.click(screen.getByRole('tab', { name: 'Grouped' }))
    expect(screen.getByRole('tab', { name: 'Grouped' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Stacked' })).toHaveAttribute('aria-selected', 'false')
  })

  it('empty_table_renders_no_rows', async () => {
    await renderAssetTab([])
    expect(dataRows()).toHaveLength(0)
  })

  it('renders_bar_line_toggle_defaulting_to_bar', async () => {
    await renderAssetTab()
    expect(screen.getByRole('tab', { name: 'Bar' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Line' })).toHaveAttribute('aria-selected', 'false')
  })

  it('clicking_line_toggle_selects_line_and_renders_the_line_chart', async () => {
    await renderAssetTab()
    fireEvent.click(screen.getByRole('tab', { name: 'Line' }))
    expect(screen.getByRole('tab', { name: 'Line' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Bar' })).toHaveAttribute('aria-selected', 'false')
    expect(screen.getByTestId('line-chart')).toBeInTheDocument()
  })

  it('renders_bar_chart_unchanged_when_bar_selected', async () => {
    await renderAssetTab()
    expect(screen.getByTestId('bar-chart')).toBeInTheDocument()
    expect(screen.queryByTestId('line-chart')).not.toBeInTheDocument()
  })

  it('renders_single_total_line_when_grouped_and_line', async () => {
    await renderAssetTab([CREDIT_DIVIDEND, CREDIT_SECURITIES_LENDING_INCOME])
    fireEvent.click(screen.getByRole('tab', { name: 'Line' }))
    fireEvent.click(screen.getByRole('tab', { name: 'Grouped' }))

    const lines = screen.getAllByTestId('line')
    expect(lines).toHaveLength(1)
    expect(lines[0]).toHaveAttribute('data-name', 'Total')
  })

  it('renders_one_line_per_type_when_stacked_and_line', async () => {
    await renderAssetTab([CREDIT_DIVIDEND, CREDIT_SECURITIES_LENDING_INCOME])
    fireEvent.click(screen.getByRole('tab', { name: 'Line' }))

    const lines = screen.getAllByTestId('line')
    expect(lines).toHaveLength(2)
    expect(lines.map((l) => l.getAttribute('data-name'))).toEqual(['Dividend', 'SecuritiesLendingIncome'])
  })

  it('clicking_value_header_sorts_rows_ascending_then_descending', async () => {
    await renderAssetTab([CREDIT_DIVIDEND, CREDIT_SECURITIES_LENDING_INCOME, CREDIT_JCP])

    fireEvent.click(screen.getByRole('button', { name: 'Value' }))
    let rows = dataRows()
    expect(within(rows[0]).getAllByText('75.00')[0]).toBeInTheDocument()
    expect(within(rows[1]).getAllByText('120.50')[0]).toBeInTheDocument()
    expect(within(rows[2]).getAllByText('350.00')[0]).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Value' }))
    rows = dataRows()
    expect(within(rows[0]).getAllByText('350.00')[0]).toBeInTheDocument()
    expect(within(rows[1]).getAllByText('120.50')[0]).toBeInTheDocument()
    expect(within(rows[2]).getAllByText('75.00')[0]).toBeInTheDocument()
  })

  it('clicking_type_header_sorts_rows_by_type_ascending', async () => {
    await renderAssetTab([CREDIT_SECURITIES_LENDING_INCOME, CREDIT_DIVIDEND, CREDIT_JCP])

    fireEvent.click(screen.getByRole('button', { name: 'Type' }))

    const rows = dataRows()
    expect(within(rows[0]).getByText('Dividend')).toBeInTheDocument()
    expect(within(rows[1]).getByText('JCP')).toBeInTheDocument()
    expect(within(rows[2]).getByText('Securities Lending Income')).toBeInTheDocument()
  })

  it('defaults to sorting by date descending, with the header showing the active sort', async () => {
    await renderAssetTab([CREDIT_SECURITIES_LENDING_INCOME, CREDIT_DIVIDEND, CREDIT_JCP])
    const table = screen.getByRole('table')
    const dateHeaderButton = within(table).getByRole('button', { name: 'Date' })
    const dateHeader = dateHeaderButton.closest('th')

    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')
    let rows = dataRows()
    expect(within(rows[0]).getByText('15/03/2024')).toBeInTheDocument()
    expect(within(rows[1]).getByText('20/02/2024')).toBeInTheDocument()
    expect(within(rows[2]).getByText('10/01/2024')).toBeInTheDocument()

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'none')

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'ascending')
    rows = dataRows()
    expect(within(rows[0]).getByText('10/01/2024')).toBeInTheDocument()
    expect(within(rows[1]).getByText('20/02/2024')).toBeInTheDocument()
    expect(within(rows[2]).getByText('15/03/2024')).toBeInTheDocument()

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')
  })

  it('shows_the_yield_affordance_for_a_credit_with_shares_attributed', async () => {
    await renderAssetTab([
      { ...CREDIT_DIVIDEND, sharesForDividend: 800, attributedShares: 800, investedAmount: 7200, yieldOnInvested: 5.5556 },
    ])
    expect(screen.getByRole('button', { name: 'Dividend yield details' })).toBeInTheDocument()
  })

  it('does_not_show_the_yield_affordance_for_a_credit_without_shares_attributed', async () => {
    await renderAssetTab([CREDIT_DIVIDEND])
    expect(screen.queryByRole('button', { name: 'Dividend yield details' })).not.toBeInTheDocument()
  })

  it('shows_the_yield_affordance_for_a_credit_with_no_shares_entered_but_attributed_to_the_entire_position', async () => {
    await renderAssetTab([
      { ...CREDIT_DIVIDEND, sharesForDividend: null, attributedShares: 1000, investedAmount: 9000, yieldOnInvested: 4.4444 },
    ])
    expect(screen.getByRole('button', { name: 'Dividend yield details' })).toBeInTheDocument()
  })

  it('shows_yield_bought_and_yield_current_columns_when_computed', async () => {
    await renderAssetTab([{ ...CREDIT_DIVIDEND, sharesForDividend: 800, yieldOnInvested: 5.5556, yieldOnMarket: 5 }])
    expect(screen.getByText('5.6%')).toBeInTheDocument()
    expect(screen.getByText('5.0%')).toBeInTheDocument()
  })

  it('shows_a_dash_for_yield_columns_when_not_computed', async () => {
    await renderAssetTab([CREDIT_DIVIDEND])
    expect(within(dataRows()[0]).getAllByText('—')).not.toHaveLength(0)
  })

  it('shows_the_fx_provenance_affordance_for_a_credit_with_a_captured_snapshot', async () => {
    await renderAssetTab([
      {
        ...CREDIT_DIVIDEND,
        currency: 'BRL',
        fxRateSnapshot: { toCurrency: 'GBP', rate: 0.146, source: 'Frankfurter', retrievedAt: '2026-07-01T08:00:00Z' },
      },
    ])
    expect(screen.getByRole('button', { name: 'FX conversion details' })).toBeInTheDocument()
  })

  it('does_not_show_the_fx_provenance_affordance_for_a_credit_without_a_captured_snapshot', async () => {
    await renderAssetTab([{ ...CREDIT_DIVIDEND, currency: 'GBP', fxRateSnapshot: null }])
    expect(screen.queryByRole('button', { name: 'FX conversion details' })).not.toBeInTheDocument()
  })

  it.each([
    ['Yield (Bought)', 'yieldOnInvested'],
    ['Yield (Current)', 'yieldOnMarket'],
  ] as const)('sorting_by_%s_ranks_credits_without_a_yield_below_those_with_one', async (header, field) => {
    const withYield: CreditDto = { ...CREDIT_DIVIDEND, id: 'y1', [field]: 5 }
    const withoutYield: CreditDto = { ...CREDIT_JCP, id: 'y2', [field]: null }
    const lowYield: CreditDto = { ...CREDIT_SECURITIES_LENDING_INCOME, id: 'y3', [field]: 1 }
    await renderAssetTab([withYield, withoutYield, lowYield])

    fireEvent.click(screen.getByRole('button', { name: header }))

    expect(screen.getByRole('button', { name: header }).closest('th')).toHaveAttribute('aria-sort', 'ascending')
    expect(dataRows().map((row) => row.textContent?.match(/JCP|Securities Lending Income|Dividend/)?.[0])).toEqual([
      'JCP',
      'Securities Lending Income',
      'Dividend',
    ])
  })

  it.each(['Withheld', 'Intermediation fee', 'Net'])(
    'clicking_the_%s_header_marks_it_as_the_active_sort',
    async (header) => {
      await renderAssetTab([CREDIT_DIVIDEND])

      fireEvent.click(screen.getByRole('button', { name: header }))

      expect(screen.getByRole('button', { name: header }).closest('th')).toHaveAttribute('aria-sort', 'ascending')
    },
  )

  it('shows_an_unknown_credit_type_as_its_raw_text_with_the_dividend_style', async () => {
    await renderAssetTab([{ ...CREDIT_DIVIDEND, type: 'Bonus' }])

    expect(screen.getByText('Bonus')).toHaveClass('credits-tab__type--dividend')
  })

  it('chart_tooltip_formats_numbers_to_two_decimals_and_leaves_other_values_untouched', async () => {
    await renderBrokerTab([CREDIT_DIVIDEND])

    const tooltip = screen.getByTestId('tooltip')
    expect(tooltip).toHaveAttribute('data-number', '12.50')
    expect(tooltip).toHaveAttribute('data-text', 'n/a')
  })

  it('each_bar_series_reads_its_own_type_from_the_month_bucket_and_zero_for_other_types', async () => {
    await renderBrokerTab([CREDIT_DIVIDEND, CREDIT_JCP])

    const bars = screen.getAllByTestId('bar')
    expect(bars.map((b) => b.getAttribute('data-name'))).toEqual(['Dividend', 'JCP'])
    for (const bar of bars) {
      expect(bar).toHaveAttribute('data-own', '7')
      expect(bar).toHaveAttribute('data-other', '0')
    }
  })

  it('bar_labels_show_positive_amounts_and_hide_zero_and_non_numeric_values', async () => {
    await renderBrokerTab([CREDIT_DIVIDEND])

    const label = screen.getByTestId('label')
    expect(label).toHaveAttribute('data-positive', '7.00')
    expect(label).toHaveAttribute('data-zero', '')
    expect(label).toHaveAttribute('data-text', '')
  })

  it('stacked_line_series_read_their_own_type_and_the_grouped_line_reads_the_total', async () => {
    await renderBrokerTab([CREDIT_DIVIDEND])
    fireEvent.click(screen.getByRole('tab', { name: 'Line' }))
    expect(screen.getByTestId('line')).toHaveAttribute('data-own', '7')

    fireEvent.click(screen.getByRole('tab', { name: 'Grouped' }))
    expect(screen.getByTestId('line')).toHaveAttribute('data-own', 'total')
  })
})
