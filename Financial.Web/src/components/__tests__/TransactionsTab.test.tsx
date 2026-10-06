import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ReactNode } from 'react'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type {
  AssetDetailsDto,
  OpenLotDto,
  SelectedNode,
  TransactionDto,
  TransactionSummaryItemDto,
  TransactionTypeEffectDto,
} from '../../api/types'
import { pinDate } from '../../test-utils/pinDate'
import { renderWithSelectedNode } from '../../test-utils/renderWithSelectedNode'
import TransactionsTab from '../TransactionsTab'

vi.mock('recharts', () => ({
  BarChart: ({ children }: { children: ReactNode }) => <div data-testid="bar-chart">{children}</div>,
  LineChart: ({ children }: { children: ReactNode }) => <div data-testid="line-chart">{children}</div>,
  Bar: () => null,
  Line: () => null,
  LabelList: () => null,
  XAxis: () => null,
  YAxis: () => null,
  CartesianGrid: () => null,
  Tooltip: () => null,
  ResponsiveContainer: ({ children }: { children: ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
}))

const {
  getAssetDetailsMock,
  getTransactionsByBrokerMock,
  getTransactionsByPortfolioMock,
  getTransactionTypeEffectsMock,
  getOpenLotsMock,
  addTransactionMock,
  updateTransactionMock,
  deleteTransactionMock,
} = vi.hoisted(() => ({
  getAssetDetailsMock: vi.fn<FinancialApiClient['getAssetDetails']>(),
  getTransactionsByBrokerMock: vi.fn<FinancialApiClient['getTransactionsByBroker']>(),
  getTransactionsByPortfolioMock: vi.fn<FinancialApiClient['getTransactionsByPortfolio']>(),
  getTransactionTypeEffectsMock: vi.fn<FinancialApiClient['getTransactionTypeEffects']>(),
  getOpenLotsMock: vi.fn<FinancialApiClient['getOpenLots']>(),
  addTransactionMock: vi.fn<FinancialApiClient['addTransaction']>(),
  updateTransactionMock: vi.fn<FinancialApiClient['updateTransaction']>(),
  deleteTransactionMock: vi.fn<FinancialApiClient['deleteTransaction']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAssetDetails: getAssetDetailsMock,
    getTransactionsByBroker: getTransactionsByBrokerMock,
    getTransactionsByPortfolio: getTransactionsByPortfolioMock,
    getTransactionTypeEffects: getTransactionTypeEffectsMock,
    getOpenLots: getOpenLotsMock,
    addTransaction: addTransactionMock,
    updateTransaction: updateTransactionMock,
    deleteTransaction: deleteTransactionMock,
  } as Partial<FinancialApiClient>,
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

const BROKER_NODE: SelectedNode = { nodeType: 'Broker', brokerName: 'XPI' }

const PORTFOLIO_NODE: SelectedNode = { nodeType: 'Portfolio', brokerName: 'XPI', portfolioName: 'Acoes' }

const TRANSACTION_BUY: TransactionDto = {
  id: 'aaa',
  date: '2024-03-15T00:00:00',
  type: 'Buy',
  quantity: 100,
  unitPrice: 4.2,
  fees: 0.5,
  withheld: 0,
  netCash: -420.5,
  currency: 'GBP',
  fxRateSnapshot: null,
}

const TRANSACTION_SELL: TransactionDto = {
  id: 'bbb',
  date: '2024-01-10T00:00:00',
  type: 'Sell',
  quantity: 50,
  unitPrice: 5.0,
  fees: 1.0,
  withheld: 0,
  netCash: 251.0,
  currency: 'GBP',
  fxRateSnapshot: null,
}

const TRANSACTION_NEW: TransactionDto = {
  id: 'ccc',
  date: '2024-05-01T00:00:00',
  type: 'Sell',
  quantity: 10,
  unitPrice: 4.5,
  fees: 0.1,
  withheld: 0,
  netCash: 44.9,
  currency: 'GBP',
  fxRateSnapshot: null,
}

const TYPE_EFFECTS: TransactionTypeEffectDto[] = [
  { type: 'Buy', quantityEffect: 'Increase', cashEffect: 'Outflow' },
  { type: 'Sell', quantityEffect: 'Decrease', cashEffect: 'Inflow' },
  { type: 'Fee', quantityEffect: 'None', cashEffect: 'Outflow' },
]

const OPEN_LOT: OpenLotDto = {
  sourceTransactionId: 'lot-a',
  date: '2024-06-01T00:00:00',
  remainingQuantity: 15,
  unitCost: 12.5,
}

const SUMMARY_ITEMS: TransactionSummaryItemDto[] = [
  { assetName: 'KLBN4', date: '2024-02-10T00:00:00', netCash: -169.5, type: 'Buy' },
]

function assetWith(
  transactions: TransactionDto[],
  costBasisMethod: AssetDetailsDto['costBasisMethod'] = 'AverageCost',
): AssetDetailsDto {
  return {
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
    priceAsOfDate: null,
    marketStatus: 'Current',
    priceOnlyReturn: null,
    totalReturn: null,
    transactions,
    credits: [],
    priceSnapshots: [],
    cashFlowsWithCredits: [],
    cashFlowsWithoutCredits: [],
    disposalRecords: [],
    corporateActions: [],
    costBasisMethod,
    taxJurisdictions: [],
  }
}

async function renderAssetTab(transactions: TransactionDto[] = [TRANSACTION_BUY], costBasisMethod?: AssetDetailsDto['costBasisMethod']) {
  getAssetDetailsMock.mockResolvedValue(assetWith(transactions, costBasisMethod))
  renderWithSelectedNode(<TransactionsTab />, ASSET_NODE)
  await screen.findByRole('table')
}

async function openNewForm() {
  fireEvent.click(screen.getByRole('button', { name: 'New transaction' }))
  await screen.findByRole('heading', { name: 'New transaction' })
}

function fillValidSale() {
  fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2024-05-01' } })
  fireEvent.change(screen.getByLabelText('Type'), { target: { value: 'Sell' } })
  fireEvent.change(screen.getByLabelText(/^Quantity/), { target: { value: '10' } })
  fireEvent.change(screen.getByLabelText(/^Unit Price/), { target: { value: '4.5' } })
  fireEvent.change(screen.getByLabelText('Fees'), { target: { value: '0.1' } })
}

function dataRows() {
  return within(screen.getByRole('table')).getAllByRole('row').slice(1)
}

function rowTypes() {
  return dataRows().map((row) => (within(row).queryByText('Buy') ? 'Buy' : 'Sell'))
}

describe('TransactionsTab', () => {
  beforeEach(() => {
    pinDate('2024-06-01T12:00:00+01:00')
    sessionStorage.clear()
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    getAssetDetailsMock.mockReset()
    getTransactionsByBrokerMock.mockReset()
    getTransactionsByPortfolioMock.mockReset()
    getTransactionTypeEffectsMock.mockReset().mockResolvedValue(TYPE_EFFECTS)
    getOpenLotsMock.mockReset().mockResolvedValue([])
    addTransactionMock.mockReset()
    updateTransactionMock.mockReset()
    deleteTransactionMock.mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  it('renders_loading_state', async () => {
    getAssetDetailsMock.mockReturnValue(new Promise(() => {}))
    renderWithSelectedNode(<TransactionsTab />, ASSET_NODE)
    expect(await screen.findByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry_and_refetches_on_try_again', async () => {
    getAssetDetailsMock.mockRejectedValueOnce(new Error('Network error'))
    getAssetDetailsMock.mockResolvedValue(assetWith([TRANSACTION_BUY]))
    renderWithSelectedNode(<TransactionsTab />, ASSET_NODE)

    expect(await screen.findByText('Network error')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('15/03/2024')).toBeInTheDocument()
    expect(getAssetDetailsMock).toHaveBeenCalledTimes(2)
    expect(screen.queryByText('Network error')).not.toBeInTheDocument()
  })

  it.each([
    ['Broker', BROKER_NODE, getTransactionsByBrokerMock],
    ['Portfolio', PORTFOLIO_NODE, getTransactionsByPortfolioMock],
  ])('renders_chart_only_for_%s_node_selection', async (_name, node, mock) => {
    mock.mockResolvedValue(SUMMARY_ITEMS)
    renderWithSelectedNode(<TransactionsTab />, node)
    await waitFor(() => expect(mock).toHaveBeenCalled())

    expect(await screen.findByText('Net Invested by Month')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'New transaction' })).not.toBeInTheDocument()
  })

  it.each([
    ['Broker', BROKER_NODE, getTransactionsByBrokerMock],
    ['Portfolio', PORTFOLIO_NODE, getTransactionsByPortfolioMock],
  ])('renders_error_state_with_retry_on_%s_fetch_failure', async (_name, node, mock) => {
    mock.mockRejectedValueOnce(new Error('Unable to load transactions'))
    mock.mockResolvedValue(SUMMARY_ITEMS)
    renderWithSelectedNode(<TransactionsTab />, node)

    expect(await screen.findByText('Unable to load transactions')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('Net Invested by Month')).toBeInTheDocument()
    expect(mock).toHaveBeenCalledTimes(2)
  })

  it('renders_chart_above_table_for_asset_node_selection', async () => {
    await renderAssetTab()
    expect(screen.getByText('Net Invested by Month')).toBeInTheDocument()
    expect(screen.getByRole('table')).toBeInTheDocument()
    expect(screen.getByText('15/03/2024')).toBeInTheDocument()
  })

  it('renders_six_period_filter_buttons', async () => {
    await renderAssetTab()
    for (const name of ['This month', 'Last 3 months', 'Last 6 months', 'Last 12 months', 'YTD', 'All time']) {
      expect(screen.getByRole('tab', { name })).toBeInTheDocument()
    }
  })

  it('renders_bar_line_toggle_defaulting_to_bar', async () => {
    await renderAssetTab()
    expect(screen.getByRole('tab', { name: 'Bar' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Line' })).toHaveAttribute('aria-selected', 'false')
    expect(screen.getByTestId('bar-chart')).toBeInTheDocument()
  })

  it('clicking_line_toggle_selects_line_chart', async () => {
    await renderAssetTab()
    fireEvent.click(screen.getByRole('tab', { name: 'Line' }))
    expect(screen.getByRole('tab', { name: 'Line' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Bar' })).toHaveAttribute('aria-selected', 'false')
    expect(screen.getByTestId('line-chart')).toBeInTheDocument()
  })

  it('clicking_filter_button_selects_that_period', async () => {
    await renderAssetTab()
    expect(screen.getByRole('tab', { name: 'Last 12 months' })).toHaveAttribute('aria-selected', 'true')
    fireEvent.click(screen.getByRole('tab', { name: 'YTD' }))
    expect(screen.getByRole('tab', { name: 'YTD' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Last 12 months' })).toHaveAttribute('aria-selected', 'false')
  })

  it('renders_table_with_correct_columns', async () => {
    await renderAssetTab()
    for (const header of ['Date', 'Type', 'Quantity', 'Unit Price', 'Fees', 'Withheld', 'Net']) {
      expect(screen.getByText(header)).toBeInTheDocument()
    }
  })

  it('renders_date_in_dd_MM_yyyy_format', async () => {
    await renderAssetTab()
    expect(screen.getByText('15/03/2024')).toBeInTheDocument()
  })

  it.each([
    [TRANSACTION_BUY, 'Buy', 'transactions-tab__type--buy'],
    [TRANSACTION_SELL, 'Sell', 'transactions-tab__type--sell'],
  ])('renders_%#_transaction_type_%s_with_its_class', async (transaction, label, typeClass) => {
    await renderAssetTab([transaction])
    expect(screen.getByText(label)).toHaveClass(typeClass)
  })

  it('renders_quantity_with_8_decimal_places', async () => {
    await renderAssetTab()
    expect(screen.getByText('100.00000000')).toBeInTheDocument()
  })

  it('renders_total_in_bold', async () => {
    await renderAssetTab()
    expect(screen.getByText('-420.50')).toHaveClass('transactions-tab__total')
  })

  it('empty_table_renders_no_rows', async () => {
    await renderAssetTab([])
    expect(dataRows()).toHaveLength(0)
  })

  it('new_button_shows_the_new_transaction_form', async () => {
    await renderAssetTab()
    expect(screen.queryByRole('heading', { name: 'New transaction' })).not.toBeInTheDocument()
    await openNewForm()
  })

  it('renders_form_fields_when_form_visible', async () => {
    await renderAssetTab()
    await openNewForm()
    expect(screen.getByLabelText(/^Date/)).toBeInTheDocument()
    expect(screen.getByLabelText('Type')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Quantity/)).toBeInTheDocument()
    expect(screen.getByLabelText(/^Unit Price/)).toBeInTheDocument()
    expect(screen.getByLabelText('Fees')).toBeInTheDocument()
    expect(screen.getByLabelText('Withheld')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2024-06-01')
  })

  it('cancel_hides_the_form', async () => {
    await renderAssetTab()
    await openNewForm()
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(screen.queryByRole('heading', { name: 'New transaction' })).not.toBeInTheDocument()
  })

  it('hides_quantity_and_unit_price_when_type_has_no_quantity_effect', async () => {
    await renderAssetTab()
    await openNewForm()
    fireEvent.change(screen.getByLabelText('Type'), { target: { value: 'Fee' } })
    await waitFor(() => expect(screen.queryByLabelText(/^Quantity/)).not.toBeInTheDocument())
    expect(screen.queryByLabelText(/^Unit Price/)).not.toBeInTheDocument()
  })

  it('type_select_includes_every_new_transaction_type', async () => {
    await renderAssetTab()
    await openNewForm()
    const select = screen.getByLabelText('Type') as HTMLSelectElement
    expect(Array.from(select.options).map((o) => o.value)).toEqual([
      'Buy',
      'Sell',
      'Fee',
      'Redemption',
      'TransferIn',
      'TransferOut',
      'CapitalCall',
      'ReturnOfCapital',
    ])
  })

  it('editing_each_form_field_changes_its_displayed_value', async () => {
    await renderAssetTab()
    await openNewForm()

    const fields: [RegExp | string, string, string | number][] = [
      [/^Date/, '2024-05-01', '2024-05-01'],
      ['Type', 'Sell', 'Sell'],
      [/^Quantity/, '10', 10],
      [/^Unit Price/, '4.5', 4.5],
      ['Fees', '0.1', 0.1],
      ['Withheld', '0.2', 0.2],
    ]
    for (const [label, typed, displayed] of fields) {
      fireEvent.change(screen.getByLabelText(label), { target: { value: typed } })
      expect(screen.getByLabelText(label)).toHaveValue(displayed)
    }
  })

  it('edit_icon_opens_the_edit_form_prefilled_with_the_transaction', async () => {
    await renderAssetTab()
    fireEvent.click(screen.getByRole('button', { name: 'Edit transaction' }))

    expect(await screen.findByRole('heading', { name: 'Edit transaction' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2024-03-15')
    expect(screen.getByLabelText('Type')).toHaveValue('Buy')
    expect(screen.getByLabelText(/^Quantity/)).toHaveValue(100)
    expect(screen.getByLabelText(/^Unit Price/)).toHaveValue(4.2)
    expect(screen.getByLabelText('Fees')).toHaveValue(0.5)
    expect(screen.getByLabelText('Withheld')).toHaveValue(0)
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
  })

  it('save_adds_the_transaction_and_shows_the_new_row_after_reload', async () => {
    addTransactionMock.mockResolvedValue(assetWith([TRANSACTION_BUY, TRANSACTION_NEW]))
    await renderAssetTab()
    await openNewForm()
    fillValidSale()

    fireEvent.click(screen.getByRole('button', { name: 'Add transaction' }))

    expect(await screen.findByText('01/05/2024')).toBeInTheDocument()
    expect(addTransactionMock).toHaveBeenCalledWith({
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      assetName: 'KLBN4',
      date: '2024-05-01',
      type: 'Sell',
      quantity: 10,
      unitPrice: 4.5,
      fees: 0.1,
      withheld: 0,
      specificLotAllocations: null,
    })
    expect(screen.queryByRole('heading', { name: 'New transaction' })).not.toBeInTheDocument()
  })

  it('save_in_edit_mode_updates_the_transaction', async () => {
    updateTransactionMock.mockResolvedValue(assetWith([{ ...TRANSACTION_BUY, quantity: 120 }]))
    await renderAssetTab()
    fireEvent.click(screen.getByRole('button', { name: 'Edit transaction' }))
    await screen.findByRole('heading', { name: 'Edit transaction' })
    fireEvent.change(screen.getByLabelText(/^Quantity/), { target: { value: '120' } })

    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('120.00000000')).toBeInTheDocument()
    expect(updateTransactionMock).toHaveBeenCalledWith({
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      assetName: 'KLBN4',
      id: 'aaa',
      date: '2024-03-15',
      type: 'Buy',
      quantity: 120,
      unitPrice: 4.2,
      fees: 0.5,
      withheld: 0,
    })
    expect(addTransactionMock).not.toHaveBeenCalled()
  })

  it('save_without_quantity_and_unit_price_shows_validation_messages_and_does_not_call_the_api', async () => {
    await renderAssetTab()
    await openNewForm()

    fireEvent.click(screen.getByRole('button', { name: 'Add transaction' }))

    expect(await screen.findByText('Quantity must be a positive number')).toBeInTheDocument()
    expect(screen.getByText('Unit Price must be a positive number')).toBeInTheDocument()
    expect(addTransactionMock).not.toHaveBeenCalled()
  })

  it('save_without_a_date_shows_the_date_validation_message', async () => {
    await renderAssetTab()
    await openNewForm()
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '' } })

    fireEvent.click(screen.getByRole('button', { name: 'Add transaction' }))

    expect(await screen.findByText('Date is required')).toBeInTheDocument()
    expect(addTransactionMock).not.toHaveBeenCalled()
  })

  it('save_button_disabled_while_saving', async () => {
    addTransactionMock.mockReturnValue(new Promise(() => {}))
    await renderAssetTab()
    await openNewForm()
    fillValidSale()

    fireEvent.click(screen.getByRole('button', { name: 'Add transaction' }))

    expect(await screen.findByRole('button', { name: 'Saving...' })).toBeDisabled()
  })

  it('renders_save_error_below_form', async () => {
    addTransactionMock.mockRejectedValue(new Error('Failed to save'))
    await renderAssetTab()
    await openNewForm()
    fillValidSale()

    fireEvent.click(screen.getByRole('button', { name: 'Add transaction' }))

    expect(await screen.findByText('Failed to save')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'New transaction' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add transaction' })).not.toBeDisabled()
  })

  it('delete_icon_deletes_the_transaction_after_the_user_confirms', async () => {
    deleteTransactionMock.mockResolvedValue(assetWith([TRANSACTION_SELL]))
    await renderAssetTab([TRANSACTION_BUY, TRANSACTION_SELL])

    fireEvent.click(within(dataRows()[0]).getByRole('button', { name: 'Delete transaction' }))

    await waitFor(() => expect(screen.queryByText('15/03/2024')).not.toBeInTheDocument())
    expect(screen.getByText('10/01/2024')).toBeInTheDocument()
    expect(deleteTransactionMock).toHaveBeenCalledWith({
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      assetName: 'KLBN4',
      id: 'aaa',
    })
  })

  it('delete_icon_does_not_delete_when_the_user_cancels_the_confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    await renderAssetTab()

    fireEvent.click(screen.getByRole('button', { name: 'Delete transaction' }))

    expect(deleteTransactionMock).not.toHaveBeenCalled()
    expect(screen.getByText('15/03/2024')).toBeInTheDocument()
  })

  it('renders_delete_error_below_table', async () => {
    deleteTransactionMock.mockRejectedValue(new Error('Failed to delete'))
    await renderAssetTab()

    fireEvent.click(screen.getByRole('button', { name: 'Delete transaction' }))

    expect(await screen.findByText('Failed to delete')).toBeInTheDocument()
    expect(screen.getByText('15/03/2024')).toBeInTheDocument()
  })

  it('does_not_show_the_lot_allocation_picker_for_a_sale_on_a_non_specific_id_asset', async () => {
    await renderAssetTab([TRANSACTION_BUY], 'AverageCost')
    await openNewForm()
    fireEvent.change(screen.getByLabelText('Type'), { target: { value: 'Sell' } })

    expect(screen.queryByText(/Allocated/)).not.toBeInTheDocument()
    expect(getOpenLotsMock).not.toHaveBeenCalled()
  })

  it('shows_the_lot_allocation_picker_for_a_sale_on_a_specific_id_asset', async () => {
    getOpenLotsMock.mockResolvedValue([OPEN_LOT])
    await renderAssetTab([TRANSACTION_BUY], 'SpecificId')
    await openNewForm()
    fireEvent.change(screen.getByLabelText('Type'), { target: { value: 'Sell' } })
    fireEvent.change(screen.getByLabelText(/^Quantity/), { target: { value: '10' } })

    expect(await screen.findByText('15.00000000')).toBeInTheDocument()
    expect(screen.getByText(/Allocated 0.00000000 of 10.00000000/)).toBeInTheDocument()
    expect(getOpenLotsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4', 'active')
  })

  it('does_not_show_the_lot_allocation_picker_when_editing_a_specific_id_sale', async () => {
    await renderAssetTab([TRANSACTION_SELL], 'SpecificId')
    fireEvent.click(screen.getByRole('button', { name: 'Edit transaction' }))
    await screen.findByRole('heading', { name: 'Edit transaction' })

    expect(screen.queryByText(/Allocated/)).not.toBeInTheDocument()
    expect(getOpenLotsMock).not.toHaveBeenCalled()
  })

  async function openSpecificIdSaleForm() {
    getOpenLotsMock.mockResolvedValue([OPEN_LOT])
    await renderAssetTab([TRANSACTION_BUY], 'SpecificId')
    await openNewForm()
    fillValidSale()
    return screen.findByRole('spinbutton', { name: /Allocate quantity/ })
  }

  it('editing_a_lot_allocation_input_changes_its_value_and_the_allocated_summary', async () => {
    const allocationInput = await openSpecificIdSaleForm()

    fireEvent.change(allocationInput, { target: { value: '4' } })

    expect(screen.getByRole('spinbutton', { name: /Allocate quantity/ })).toHaveValue(4)
    expect(screen.getByText(/Allocated 4.00000000 of 10.00000000/)).toBeInTheDocument()
  })

  it('disables_Save_until_the_allocation_exactly_matches_the_sale_quantity', async () => {
    const allocationInput = await openSpecificIdSaleForm()

    fireEvent.change(allocationInput, { target: { value: '4' } })

    expect(screen.getByRole('button', { name: 'Add transaction' })).toBeDisabled()
  })

  it('enables_Save_once_the_allocation_exactly_matches_the_sale_quantity_and_sends_it', async () => {
    addTransactionMock.mockResolvedValue(assetWith([TRANSACTION_BUY, TRANSACTION_NEW], 'SpecificId'))
    const allocationInput = await openSpecificIdSaleForm()

    fireEvent.change(allocationInput, { target: { value: '10' } })

    const saveButton = screen.getByRole('button', { name: 'Add transaction' })
    expect(saveButton).not.toBeDisabled()
    fireEvent.click(saveButton)

    expect(await screen.findByText('01/05/2024')).toBeInTheDocument()
    expect(addTransactionMock).toHaveBeenCalledWith(
      expect.objectContaining({
        type: 'Sell',
        quantity: 10,
        specificLotAllocations: [{ sourceTransactionId: 'lot-a', quantity: 10 }],
      }),
    )
  })

  it('clicking_total_header_sorts_rows_ascending_then_descending', async () => {
    await renderAssetTab([TRANSACTION_BUY, TRANSACTION_SELL])

    fireEvent.click(screen.getByRole('button', { name: 'Net' }))
    let rows = dataRows()
    expect(within(rows[0]).getByText('-420.50')).toBeInTheDocument()
    expect(within(rows[1]).getByText('251.00')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Net' }))
    rows = dataRows()
    expect(within(rows[0]).getByText('251.00')).toBeInTheDocument()
    expect(within(rows[1]).getByText('-420.50')).toBeInTheDocument()
  })

  it('defaults_to_sorting_by_date_descending_with_the_header_showing_the_active_sort', async () => {
    await renderAssetTab([TRANSACTION_SELL, TRANSACTION_BUY])
    const table = screen.getByRole('table')
    const dateHeaderButton = within(table).getByRole('button', { name: 'Date' })
    const dateHeader = dateHeaderButton.closest('th')

    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')
    let rows = dataRows()
    expect(within(rows[0]).getByText('15/03/2024')).toBeInTheDocument()
    expect(within(rows[1]).getByText('10/01/2024')).toBeInTheDocument()

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'none')

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'ascending')
    rows = dataRows()
    expect(within(rows[0]).getByText('10/01/2024')).toBeInTheDocument()
    expect(within(rows[1]).getByText('15/03/2024')).toBeInTheDocument()

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')
  })

  it.each([
    ['Type', ['Buy', 'Sell']],
    ['Quantity', ['Sell', 'Buy']],
    ['Unit Price', ['Buy', 'Sell']],
    ['Fees', ['Buy', 'Sell']],
  ])('clicking_%s_header_sorts_rows_ascending_then_descending', async (header, ascendingTypes) => {
    await renderAssetTab([TRANSACTION_BUY, TRANSACTION_SELL])

    fireEvent.click(screen.getByRole('button', { name: header }))
    expect(rowTypes()).toEqual(ascendingTypes)

    fireEvent.click(screen.getByRole('button', { name: header }))
    expect(rowTypes()).toEqual([...ascendingTypes].reverse())
  })

  it('shows_the_fx_provenance_affordance_for_a_transaction_with_a_captured_snapshot', async () => {
    await renderAssetTab([
      {
        ...TRANSACTION_BUY,
        currency: 'BRL',
        fxRateSnapshot: { toCurrency: 'GBP', rate: 0.146, source: 'Frankfurter', retrievedAt: '2026-07-01T08:00:00Z' },
      },
    ])

    expect(screen.getByRole('button', { name: 'FX conversion details' })).toBeInTheDocument()
  })

  it('does_not_show_the_fx_provenance_affordance_for_a_transaction_without_a_captured_snapshot', async () => {
    await renderAssetTab([{ ...TRANSACTION_BUY, currency: 'GBP', fxRateSnapshot: null }])

    expect(screen.queryByRole('button', { name: 'FX conversion details' })).not.toBeInTheDocument()
  })
})
