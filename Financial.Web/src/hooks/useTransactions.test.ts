import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { AssetDetailsDto, SelectedNode, TransactionDto, TransactionSummaryItemDto } from '../api/types'
import { createSelectedNodeWrapper } from '../test-utils/selectedNodeTestWrapper'
import { buildMonthlyNetInvested, useTransactions } from './useTransactions'

const getAssetDetailsMock = vi.fn<FinancialApiClient['getAssetDetails']>()
const addTransactionMock = vi.fn<FinancialApiClient['addTransaction']>()
const updateTransactionMock = vi.fn<FinancialApiClient['updateTransaction']>()
const deleteTransactionMock = vi.fn<FinancialApiClient['deleteTransaction']>()
const getTransactionsByBrokerMock = vi.fn<FinancialApiClient['getTransactionsByBroker']>()
const getTransactionsByPortfolioMock = vi.fn<FinancialApiClient['getTransactionsByPortfolio']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getAssetDetails: getAssetDetailsMock,
    addTransaction: addTransactionMock,
    updateTransaction: updateTransactionMock,
    deleteTransaction: deleteTransactionMock,
    getTransactionsByBroker: getTransactionsByBrokerMock,
    getTransactionsByPortfolio: getTransactionsByPortfolioMock,
  }),
}))

vi.stubGlobal('confirm', vi.fn(() => true))

const ASSET_NODE: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetName: 'KLBN4',
  ticker: 'KLBN4',
  exchange: 'BVMF',
  isActive: true,
}

const PORTFOLIO_NODE: SelectedNode = {
  nodeType: 'Portfolio',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
}

const BROKER_NODE: SelectedNode = {
  nodeType: 'Broker',
  brokerName: 'XPI',
}

const SUMMARY_ITEM_A: TransactionSummaryItemDto = {
  assetName: 'KLBN4',
  date: '2024-03-15T00:00:00',
  type: 'Buy',
  totalPrice: 420.5,
}

const SUMMARY_ITEM_B: TransactionSummaryItemDto = {
  assetName: 'PETR4',
  date: '2024-01-10T00:00:00',
  type: 'Sell',
  totalPrice: 251.0,
}

const TRANSACTION_A: TransactionDto = {
  id: 'aaa',
  date: '2024-03-15T00:00:00',
  type: 'Buy',
  quantity: 100,
  unitPrice: 4.2,
  fees: 0.5,
  totalPrice: 420.5,
}

const TRANSACTION_B: TransactionDto = {
  id: 'bbb',
  date: '2024-01-10T00:00:00',
  type: 'Sell',
  quantity: 50,
  unitPrice: 5.0,
  fees: 1.0,
  totalPrice: 251.0,
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
  quantity: 100,
  averagePrice: 20,
  isActive: true,
  totalBought: 2000,
  totalSold: 0,
  totalCredits: 50,
  transactions: [TRANSACTION_A, TRANSACTION_B],
  credits: [],
  cashFlowsWithCredits: [],
  cashFlowsWithoutCredits: [],
}

