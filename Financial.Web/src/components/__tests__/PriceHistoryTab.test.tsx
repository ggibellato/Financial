import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import React from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetDetailsDto, AssetPriceSnapshotDto, SelectedNode } from '../../api/types'
import { confirmDelete as confirmDeleteOf, requestDelete as requestDeleteOf } from '../../test-utils/confirmDelete'
import { pinDate } from '../../test-utils/pinDate'
import { renderWithSelectedNode } from '../../test-utils/renderWithSelectedNode'
import PriceHistoryTab from '../PriceHistoryTab'
import { findErrorAlert } from '../../test-utils/errorAlert'

const { getAssetDetailsMock, setAssetPriceMock, deleteAssetPriceMock } = vi.hoisted(() => ({
  getAssetDetailsMock: vi.fn<FinancialApiClient['getAssetDetails']>(),
  setAssetPriceMock: vi.fn<FinancialApiClient['setAssetPrice']>(),
  deleteAssetPriceMock: vi.fn<FinancialApiClient['deleteAssetPrice']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAssetDetails: getAssetDetailsMock,
    setAssetPrice: setAssetPriceMock,
    deleteAssetPrice: deleteAssetPriceMock,
  } as Partial<FinancialApiClient>,
}))

type MockDotProps = {
  cx?: number
  cy?: number
  payload?: { kind: 'automatic' | 'manual' | 'buy' | 'sell'; date: string; value: number }
  key?: React.Key | null
}

vi.mock('recharts', () => ({
  LineChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="line-chart">{children}</div>
  ),
  Line: ({ dot }: { dot?: (props: MockDotProps) => React.ReactNode }) => (
    <div data-testid="chart-dots">
      {dot?.({ cx: 10, cy: 20, payload: { kind: 'automatic', date: '2024-01-01', value: 10 }, key: 'automatic' })}
      {dot?.({ cx: 10, cy: 20, payload: { kind: 'manual', date: '2024-01-02', value: 20 }, key: 'manual' })}
      {dot?.({ cx: 10, cy: 20, payload: { kind: 'buy', date: '2024-01-03', value: 30 }, key: 'buy' })}
      {dot?.({ cx: 10, cy: 20, payload: { kind: 'sell', date: '2024-01-04', value: 40 }, key: 'sell' })}
      {dot?.({ cx: undefined, cy: undefined, payload: undefined, key: 'missing' })}
    </div>
  ),
  XAxis: () => null,
  YAxis: () => null,
  CartesianGrid: () => null,
  Tooltip: ({
    content,
  }: {
    content?: React.ReactElement<{ active?: boolean; payload?: { payload: MockDotProps['payload'] }[] }>
  }) =>
    content ? (
      <div data-testid="chart-tooltip">
        {React.cloneElement(content, { active: false })}
        {React.cloneElement(content, {
          active: true,
          payload: [{ payload: { kind: 'manual', date: '2024-01-02', value: 20.5 } }],
        })}
      </div>
    ) : null,
  Legend: ({ content: Content }: { content?: React.ComponentType }) => (Content ? <Content /> : null),
  DefaultLegendContent: ({ payload }: { payload?: { value?: string }[] }) => (
    <div data-testid="chart-legend">{payload?.map((entry) => entry.value).join(',')}</div>
  ),
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
}))

const ASSET_NODE: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetName: 'KLBN4',
  ticker: 'KLBN4',
  exchange: 'BVMF',
  positionType: 'Long',
}

const MANUAL_ENTRY: AssetPriceSnapshotDto = {
  date: '2024-03-15T00:00:00',
  price: 120.5,
  isManual: true,
  currency: 'BRL',
  source: 'Manual',
  sourceReference: null,
  valuationMethod: 'MarketPrice',
  retrievedAt: '2024-03-15T00:00:00Z',
}

