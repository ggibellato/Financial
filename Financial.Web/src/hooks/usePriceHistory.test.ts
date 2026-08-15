import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { AssetDetailsDto, AssetPriceSnapshotDto, SelectedNode } from '../api/types'
import { createSelectedNodeWrapper } from '../test-utils/selectedNodeTestWrapper'
import { usePriceHistory } from './usePriceHistory'

const getAssetDetailsMock = vi.fn<FinancialApiClient['getAssetDetails']>()
const setAssetPriceMock = vi.fn<FinancialApiClient['setAssetPrice']>()
const deleteAssetPriceMock = vi.fn<FinancialApiClient['deleteAssetPrice']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getAssetDetails: getAssetDetailsMock,
    setAssetPrice: setAssetPriceMock,
    deleteAssetPrice: deleteAssetPriceMock,
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
  positionType: 'Long',
}

const ASSET_NODE_B: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetName: 'TASA4',
  ticker: 'TASA4',
  exchange: 'BVMF',
  positionType: 'Long',
}

const BROKER_NODE: SelectedNode = {
  nodeType: 'Broker',
  brokerName: 'XPI',
}

const ENTRY_A: AssetPriceSnapshotDto = { date: '2026-08-15', price: 110.5, isManual: true }
const ENTRY_B: AssetPriceSnapshotDto = { date: '2026-08-01', price: 100, isManual: false }

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
  averageSellPrice: null,
  positionType: 'Long',
  totalBought: 2000,
  totalSold: 0,
  totalCredits: 0,
  realizedGainLoss: 0,
  transactions: [],
  credits: [],
  priceHistory: [ENTRY_A, ENTRY_B],
  cashFlowsWithCredits: [],
  cashFlowsWithoutCredits: [],
}

describe('usePriceHistory', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset()
    setAssetPriceMock.mockReset()
    deleteAssetPriceMock.mockReset()
    vi.mocked(window.confirm).mockReturnValue(true)
  })

  it('returns_initial_empty_state', () => {
    const { wrapper } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    expect(result.current.isLoading).toBe(false)
    expect(result.current.entries).toEqual([])
    expect(result.current.error).toBeNull()
    expect(result.current.selectedFilter).toBe('last-12-months')
  })

  it('fetches_price_history_on_asset_selection', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4', 'active'))
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
  })

  it('does_not_fetch_on_broker_selection', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(BROKER_NODE)
    expect(getAssetDetailsMock).not.toHaveBeenCalled()
    expect(result.current.entries).toEqual([])
  })

  it('resets_entries_when_node_is_null', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    setNode(null)
    await waitFor(() => expect(result.current.entries).toEqual([]))
  })

  it('increments_retry_and_refetches', async () => {
    getAssetDetailsMock.mockRejectedValueOnce(new Error('Network error'))
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.error).toBe('Network error'))
    act(() => result.current.retry())
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    expect(getAssetDetailsMock).toHaveBeenCalledTimes(2)
  })

  it('sorts_entries_by_date_descending', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    expect(result.current.entries[0].date).toBe('2026-08-15')
    expect(result.current.entries[1].date).toBe('2026-08-01')
  })

  it('set_filter_updates_selected_filter', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    act(() => result.current.setFilter('last-3-months'))
    expect(result.current.selectedFilter).toBe('last-3-months')
  })

  it('persists_filter_per_selection_key', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })

    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    act(() => result.current.setFilter('all-time'))
    expect(result.current.selectedFilter).toBe('all-time')

    setNode(ASSET_NODE_B)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'TASA4', 'active'))
    expect(result.current.selectedFilter).toBe('last-12-months')

    setNode(ASSET_NODE)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4', 'active'))
    await waitFor(() => expect(result.current.selectedFilter).toBe('all-time'))
  })

  it('show_new_form_opens_blank_form', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    act(() => result.current.showNewForm())
    expect(result.current.isFormVisible).toBe(true)
    expect(result.current.editingDate).toBeNull()
    expect(result.current.formDate).toBe('')
    expect(result.current.formPrice).toBe('')
  })

  it('show_edit_form_populates_fields', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    act(() => result.current.showEditForm(ENTRY_A))
    expect(result.current.isFormVisible).toBe(true)
    expect(result.current.editingDate).toBe('2026-08-15')
    expect(result.current.formPrice).toBe('110.5')
  })

  it('cancel_form_hides_form_and_resets', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => result.current.cancelForm())
    expect(result.current.isFormVisible).toBe(false)
  })

  it('save_calls_setAssetPrice_and_updates_entries', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const updated = { ...ASSET_DETAILS, priceHistory: [ENTRY_A] }
    setAssetPriceMock.mockResolvedValue(updated)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2026-08-20')
      result.current.setFormField('formPrice', '125.75')
    })
    act(() => result.current.saveForm())
    await waitFor(() =>
      expect(setAssetPriceMock).toHaveBeenCalledWith({
        brokerName: 'XPI',
        portfolioName: 'Acoes',
        assetName: 'KLBN4',
        date: '2026-08-20',
        price: 125.75,
      }),
    )
    await waitFor(() => expect(result.current.isFormVisible).toBe(false))
    expect(result.current.entries).toEqual([ENTRY_A])
  })

  it('save_validates_date_required', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formPrice', '50'))
    act(() => result.current.saveForm())
    expect(result.current.saveError).not.toBeNull()
    expect(setAssetPriceMock).not.toHaveBeenCalled()
  })

  it('save_validates_price_greater_than_zero', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2026-08-20')
      result.current.setFormField('formPrice', '0')
    })
    act(() => result.current.saveForm())
    expect(result.current.saveError).not.toBeNull()
    expect(setAssetPriceMock).not.toHaveBeenCalled()
  })

  it('save_sets_error_on_api_failure', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    setAssetPriceMock.mockRejectedValue(new Error('Server error'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2026-08-20')
      result.current.setFormField('formPrice', '50')
    })
    act(() => result.current.saveForm())
    await waitFor(() => expect(result.current.saveError).toBe('Server error'))
    expect(result.current.isFormVisible).toBe(true)
  })

  it('delete_entry_calls_api_and_updates_entries', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const updated = { ...ASSET_DETAILS, priceHistory: [ENTRY_B] }
    deleteAssetPriceMock.mockResolvedValue(updated)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    act(() => result.current.deleteEntry('2026-08-15'))
    await waitFor(() =>
      expect(deleteAssetPriceMock).toHaveBeenCalledWith({
        brokerName: 'XPI',
        portfolioName: 'Acoes',
        assetName: 'KLBN4',
        date: '2026-08-15',
      }),
    )
    await waitFor(() => expect(result.current.entries).toEqual([ENTRY_B]))
  })

  it('delete_failure_sets_delete_error', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    deleteAssetPriceMock.mockRejectedValue(new Error('Delete failed'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    act(() => result.current.deleteEntry('2026-08-15'))
    await waitFor(() => expect(result.current.deleteError).toBe('Delete failed'))
    expect(result.current.entries).toHaveLength(2)
  })

  it('filteredEntries_excludes_entries_outside_the_selected_window', async () => {
    const recent: AssetPriceSnapshotDto = { date: new Date().toISOString().slice(0, 10), price: 100, isManual: true }
    const old: AssetPriceSnapshotDto = { date: '2020-01-01', price: 50, isManual: false }
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, priceHistory: [recent, old] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePriceHistory(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.entries).toHaveLength(2))
    act(() => result.current.setFilter('this-month'))
    expect(result.current.filteredEntries).toHaveLength(1)
    expect(result.current.filteredEntries[0].date).toBe(recent.date)
  })
})