describe('useTransactions', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset()
    addTransactionMock.mockReset()
    updateTransactionMock.mockReset()
    deleteTransactionMock.mockReset()
    getTransactionsByBrokerMock.mockReset().mockResolvedValue([])
    getTransactionsByPortfolioMock.mockReset().mockResolvedValue([])
    vi.mocked(window.confirm).mockReturnValue(true)
  })

  it('returns_initial_empty_state', () => {
    const { wrapper } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    expect(result.current.isLoading).toBe(false)
    expect(result.current.asset).toBeNull()
    expect(result.current.error).toBeNull()
    expect(result.current.transactions).toEqual([])
    expect(result.current.isFormVisible).toBe(false)
  })

  it('fetches_asset_details_on_asset_selection', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => {
      expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4')
    })
  })

  it('resets_state_when_non_asset_node_selected', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.asset).toBeNull())
    expect(getAssetDetailsMock).toHaveBeenCalledTimes(1)
  })

  it('increments_retry_and_refetches_on_retry', async () => {
    getAssetDetailsMock.mockRejectedValueOnce(new Error('Network error'))
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.error).toBe('Network error'))
    act(() => result.current.retry())
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    expect(getAssetDetailsMock).toHaveBeenCalledTimes(2)
  })

  it('sorts_transactions_by_date_descending', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.transactions.length).toBe(2))
    expect(result.current.transactions[0].id).toBe('aaa')
    expect(result.current.transactions[1].id).toBe('bbb')
  })

  it('show_new_form_opens_blank_form', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    act(() => result.current.showNewForm())
    expect(result.current.isFormVisible).toBe(true)
    expect(result.current.editingId).toBeNull()
    expect(result.current.formDate).toBe('')
    expect(result.current.formType).toBe('Buy')
    expect(result.current.formQuantity).toBe('')
    expect(result.current.formUnitPrice).toBe('')
    expect(result.current.formFees).toBe('')
  })

  it('show_edit_form_populates_fields', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    act(() => result.current.showEditForm(TRANSACTION_A))
    expect(result.current.isFormVisible).toBe(true)
    expect(result.current.editingId).toBe('aaa')
    expect(result.current.formDate).toBe('2024-03-15')
    expect(result.current.formType).toBe('Buy')
    expect(result.current.formQuantity).toBe('100')
    expect(result.current.formUnitPrice).toBe('4.2')
    expect(result.current.formFees).toBe('0.5')
  })

  it('cancel_form_hides_form_and_resets_fields', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    act(() => result.current.showNewForm())
    expect(result.current.isFormVisible).toBe(true)
    act(() => result.current.cancelForm())
    expect(result.current.isFormVisible).toBe(false)
    expect(result.current.formDate).toBe('')
    expect(result.current.formQuantity).toBe('')
  })

  it('save_new_transaction_calls_add_and_updates_asset', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const updatedAsset = { ...ASSET_DETAILS, transactions: [TRANSACTION_A, TRANSACTION_B] }
    addTransactionMock.mockResolvedValue(updatedAsset)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formType', 'Buy')
      result.current.setFormField('formQuantity', '50')
      result.current.setFormField('formUnitPrice', '10')
      result.current.setFormField('formFees', '0.5')
    })
    act(() => result.current.saveForm())
    await waitFor(() => expect(addTransactionMock).toHaveBeenCalledWith({
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      assetName: 'KLBN4',
      date: '2024-06-01',
      type: 'Buy',
      quantity: 50,
      unitPrice: 10,
      fees: 0.5,
    }))
    await waitFor(() => expect(result.current.isFormVisible).toBe(false))
    expect(result.current.asset).toEqual(updatedAsset)
  })

  it('save_edit_transaction_calls_update', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const updatedAsset = { ...ASSET_DETAILS }
    updateTransactionMock.mockResolvedValue(updatedAsset)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    act(() => result.current.showEditForm(TRANSACTION_A))
    act(() => {
      result.current.setFormField('formQuantity', '200')
    })
    act(() => result.current.saveForm())
    await waitFor(() => expect(updateTransactionMock).toHaveBeenCalledWith(
      expect.objectContaining({ id: 'aaa', quantity: 200 }),
    ))
    expect(result.current.asset).toEqual(updatedAsset)
  })

  it('save_sets_error_on_api_failure', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    addTransactionMock.mockRejectedValue(new Error('Server error'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formQuantity', '50')
      result.current.setFormField('formUnitPrice', '10')
    })
    act(() => result.current.saveForm())
    await waitFor(() => expect(result.current.saveError).toBe('Server error'))
    expect(result.current.isFormVisible).toBe(true)
    expect(result.current.isSaving).toBe(false)
  })

  it('save_defaults_fees_to_zero_when_blank', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    addTransactionMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formQuantity', '50')
      result.current.setFormField('formUnitPrice', '10')
    })
    act(() => result.current.saveForm())
    await waitFor(() =>
      expect(addTransactionMock).toHaveBeenCalledWith(
        expect.objectContaining({ fees: 0 }),
      ),
    )
  })

  it('save_validation_error_when_required_fields_missing', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    act(() => result.current.showNewForm())
    act(() => result.current.saveForm())
    expect(result.current.saveError).not.toBeNull()
    expect(addTransactionMock).not.toHaveBeenCalled()
  })

  it('delete_transaction_calls_api_and_updates_asset', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const updatedAsset = { ...ASSET_DETAILS, transactions: [TRANSACTION_B] }
    deleteTransactionMock.mockResolvedValue(updatedAsset)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    act(() => result.current.deleteTransaction('aaa'))
    await waitFor(() => {
      expect(deleteTransactionMock).toHaveBeenCalledWith({
        brokerName: 'XPI',
        portfolioName: 'Acoes',
        assetName: 'KLBN4',
        id: 'aaa',
      })
      expect(result.current.asset).toEqual(updatedAsset)
    })
  })

  it('delete_failure_sets_delete_error', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    deleteTransactionMock.mockRejectedValue(new Error('Delete failed'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    act(() => result.current.deleteTransaction('aaa'))
    await waitFor(() => expect(result.current.deleteError).toBe('Delete failed'))
    expect(result.current.asset).toEqual(ASSET_DETAILS)
  })

  it('fetches_transactions_on_broker_selection', async () => {
    getTransactionsByBrokerMock.mockResolvedValue([SUMMARY_ITEM_A, SUMMARY_ITEM_B])
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(getTransactionsByBrokerMock).toHaveBeenCalledWith('XPI'))
    await waitFor(() => expect(result.current.chartData.length).toBeGreaterThan(0))
  })

  it('fetches_transactions_on_portfolio_selection', async () => {
    getTransactionsByPortfolioMock.mockResolvedValue([SUMMARY_ITEM_A])
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() =>
      expect(getTransactionsByPortfolioMock).toHaveBeenCalledWith('XPI', 'Acoes'),
    )
    await waitFor(() => expect(result.current.chartData.length).toBeGreaterThan(0))
  })

  it('transactions_fetch_error_sets_error_state', async () => {
    getTransactionsByBrokerMock.mockRejectedValue(new Error('Network error'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.error).toBe('Network error'))
  })

  it('set_filter_and_mode_persist_per_node_selection', async () => {
    getTransactionsByBrokerMock.mockResolvedValue([SUMMARY_ITEM_A])
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useTransactions(), { wrapper })

    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.selectedFilter).toBe('last-12-months'))
    act(() => result.current.setFilter('ytd'))
    act(() => result.current.setChartMode('Line'))
    expect(result.current.selectedFilter).toBe('ytd')
    expect(result.current.selectedChartMode).toBe('Line')

    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    expect(result.current.selectedFilter).toBe('last-12-months')
    expect(result.current.selectedChartMode).toBe('Bar')

    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.selectedFilter).toBe('ytd'))
    expect(result.current.selectedChartMode).toBe('Line')
  })
})

