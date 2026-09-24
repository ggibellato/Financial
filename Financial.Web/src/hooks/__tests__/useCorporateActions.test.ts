import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetAdminDto, AssetDetailsDto, CorporateActionDto, SelectedNode } from '../../api/types'
import { BLANK_TARGET_ASSET_IDENTITY } from '../../components/targetAssetPickerValue'
import { createSelectedNodeWrapper } from '../../test-utils/selectedNodeTestWrapper'
import { useCorporateActions } from '../useCorporateActions'

const {
  getAssetDetailsMock,
  addSplitMock,
  updateSplitMock,
  addMergerMock,
  updateMergerMock,
  deleteCorporateActionMock,
  getAdminAssetsMock,
} = vi.hoisted(() => ({
  getAssetDetailsMock: vi.fn<FinancialApiClient['getAssetDetails']>(),
  addSplitMock: vi.fn<FinancialApiClient['addSplit']>(),
  updateSplitMock: vi.fn<FinancialApiClient['updateSplit']>(),
  addMergerMock: vi.fn<FinancialApiClient['addMerger']>(),
  updateMergerMock: vi.fn<FinancialApiClient['updateMerger']>(),
  deleteCorporateActionMock: vi.fn<FinancialApiClient['deleteCorporateAction']>(),
  getAdminAssetsMock: vi.fn<FinancialApiClient['getAdminAssets']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAssetDetails: getAssetDetailsMock,
    addSplit: addSplitMock,
    updateSplit: updateSplitMock,
    addMerger: addMergerMock,
    updateMerger: updateMergerMock,
    deleteCorporateAction: deleteCorporateActionMock,
    getAdminAssets: getAdminAssetsMock,
  } as Partial<FinancialApiClient>,
}))

function makeAdminAsset(name: string): AssetAdminDto {
  return {
    name,
    brokerName: 'XPI',
    portfolioName: 'Acoes',
    brokerStatus: 'Active',
    isin: '',
    exchange: '',
    ticker: '',
    country: 'Unknown',
    localTypeCode: '',
    class: 'Unknown',
    valuationMethod: 'Unspecified',
    incomePolicy: 'Unknown',
    quantity: 0,
  }
}

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

const MERGER_RECORD: CorporateActionDto = {
  id: 'ca2',
  type: 'Merger',
  effectiveDate: '2024-05-01T00:00:00',
  ratioFactor: null,
  allocationPercentage: null,
  calculationStatus: 'RequiresReview',
  carriedCostBasis: 2000,
  cashInLieu: null,
  convertedQuantity: 100,
  correlationId: 'corr-1',
  exchangeRatio: 0.5,
  linkedAssetName: 'Company B',
  note: null,
  role: 'Source',
}

