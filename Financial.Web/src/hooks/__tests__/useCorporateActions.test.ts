import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetDetailsDto, CorporateActionDto, SelectedNode } from '../../api/types'
import { createSelectedNodeWrapper } from '../../test-utils/selectedNodeTestWrapper'
import { useCorporateActions } from '../useCorporateActions'

const { getAssetDetailsMock, addSplitMock, updateSplitMock, deleteCorporateActionMock } = vi.hoisted(() => ({
  getAssetDetailsMock: vi.fn<FinancialApiClient['getAssetDetails']>(),
  addSplitMock: vi.fn<FinancialApiClient['addSplit']>(),
  updateSplitMock: vi.fn<FinancialApiClient['updateSplit']>(),
  deleteCorporateActionMock: vi.fn<FinancialApiClient['deleteCorporateAction']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAssetDetails: getAssetDetailsMock,
    addSplit: addSplitMock,
    updateSplit: updateSplitMock,
    deleteCorporateAction: deleteCorporateActionMock,
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

const PORTFOLIO_NODE: SelectedNode = {
  nodeType: 'Portfolio',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
}

const SPLIT_RECORD: CorporateActionDto = {
  id: 'ca1',
  type: 'Split',
  effectiveDate: '2024-03-15T00:00:00',
  ratioFactor: 2,
  allocationPercentage: null,
  calculationStatus: null,
  carriedCostBasis: null,
  cashInLieu: null,
  convertedQuantity: null,
  correlationId: null,
  exchangeRatio: null,
  linkedAssetName: null,
  note: 'Announced 2-for-1 split',
  role: null,
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
  quantity: 200,
  averagePrice: 10,
  averageSellPrice: null,
  positionType: 'Long',
  totalBought: 2000,
  totalSold: 0,
  totalCredits: 0,
  realizedGainLoss: 0,
  marketValue: null,
  costOfUnitsHeld: 2000,
  unrealisedGain: null,
  priceAsOfDate: null,
  marketStatus: 'Current',
  priceOnlyReturn: null,
  totalReturn: null,
  transactions: [],
  credits: [],
  priceSnapshots: [],
  cashFlowsWithCredits: [],
  cashFlowsWithoutCredits: [],
  disposalRecords: [],
  corporateActions: [SPLIT_RECORD],
  costBasisMethod: 'AverageCost',
  taxJurisdictions: [],
}

describe('useCorporateActions', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset().mockResolvedValue(ASSET_DETAILS)
    addSplitMock.mockReset()
    updateSplitMock.mockReset()
    deleteCorporateActionMock.mockReset()
  })

  it('fetches the asset and exposes its corporate actions once on selection', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })

    setNode(ASSET_NODE)

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getAssetDetailsMock).toHaveBeenCalledTimes(1)
    expect(result.current.corporateActions).toEqual([SPLIT_RECORD])
  })

  it('resets to empty when the selected node is not an asset', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })

    setNode(PORTFOLIO_NODE)

    expect(result.current.isLoading).toBe(false)
    expect(result.current.corporateActions).toEqual([])
    expect(getAssetDetailsMock).not.toHaveBeenCalled()
  })

  it('surfaces a fetch failure and retries', async () => {
    getAssetDetailsMock.mockReset().mockRejectedValueOnce(new Error('boom')).mockResolvedValueOnce(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })

    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.error).toBe('boom'))

    act(() => result.current.retry())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBeNull()
    expect(result.current.corporateActions).toEqual([SPLIT_RECORD])
  })

  it('opens a blank form defaulted to Split with today as the effective date', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())

    expect(result.current.isFormVisible).toBe(true)
    expect(result.current.editingId).toBeNull()
    expect(result.current.formType).toBe('Split')
    expect(result.current.formEffectiveDate).not.toBe('')
    expect(result.current.formRatioNumerator).toBe('')
    expect(result.current.formRatioDenominator).toBe('')
  })

  it('populates the form from an existing record when editing', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(SPLIT_RECORD))

    expect(result.current.editingId).toBe('ca1')
    expect(result.current.formRatioNumerator).toBe('2')
    expect(result.current.formRatioDenominator).toBe('1')
    expect(result.current.formNote).toBe('Announced 2-for-1 split')
  })

  it('rejects an invalid ratio without calling the API', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formRatioNumerator', '1'))
    act(() => result.current.setFormField('formRatioDenominator', '1'))
    act(() => result.current.saveForm())

    expect(result.current.saveErrorFields.formRatio).toBe('Enter a valid split ratio other than 1-for-1')
    expect(addSplitMock).not.toHaveBeenCalled()
  })

  it('saves a new split and closes the form on success', async () => {
    const savedAsset: AssetDetailsDto = { ...ASSET_DETAILS, quantity: 400, averagePrice: 5 }
    addSplitMock.mockResolvedValueOnce(savedAsset)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formRatioNumerator', '2'))
    act(() => result.current.setFormField('formRatioDenominator', '1'))
    act(() => result.current.saveForm())

    await waitFor(() => expect(result.current.isFormVisible).toBe(false))

    expect(addSplitMock).toHaveBeenCalledWith(
      expect.objectContaining({
        brokerName: 'XPI',
        portfolioName: 'Acoes',
        assetName: 'KLBN4',
        ratioFactor: 2,
        note: null,
      }),
    )
    expect(result.current.asset).toEqual(savedAsset)
  })

  it('updates an existing split when editing', async () => {
    const savedAsset: AssetDetailsDto = { ...ASSET_DETAILS }
    updateSplitMock.mockResolvedValueOnce(savedAsset)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(SPLIT_RECORD))
    act(() => result.current.saveForm())

    await waitFor(() => expect(result.current.isFormVisible).toBe(false))

    expect(updateSplitMock).toHaveBeenCalledWith(expect.objectContaining({ id: 'ca1', ratioFactor: 2 }))
    expect(addSplitMock).not.toHaveBeenCalled()
  })

  it('surfaces a server-side save rejection and preserves the entered form data', async () => {
    addSplitMock.mockRejectedValueOnce(new Error('This holding has no open position to split.'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formRatioNumerator', '2'))
    act(() => result.current.setFormField('formRatioDenominator', '1'))
    act(() => result.current.setFormField('formNote', 'from broker letter'))
    act(() => result.current.saveForm())

    await waitFor(() => expect(result.current.isSaving).toBe(false))

    expect(result.current.saveError).toBe('This holding has no open position to split.')
    expect(result.current.isFormVisible).toBe(true)
    expect(result.current.formRatioNumerator).toBe('2')
    expect(result.current.formNote).toBe('from broker letter')
  })

  it('deletes a corporate action and refreshes the asset', async () => {
    const savedAsset: AssetDetailsDto = { ...ASSET_DETAILS, corporateActions: [] }
    deleteCorporateActionMock.mockResolvedValueOnce(savedAsset)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteCorporateAction('ca1'))

    await waitFor(() => expect(result.current.corporateActions).toEqual([]))
    expect(deleteCorporateActionMock).toHaveBeenCalledWith({
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      assetName: 'KLBN4',
      id: 'ca1',
    })
  })

  it('surfaces a delete failure without discarding the current list', async () => {
    deleteCorporateActionMock.mockRejectedValueOnce(new Error('Cannot delete: a later disposal depends on lots created by this split'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteCorporateAction('ca1'))

    await waitFor(() =>
      expect(result.current.deleteError).toBe('Cannot delete: a later disposal depends on lots created by this split'),
    )
    expect(result.current.corporateActions).toEqual([SPLIT_RECORD])
  })
})