const AUTOMATIC_ENTRY: AssetPriceSnapshotDto = {
  date: '2024-01-10T00:00:00',
  price: 350.0,
  isManual: false,
  currency: 'BRL',
  source: 'Google',
  sourceReference: null,
  valuationMethod: 'MarketPrice',
  retrievedAt: '2024-01-10T00:00:00Z',
}

const ASSET_DETAILS: AssetDetailsDto = {
  name: 'KLBN4',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  ticker: 'KLBN4',
  isin: 'BRKLBN',
  exchange: 'BVMF',
  country: 'BR',
  localTypeCode: 'ON',
  class: 'Equity',
  valuationMethod: 'Unspecified',
  incomePolicy: 'Unknown',
  quantity: 100,
  averagePrice: 20,
  averageSellPrice: null,
  positionType: 'Long',
  totalBought: 2000,
  totalSold: 0,
  totalCredits: 0,
  realizedGainLoss: 0,
  realizedGainLossSharesOnly: 0,
  marketValue: null,
  costOfUnitsHeld: 2000,
  unrealisedGain: null,
  resultPercent: null,
  resultWithCreditsPercent: null,
  priceAsOfDate: null,
  marketStatus: 'Current',
  priceOnlyReturn: null,
  totalReturn: null,
  transactions: [],
  credits: [],
  priceSnapshots: [],
  disposalRecords: [],
  corporateActions: [],
  costBasisMethod: 'AverageCost',
  taxJurisdictions: [],
}

function detailsWith(...priceSnapshots: AssetPriceSnapshotDto[]): AssetDetailsDto {
  return { ...ASSET_DETAILS, priceSnapshots }
}

async function renderLoaded(...entries: AssetPriceSnapshotDto[]) {
  getAssetDetailsMock.mockResolvedValue(detailsWith(...entries))
  renderWithSelectedNode(<PriceHistoryTab />, ASSET_NODE)
  return screen.findByRole('table')
}

async function renderEmpty() {
  getAssetDetailsMock.mockResolvedValue(detailsWith())
  renderWithSelectedNode(<PriceHistoryTab />, ASSET_NODE)
  await screen.findByRole('button', { name: 'New price' })
}

const EARLIER_MANUAL_ENTRY: AssetPriceSnapshotDto = { ...MANUAL_ENTRY, date: '2024-02-10T00:00:00', price: 110 }
const MANUAL_NAME = 'price 15/03/2024'
const EARLIER_MANUAL_NAME = 'price 10/02/2024'
const requestDelete = (name: string) => requestDeleteOf(name, 'price')
const confirmDelete = (name: string) => confirmDeleteOf(name, 'price')
const rowButton = (name: string) => screen.getByRole('button', { name, hidden: true })

function dataRows(table: HTMLElement) {
  return within(table).getAllByRole('row').slice(1)
}

const priceInput = () => screen.getByRole('spinbutton', { name: /^Price/ })