describe('useCorporateActions', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset().mockResolvedValue(ASSET_DETAILS)
    addSplitMock.mockReset()
    updateSplitMock.mockReset()
    addMergerMock.mockReset()
    updateMergerMock.mockReset()
    deleteCorporateActionMock.mockReset()
    getAdminAssetsMock.mockReset().mockResolvedValue([])
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

  it('reconstructs a reverse split as a whole-number fraction when editing', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm({ ...SPLIT_RECORD, ratioFactor: 0.1 }))

    expect(result.current.formRatioNumerator).toBe('1')
    expect(result.current.formRatioDenominator).toBe('10')
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

  it('still defaults a new form to Split even after Merger was last selected', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formType', 'Merger'))
    act(() => result.current.cancelForm())
    act(() => result.current.showNewForm())

    expect(result.current.formType).toBe('Split')
  })

  it('blocks advanceToConfirm on a missing target asset and an invalid exchange ratio, leaving formStep on fields', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formType', 'Merger'))
    act(() => result.current.setFormField('formExchangeRatio', '0'))
    act(() => result.current.advanceToConfirm())

    expect(result.current.formStep).toBe('fields')
    expect(result.current.saveErrorFields.formTargetAsset).toBe('Target asset is required')
    expect(result.current.saveErrorFields.formExchangeRatio).toBe('Exchange ratio must be greater than zero')
  })

  it('moves formStep to confirm once the Merger fields are valid', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formType', 'Merger'))
    act(() => result.current.setTargetAsset({ assetName: 'Company B', identity: BLANK_TARGET_ASSET_IDENTITY }))
    act(() => result.current.setFormField('formExchangeRatio', '0.5'))
    act(() => result.current.advanceToConfirm())

    expect(result.current.formStep).toBe('confirm')
    expect(result.current.saveErrorFields).toEqual({})
  })

  it('does not call the API when saveForm is invoked on a Merger still at the fields step', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formType', 'Merger'))
    act(() => result.current.setTargetAsset({ assetName: 'Company B', identity: BLANK_TARGET_ASSET_IDENTITY }))
    act(() => result.current.setFormField('formExchangeRatio', '0.5'))
    act(() => result.current.saveForm())

    expect(result.current.formStep).toBe('fields')
    expect(result.current.isFormVisible).toBe(true)
    expect(addMergerMock).not.toHaveBeenCalled()
  })

  it('backToFields returns to the fields step and clears the save error', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formType', 'Merger'))
    act(() => result.current.setFormField('formExchangeRatio', '0'))
    act(() => result.current.advanceToConfirm())
    expect(result.current.saveErrorFields.formExchangeRatio).toBeDefined()

    act(() => result.current.backToFields())

    expect(result.current.formStep).toBe('fields')
    expect(result.current.saveError).toBeNull()
    expect(result.current.saveErrorFields).toEqual({})
  })

  it('saves a new merger with createTargetAssetInline true when the typed name matches no existing asset', async () => {
    getAdminAssetsMock.mockReset().mockResolvedValue([makeAdminAsset('Company C')])
    const savedResult = {
      source: { ...ASSET_DETAILS, quantity: 0 },
      target: { ...ASSET_DETAILS, name: 'Company B', quantity: 100 },
    }
    addMergerMock.mockResolvedValueOnce(savedResult)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formType', 'Merger'))
    await waitFor(() => expect(result.current.targetAssetOptions.isLoading).toBe(false))

    act(() => result.current.setTargetAsset({ assetName: 'Company B', identity: BLANK_TARGET_ASSET_IDENTITY }))
    act(() => result.current.setFormField('formExchangeRatio', '0.5'))
    act(() => result.current.setFormField('formEffectiveDate', '2024-05-01'))
    act(() => result.current.advanceToConfirm())
    act(() => result.current.saveForm())

    await waitFor(() => expect(result.current.isFormVisible).toBe(false))

    expect(addMergerMock).toHaveBeenCalledWith(
      expect.objectContaining({
        brokerName: 'XPI',
        portfolioName: 'Acoes',
        sourceAssetName: 'KLBN4',
        targetAssetName: 'Company B',
        createTargetAssetInline: true,
        exchangeRatio: 0.5,
      }),
    )
  })

  it('saves a new merger with createTargetAssetInline false when the typed name matches an existing asset', async () => {
    getAdminAssetsMock.mockReset().mockResolvedValue([makeAdminAsset('Company B')])
    const savedResult = {
      source: { ...ASSET_DETAILS, quantity: 0 },
      target: { ...ASSET_DETAILS, name: 'Company B', quantity: 100 },
    }
    addMergerMock.mockResolvedValueOnce(savedResult)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formType', 'Merger'))
    await waitFor(() => expect(result.current.targetAssetOptions.isLoading).toBe(false))

    act(() => result.current.setTargetAsset({ assetName: 'Company B', identity: BLANK_TARGET_ASSET_IDENTITY }))
    act(() => result.current.setFormField('formExchangeRatio', '0.5'))
    act(() => result.current.advanceToConfirm())
    act(() => result.current.saveForm())

    await waitFor(() => expect(result.current.isFormVisible).toBe(false))

    expect(addMergerMock).toHaveBeenCalledWith(
      expect.objectContaining({ createTargetAssetInline: false }),
    )
  })

  it('saves an edited merger via updateMerger with id/sourceAssetName and no target fields', async () => {
    const savedResult = {
      source: { ...ASSET_DETAILS, quantity: 0 },
      target: { ...ASSET_DETAILS, name: 'Company B', quantity: 100 },
    }
    updateMergerMock.mockResolvedValueOnce(savedResult)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(MERGER_RECORD))
    act(() => result.current.advanceToConfirm())
    act(() => result.current.saveForm())

    await waitFor(() => expect(result.current.isFormVisible).toBe(false))

    expect(updateMergerMock).toHaveBeenCalledWith({
      id: 'ca2',
      brokerName: 'XPI',
      portfolioName: 'Acoes',
      sourceAssetName: 'KLBN4',
      effectiveDate: '2024-05-01',
      exchangeRatio: 0.5,
      cashInLieuAmount: null,
      note: null,
    })
    expect(addMergerMock).not.toHaveBeenCalled()
  })

  it('routes a merger name-collision server error to saveErrorFields.formTargetAsset', async () => {
    addMergerMock.mockRejectedValueOnce(
      new Error('An asset named "Company B" already exists — select it or choose a different name'),
    )
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formType', 'Merger'))
    act(() => result.current.setTargetAsset({ assetName: 'Company B', identity: BLANK_TARGET_ASSET_IDENTITY }))
    act(() => result.current.setFormField('formExchangeRatio', '0.5'))
    act(() => result.current.advanceToConfirm())
    act(() => result.current.saveForm())

    await waitFor(() => expect(result.current.isSaving).toBe(false))

    expect(result.current.saveErrorFields.formTargetAsset).toBe(
      'An asset named "Company B" already exists — select it or choose a different name',
    )
  })

  it('SAVE_SUCCESS picks the target side of the merger result when the currently-selected asset is the target', async () => {
    const savedResult = {
      source: { ...ASSET_DETAILS, name: 'Company A', quantity: 0 },
      target: { ...ASSET_DETAILS, name: 'KLBN4', quantity: 100 },
    }
    addMergerMock.mockResolvedValueOnce(savedResult)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCorporateActions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formType', 'Merger'))
    act(() => result.current.setTargetAsset({ assetName: 'Company A', identity: BLANK_TARGET_ASSET_IDENTITY }))
    act(() => result.current.setFormField('formExchangeRatio', '0.5'))
    act(() => result.current.advanceToConfirm())
    act(() => result.current.saveForm())

    await waitFor(() => expect(result.current.isFormVisible).toBe(false))

    expect(result.current.asset).toEqual(savedResult.target)
  })
})