describe('buildMonthlyNetInvested', () => {
  it('zero_fills_months_with_no_transactions_across_selected_period', () => {
    const referenceDate = new Date(2024, 2, 15)
    const buckets = buildMonthlyNetInvested([SUMMARY_ITEM_A], 'last-12-months', referenceDate)
    expect(buckets.length).toBe(12)
    const marchBucket = buckets.find((b) => b.month === formatMonth(referenceDate))
    expect(marchBucket?.netInvested).toBe(420.5)
    const emptyMonths = buckets.filter((b) => b.netInvested === 0)
    expect(emptyMonths.length).toBe(11)
  })

  it('nets_buy_minus_sell_within_the_same_month', () => {
    const referenceDate = new Date(2024, 0, 31)
    const buckets = buildMonthlyNetInvested(
      [
        { date: '2024-01-05', type: 'Buy', totalPrice: 500 },
        { date: '2024-01-20', type: 'Sell', totalPrice: 200 },
      ],
      'ytd',
      referenceDate,
    )
    expect(buckets.length).toBe(1)
    expect(buckets[0].netInvested).toBe(300)
  })

  it('works_identically_for_broker_summary_shape_and_asset_transaction_shape', () => {
    const referenceDate = new Date(2024, 2, 31)
    const summaryResult = buildMonthlyNetInvested([SUMMARY_ITEM_A], 'ytd', referenceDate)
    const assetResult = buildMonthlyNetInvested([TRANSACTION_A], 'ytd', referenceDate)
    expect(summaryResult).toEqual(assetResult)
    expect(summaryResult.some((b) => b.netInvested === 420.5)).toBe(true)
  })
})

function formatMonth(date: Date): string {
  return date.toLocaleDateString('en-GB', { month: 'short', year: 'numeric' })
}