describe('PriceHistoryTab', () => {
  beforeEach(() => {
    pinDate('2026-08-20T12:00:00+01:00')
    sessionStorage.clear()
    getAssetDetailsMock.mockReset()
    setAssetPriceMock.mockReset()
    deleteAssetPriceMock.mockReset()
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.useRealTimers()
  })

  it('renders_loading_state', async () => {
    getAssetDetailsMock.mockReturnValue(new Promise(() => {}))
    renderWithSelectedNode(<PriceHistoryTab />, ASSET_NODE)
    expect(await screen.findByText('Loading…')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry', async () => {
    getAssetDetailsMock.mockRejectedValueOnce(new Error('Network error'))
    getAssetDetailsMock.mockResolvedValue(detailsWith(MANUAL_ENTRY))
    renderWithSelectedNode(<PriceHistoryTab />, ASSET_NODE)

    expect(await screen.findByText('Network error')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('15/03/2024')).toBeInTheDocument()
    expect(screen.queryByText('Network error')).not.toBeInTheDocument()
    expect(getAssetDetailsMock).toHaveBeenCalledTimes(2)
  })

  it('requests_the_selected_asset_price_history', async () => {
    await renderLoaded(MANUAL_ENTRY)
    expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4', 'active')
  })

  it('renders_table_and_chart', async () => {
    await renderLoaded(MANUAL_ENTRY)
    expect(screen.getByRole('table')).toBeInTheDocument()
    expect(screen.getByTestId('responsive-container')).toBeInTheDocument()
  })

  it('renders_table_columns_date_price_source', async () => {
    await renderLoaded(MANUAL_ENTRY)
    expect(screen.getByText('Date')).toBeInTheDocument()
    expect(screen.getByText('Price')).toBeInTheDocument()
    expect(screen.getByText('Source')).toBeInTheDocument()
  })

  it('renders_date_in_dd_MM_yyyy_format', async () => {
    await renderLoaded(MANUAL_ENTRY)
    expect(screen.getByText('15/03/2024')).toBeInTheDocument()
  })

  it('renders_price_in_n2', async () => {
    await renderLoaded(MANUAL_ENTRY)
    expect(screen.getByText('120.50')).toBeInTheDocument()
  })

  it('renders_manual_source_label', async () => {
    const table = await renderLoaded(MANUAL_ENTRY)
    const sourceCell = within(dataRows(table)[0]).getByText('Manual').closest('td')
    expect(sourceCell).toHaveClass('price-history-tab__source--manual')
  })

  it('renders_named_provider_source_label', async () => {
    const table = await renderLoaded(AUTOMATIC_ENTRY)
    const sourceCell = within(dataRows(table)[0]).getByText('Google').closest('td')
    expect(sourceCell).toHaveClass('price-history-tab__source--automatic')
  })

  it('edit_and_delete_buttons_shown_for_manual_entry', async () => {
    await renderLoaded(MANUAL_ENTRY)
    expect(screen.getByRole('button', { name: 'Edit price 15/03/2024' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Delete price 15/03/2024' })).toBeInTheDocument()
  })

  it('edit_and_delete_buttons_hidden_for_automatic_entry', async () => {
    await renderLoaded(AUTOMATIC_ENTRY)
    expect(screen.queryByRole('button', { name: /^Edit price\b/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /^Delete price\b/ })).not.toBeInTheDocument()
  })

  it('shows_an_empty_state_instead_of_the_table_when_the_holding_has_no_prices', async () => {
    await renderEmpty()
    expect(screen.getByText('No prices yet — add the first price for this holding.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('form_is_hidden_until_new_price_is_clicked', async () => {
    await renderEmpty()
    expect(screen.queryByLabelText(/^Date/)).not.toBeInTheDocument()
    expect(screen.queryByRole('spinbutton', { name: /^Price/ })).not.toBeInTheDocument()
  })

  it('new_button_shows_blank_form_with_todays_date', async () => {
    await renderEmpty()
    fireEvent.click(screen.getByRole('button', { name: 'New price' }))

    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-08-20')
    expect(priceInput()).toHaveValue(null)
  })

  it('form_title_is_new_price_when_adding', async () => {
    await renderEmpty()
    fireEvent.click(screen.getByRole('button', { name: 'New price' }))

    expect(screen.getByRole('heading', { name: 'New price' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add price' })).toBeInTheDocument()
  })

  it('edit_button_opens_form_populated_from_the_entry', async () => {
    await renderLoaded(MANUAL_ENTRY)
    fireEvent.click(screen.getByRole('button', { name: /^Edit price\b/ }))

    expect(screen.getByRole('heading', { name: 'Edit price' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2024-03-15')
    expect(priceInput()).toHaveValue(120.5)
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
  })

  it('cancel_hides_the_form', async () => {
    await renderEmpty()
    fireEvent.click(screen.getByRole('button', { name: 'New price' }))
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(screen.queryByLabelText(/^Date/)).not.toBeInTheDocument()
  })

  it('editing_each_form_field_updates_its_displayed_value', async () => {
    await renderEmpty()
    fireEvent.click(screen.getByRole('button', { name: 'New price' }))

    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2024-05-01' } })
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2024-05-01')

    fireEvent.change(priceInput(), { target: { value: '4.5' } })
    expect(priceInput()).toHaveValue(4.5)
  })

  it('saving_a_new_price_posts_the_payload_and_shows_the_new_row', async () => {
    const created: AssetPriceSnapshotDto = { ...MANUAL_ENTRY, date: '2026-08-18', price: 125.75 }
    const table = await renderLoaded(AUTOMATIC_ENTRY)
    setAssetPriceMock.mockResolvedValue(detailsWith(created, AUTOMATIC_ENTRY))

    fireEvent.click(screen.getByRole('button', { name: 'New price' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-08-18' } })
    fireEvent.change(priceInput(), { target: { value: '125.75' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add price' }))

    expect(await within(table).findByText('18/08/2026')).toBeInTheDocument()
    expect(within(table).getByText('125.75')).toBeInTheDocument()
    expect(setAssetPriceMock).toHaveBeenCalledWith({
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      assetName: 'KLBN4',
      date: '2026-08-18',
      price: 125.75,
      currency: null,
      sourceReference: null,
    })
    expect(screen.queryByLabelText(/^Date/)).not.toBeInTheDocument()
  })

  it('saving_an_edit_updates_the_row_price', async () => {
    const table = await renderLoaded(MANUAL_ENTRY)
    setAssetPriceMock.mockResolvedValue(detailsWith({ ...MANUAL_ENTRY, price: 130 }))

    fireEvent.click(screen.getByRole('button', { name: /^Edit price\b/ }))
    fireEvent.change(priceInput(), { target: { value: '130' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await within(table).findByText('130.00')).toBeInTheDocument()
    expect(within(table).queryByText('120.50')).not.toBeInTheDocument()
    expect(setAssetPriceMock).toHaveBeenCalledWith(expect.objectContaining({ date: '2024-03-15', price: 130 }))
  })

  it('save_button_disabled_while_saving', async () => {
    await renderEmpty()
    setAssetPriceMock.mockReturnValue(new Promise(() => {}))

    fireEvent.click(screen.getByRole('button', { name: 'New price' }))
    fireEvent.change(priceInput(), { target: { value: '50' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add price' }))

    expect(await screen.findByRole('button', { name: 'Saving…' })).toBeDisabled()
  })

  it('save_requires_a_date', async () => {
    await renderEmpty()
    fireEvent.click(screen.getByRole('button', { name: 'New price' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '' } })
    fireEvent.change(priceInput(), { target: { value: '50' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add price' }))

    expect(await screen.findByText('Date is required.')).toBeInTheDocument()
    expect(setAssetPriceMock).not.toHaveBeenCalled()
  })

  it('save_requires_a_positive_price', async () => {
    await renderEmpty()
    fireEvent.click(screen.getByRole('button', { name: 'New price' }))
    fireEvent.change(priceInput(), { target: { value: '0' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add price' }))

    expect(await screen.findByText('Price must be a positive number.')).toBeInTheDocument()
    expect(setAssetPriceMock).not.toHaveBeenCalled()
  })

  it('renders_save_error_below_form', async () => {
    await renderEmpty()
    setAssetPriceMock.mockRejectedValue(new Error('Failed'))

    fireEvent.click(screen.getByRole('button', { name: 'New price' }))
    fireEvent.change(priceInput(), { target: { value: '50' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add price' }))

    expect(await screen.findByText('Failed')).toBeInTheDocument()
    expect(priceInput()).toHaveValue(50)
  })

  it('delete_button_calls_the_api_and_removes_the_row_after_confirmation', async () => {
    const table = await renderLoaded(MANUAL_ENTRY, AUTOMATIC_ENTRY)
    deleteAssetPriceMock.mockResolvedValue(detailsWith(AUTOMATIC_ENTRY))

    const dialog = await requestDelete(MANUAL_NAME)
    expect(dialog).toHaveAccessibleDescription(`Delete the ${MANUAL_NAME}? This cannot be undone.`)
    fireEvent.click(within(dialog).getByRole('button', { name: 'Delete', hidden: true }))

    await waitFor(() => expect(within(table).queryByText('15/03/2024')).not.toBeInTheDocument())
    expect(within(table).getByText('10/01/2024')).toBeInTheDocument()
    expect(deleteAssetPriceMock).toHaveBeenCalledWith({
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      assetName: 'KLBN4',
      date: '2024-03-15T00:00:00',
    })
  })

  it('delete_button_does_nothing_when_the_user_cancels', async () => {
    const table = await renderLoaded(MANUAL_ENTRY)

    const dialog = await requestDelete(MANUAL_NAME)
    fireEvent.click(within(dialog).getByRole('button', { name: 'Cancel', hidden: true }))

    await waitFor(() => expect(rowButton(`Delete ${MANUAL_NAME}`)).toHaveFocus())
    expect(deleteAssetPriceMock).not.toHaveBeenCalled()
    expect(within(table).getByText('15/03/2024')).toBeInTheDocument()
  })

  it('shows_the_row_as_deleting_and_keeps_focus_on_it_while_the_delete_is_in_progress', async () => {
    deleteAssetPriceMock.mockReturnValue(new Promise(() => {}))
    await renderLoaded(MANUAL_ENTRY, EARLIER_MANUAL_ENTRY)

    await confirmDelete(MANUAL_NAME)

    await waitFor(() => expect(rowButton(`Deleting ${MANUAL_NAME}`)).toHaveFocus())
    expect(rowButton(`Deleting ${MANUAL_NAME}`)).toHaveAttribute('aria-disabled', 'true')
  })

  it('blocks_a_second_delete_while_one_is_in_progress', async () => {
    deleteAssetPriceMock.mockReturnValue(new Promise(() => {}))
    await renderLoaded(MANUAL_ENTRY, EARLIER_MANUAL_ENTRY)
    await confirmDelete(MANUAL_NAME)

    const blocked = await screen.findByRole('button', { name: `Delete ${EARLIER_MANUAL_NAME}`, hidden: true })
    await waitFor(() => expect(blocked).toHaveAttribute('aria-disabled', 'true'))
    fireEvent.click(blocked)
    expect(screen.queryByRole('dialog', { name: /^Delete/, hidden: true })).not.toBeInTheDocument()

    expect(deleteAssetPriceMock).toHaveBeenCalledOnce()
  })

  it('moves_focus_to_the_next_row_delete_after_a_delete', async () => {
    deleteAssetPriceMock.mockResolvedValue(detailsWith(EARLIER_MANUAL_ENTRY))
    await renderLoaded(MANUAL_ENTRY, EARLIER_MANUAL_ENTRY)

    await confirmDelete(MANUAL_NAME)

    await waitFor(() => expect(rowButton(`Delete ${EARLIER_MANUAL_NAME}`)).toHaveFocus())
  })

  it('moves_focus_to_new_price_and_shows_the_empty_state_after_deleting_the_only_row', async () => {
    deleteAssetPriceMock.mockResolvedValue(detailsWith())
    await renderLoaded(MANUAL_ENTRY)

    await confirmDelete(MANUAL_NAME)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New price' })).toHaveFocus())
    expect(screen.getByText(/^No prices yet/)).toBeInTheDocument()
  })

  it('closes_the_edit_form_when_the_price_being_edited_is_deleted', async () => {
    deleteAssetPriceMock.mockResolvedValue(detailsWith(EARLIER_MANUAL_ENTRY))
    await renderLoaded(MANUAL_ENTRY, EARLIER_MANUAL_ENTRY)
    fireEvent.click(rowButton(`Edit ${MANUAL_NAME}`))
    await screen.findByRole('heading', { name: 'Edit price' })

    await confirmDelete(MANUAL_NAME)

    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Edit price', hidden: true })).not.toBeInTheDocument())
  })

  it('announces_a_failed_delete_and_moves_focus_to_it', async () => {
    deleteAssetPriceMock.mockRejectedValue(new Error('Failed to delete'))
    const table = await renderLoaded(MANUAL_ENTRY)

    await confirmDelete(MANUAL_NAME)

    const alert = await findErrorAlert({ hidden: true })
    expect(alert).toHaveTextContent('Failed to delete')
    await waitFor(() => expect(alert).toHaveFocus())
    expect(within(table).getByText('15/03/2024')).toBeInTheDocument()
  })

  it('renders_all_filter_buttons', async () => {
    await renderEmpty()
    for (const name of ['This month', 'Last 3 months', 'Last 6 months', 'Last 12 months', 'YTD', 'All time']) {
      expect(screen.getByRole('tab', { name })).toBeInTheDocument()
    }
  })

  it('active_filter_is_last_12_months_by_default', async () => {
    await renderEmpty()
    expect(screen.getByRole('tab', { name: 'Last 12 months' })).toHaveAttribute('aria-selected', 'true')
  })

  it('clicking_filter_selects_that_tab', async () => {
    await renderEmpty()
    fireEvent.click(screen.getByRole('tab', { name: 'Last 3 months' }))

    expect(screen.getByRole('tab', { name: 'Last 3 months' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Last 12 months' })).toHaveAttribute('aria-selected', 'false')
  })

  it('renders_chart_legend_for_all_four_series', async () => {
    await renderEmpty()
    expect(screen.getByTestId('chart-legend')).toHaveTextContent('Automatic,Manual,Buy,Sell')
  })

  it('clicking_price_header_sorts_rows_ascending_then_descending', async () => {
    const table = await renderLoaded(MANUAL_ENTRY, AUTOMATIC_ENTRY)

    fireEvent.click(screen.getByRole('button', { name: 'Price' }))
    let rows = dataRows(table)
    expect(within(rows[0]).getByText('120.50')).toBeInTheDocument()
    expect(within(rows[1]).getByText('350.00')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Price' }))
    rows = dataRows(table)
    expect(within(rows[0]).getByText('350.00')).toBeInTheDocument()
    expect(within(rows[1]).getByText('120.50')).toBeInTheDocument()
  })

  it('defaults_to_sorting_by_date_descending_with_the_header_showing_the_active_sort', async () => {
    const table = await renderLoaded(AUTOMATIC_ENTRY, MANUAL_ENTRY)
    const dateHeaderButton = within(table).getByRole('button', { name: 'Date' })
    const dateHeader = dateHeaderButton.closest('th')

    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')
    let rows = dataRows(table)
    expect(within(rows[0]).getByText('15/03/2024')).toBeInTheDocument()
    expect(within(rows[1]).getByText('10/01/2024')).toBeInTheDocument()

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'none')

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'ascending')
    rows = dataRows(table)
    expect(within(rows[0]).getByText('10/01/2024')).toBeInTheDocument()
    expect(within(rows[1]).getByText('15/03/2024')).toBeInTheDocument()

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')
  })

  it('clicking_source_header_sorts_rows_by_source', async () => {
    const table = await renderLoaded(MANUAL_ENTRY, AUTOMATIC_ENTRY)
    fireEvent.click(screen.getByRole('button', { name: 'Source' }))

    const rows = dataRows(table)
    expect(rows).toHaveLength(2)
    expect(within(rows[0]).getByText('Google')).toBeInTheDocument()
    expect(within(rows[1]).getByText('Manual')).toBeInTheDocument()
  })
})
