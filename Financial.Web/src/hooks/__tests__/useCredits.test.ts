import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetDetailsDto, CreditDto, SelectedNode } from '../../api/types'
import { createSelectedNodeWrapper } from '../../test-utils/selectedNodeTestWrapper'
import { buildSelectionKey, useCredits } from '../useCredits'
import { pinDate } from '../../test-utils/pinDate'
import { deferred } from '../../test-utils/deferred'

const {
  getAssetDetailsMock,
  getCreditsByBrokerMock,
  getCreditsByPortfolioMock,
  addCreditMock,
  updateCreditMock,
  deleteCreditMock,
} = vi.hoisted(() => ({
  getAssetDetailsMock: vi.fn<FinancialApiClient['getAssetDetails']>(),
  getCreditsByBrokerMock: vi.fn<FinancialApiClient['getCreditsByBroker']>(),
  getCreditsByPortfolioMock: vi.fn<FinancialApiClient['getCreditsByPortfolio']>(),
  addCreditMock: vi.fn<FinancialApiClient['addCredit']>(),
  updateCreditMock: vi.fn<FinancialApiClient['updateCredit']>(),
  deleteCreditMock: vi.fn<FinancialApiClient['deleteCredit']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAssetDetails: getAssetDetailsMock,
    getCreditsByBroker: getCreditsByBrokerMock,
    getCreditsByPortfolio: getCreditsByPortfolioMock,
    addCredit: addCreditMock,
    updateCredit: updateCreditMock,
    deleteCredit: deleteCreditMock,
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

const BROKER_NODE: SelectedNode = {
  nodeType: 'Broker',
  brokerName: 'XPI',
}

const PORTFOLIO_NODE: SelectedNode = {
  nodeType: 'Portfolio',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
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

const CREDIT_A: CreditDto = {
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

const CREDIT_B: CreditDto = {
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
  totalCredits: 470.5,
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
  credits: [CREDIT_A, CREDIT_B],
  priceSnapshots: [],
  disposalRecords: [],
  corporateActions: [],
  costBasisMethod: 'AverageCost',
  taxJurisdictions: [],
}

describe('useCredits', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  beforeEach(() => {
    getAssetDetailsMock.mockReset()
    getCreditsByBrokerMock.mockReset()
    getCreditsByPortfolioMock.mockReset()
    addCreditMock.mockReset()
    updateCreditMock.mockReset()
    deleteCreditMock.mockReset()
    sessionStorage.clear()
  })

  it('returns_initial_empty_state', () => {
    const { wrapper } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    expect(result.current.isLoading).toBe(false)
    expect(result.current.credits).toEqual([])
    expect(result.current.error).toBeNull()
    expect(result.current.selectedFilter).toBe('last-12-months')
    expect(result.current.selectedMode).toBe('Stacked')
  })

  it('fetches_asset_credits_on_asset_selection', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4', 'active'))
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(result.current.credits).toHaveLength(2)
  })

  it('resolves_asset_via_getAssetDetails_with_historic_scope', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper('historic')
    renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4', 'historic'))
  })

  it('fetches_broker_credits_on_broker_selection', async () => {
    getCreditsByBrokerMock.mockResolvedValue([CREDIT_A])
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() =>
      expect(getCreditsByBrokerMock).toHaveBeenCalledWith('XPI', 'active'),
    )
    await waitFor(() => expect(result.current.credits).toHaveLength(1))
  })

  it('fetches_portfolio_credits_on_portfolio_selection', async () => {
    getCreditsByPortfolioMock.mockResolvedValue([CREDIT_B])
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() =>
      expect(getCreditsByPortfolioMock).toHaveBeenCalledWith('XPI', 'Acoes', 'active'),
    )
    await waitFor(() => expect(result.current.credits).toHaveLength(1))
  })

  it('fetches_broker_credits_with_historic_scope', async () => {
    getCreditsByBrokerMock.mockResolvedValue([CREDIT_A])
    const { wrapper, setNode } = createSelectedNodeWrapper('historic')
    renderHook(() => useCredits(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() =>
      expect(getCreditsByBrokerMock).toHaveBeenCalledWith('XPI', 'historic'),
    )
  })

  it('fetches_portfolio_credits_with_historic_scope', async () => {
    getCreditsByPortfolioMock.mockResolvedValue([CREDIT_B])
    const { wrapper, setNode } = createSelectedNodeWrapper('historic')
    renderHook(() => useCredits(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() =>
      expect(getCreditsByPortfolioMock).toHaveBeenCalledWith('XPI', 'Acoes', 'historic'),
    )
  })

  it('resets_credits_when_node_is_null', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    setNode(null)
    await waitFor(() => expect(result.current.credits).toEqual([]))
    expect(result.current.isLoading).toBe(false)
  })

  it('increments_retry_and_refetches', async () => {
    getAssetDetailsMock.mockRejectedValueOnce(new Error('Network error'))
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.error).toBe('Network error'))
    act(() => result.current.retry())
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    expect(getAssetDetailsMock).toHaveBeenCalledTimes(2)
  })

  it('set_filter_updates_selected_filter', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.setFilter('last-3-months'))
    expect(result.current.selectedFilter).toBe('last-3-months')
  })

  it('set_mode_updates_selected_mode', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.setMode('Grouped'))
    expect(result.current.selectedMode).toBe('Grouped')
  })

  it('persists_filter_and_mode_per_selection_key', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })

    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.setFilter('last-3-months'))
    act(() => result.current.setMode('Grouped'))
    expect(result.current.selectedFilter).toBe('last-3-months')
    expect(result.current.selectedMode).toBe('Grouped')

    setNode(ASSET_NODE_B)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'TASA4', 'active'))
    expect(result.current.selectedFilter).toBe('last-12-months')
    expect(result.current.selectedMode).toBe('Stacked')

    setNode(ASSET_NODE)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4', 'active'))
    await waitFor(() => expect(result.current.selectedFilter).toBe('last-3-months'))
    expect(result.current.selectedMode).toBe('Grouped')
  })

  it('defaults_to_last_year_stacked_on_first_selection', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(result.current.selectedFilter).toBe('last-12-months')
    expect(result.current.selectedMode).toBe('Stacked')
  })

  it('show_new_form_opens_blank_form_defaulting_date_to_today_and_type_to_dividend', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    pinDate('2026-07-01T00:30:00+01:00')
    act(() => result.current.showNewForm())
    expect(result.current.isFormVisible).toBe(true)
    expect(result.current.editingId).toBeNull()
    expect(result.current.formDate).toBe('2026-07-01')
    expect(result.current.formType).toBe('Dividend')
    expect(result.current.formValue).toBe('')
  })

  it('persists date and type after a successful save, for the next new-credit form', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    addCreditMock.mockResolvedValue({ ...ASSET_DETAILS, credits: [CREDIT_A, CREDIT_B] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formType', 'JCP')
      result.current.setFormField('formValue', '50')
    })
    act(() => result.current.saveForm())
    await waitFor(() => expect(addCreditMock).toHaveBeenCalledTimes(1))

    act(() => result.current.showNewForm())

    expect(result.current.formDate).toBe('2024-06-01')
    expect(result.current.formType).toBe('JCP')
    expect(result.current.formValue).toBe('')
  })

  it('show_edit_form_populates_fields', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showEditForm(CREDIT_A))
    expect(result.current.isFormVisible).toBe(true)
    expect(result.current.editingId).toBe('aaa')
    expect(result.current.formDate).toBe('2024-03-15')
    expect(result.current.formType).toBe('Dividend')
    expect(result.current.formValue).toBe('120.5')
  })

  it('cancel_form_hides_form_and_resets', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showNewForm())
    expect(result.current.isFormVisible).toBe(true)
    act(() => result.current.cancelForm())
    expect(result.current.isFormVisible).toBe(false)
    expect(result.current.formDate).toBe('')
    expect(result.current.formValue).toBe('')
  })

  it('save_new_credit_calls_add_and_updates_asset', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const updatedAsset = { ...ASSET_DETAILS, credits: [CREDIT_A] }
    addCreditMock.mockResolvedValue(updatedAsset)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formType', 'Dividend')
      result.current.setFormField('formValue', '120.50')
    })
    act(() => result.current.saveForm())
    await waitFor(() =>
      expect(addCreditMock).toHaveBeenCalledWith({
        brokerName: 'XPI',
        portfolioName: 'Acoes',
        assetName: 'KLBN4',
        scope: 'active',
        date: '2024-06-01',
        type: 'Dividend',
        value: 120.5,
        withheld: 0,
        intermediationFee: 0,
        sharesForDividend: null,
      }),
    )
    await waitFor(() => expect(result.current.isFormVisible).toBe(false))
    expect(result.current.credits).toEqual([CREDIT_A])
  })

  it('save_new_credit_with_shares_for_dividend_includes_it_in_the_request', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    addCreditMock.mockResolvedValue({ ...ASSET_DETAILS, credits: [CREDIT_A] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formType', 'Dividend')
      result.current.setFormField('formValue', '400')
      result.current.setFormField('formSharesForDividend', '800')
    })
    act(() => result.current.saveForm())
    await waitFor(() =>
      expect(addCreditMock).toHaveBeenCalledWith(expect.objectContaining({ sharesForDividend: 800 })),
    )
  })

  it('save_new_credit_with_nonPositive_shares_for_dividend_rejectsAndDoesNotCallAdd', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formType', 'Dividend')
      result.current.setFormField('formValue', '400')
      result.current.setFormField('formSharesForDividend', '0')
    })
    act(() => result.current.saveForm())
    expect(result.current.saveErrorFields.formSharesForDividend).toBeDefined()
    expect(addCreditMock).not.toHaveBeenCalled()
  })

  it('showEditForm_populatesFormSharesForDividendFromTheCredit', async () => {
    getAssetDetailsMock.mockResolvedValue({
      ...ASSET_DETAILS,
      credits: [{ ...CREDIT_A, sharesForDividend: 800 }],
    })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(1))
    act(() => result.current.showEditForm(result.current.credits[0]))
    expect(result.current.formSharesForDividend).toBe('800')
  })

  it('save_edit_credit_calls_update', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const updatedAsset = { ...ASSET_DETAILS }
    updateCreditMock.mockResolvedValue(updatedAsset)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showEditForm(CREDIT_A))
    act(() => result.current.setFormField('formValue', '200'))
    act(() => result.current.saveForm())
    await waitFor(() =>
      expect(updateCreditMock).toHaveBeenCalledWith(
        expect.objectContaining({ id: 'aaa', value: 200 }),
      ),
    )
    expect(result.current.credits).toEqual(ASSET_DETAILS.credits)
  })

  it('save_sets_error_on_api_failure', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    addCreditMock.mockRejectedValue(new Error('Server error'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formValue', '50')
    })
    act(() => result.current.saveForm())
    await waitFor(() => expect(result.current.saveError).toBe('Server error'))
    expect(result.current.isFormVisible).toBe(true)
    expect(result.current.isSaving).toBe(false)
  })

  it('save_validates_date_required', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formDate', ''))
    act(() => result.current.setFormField('formValue', '50'))
    act(() => result.current.saveForm())
    expect(result.current.saveError).not.toBeNull()
    expect(addCreditMock).not.toHaveBeenCalled()
  })

  it('save_validates_value_greater_than_zero', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formValue', '0')
    })
    act(() => result.current.saveForm())
    expect(result.current.saveError).not.toBeNull()
    expect(addCreditMock).not.toHaveBeenCalled()
  })

  it('save_new_credit_on_a_historic_asset_sends_the_historic_scope', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    addCreditMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper('historic')
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2026-10-29')
      result.current.setFormField('formType', 'Dividend')
      result.current.setFormField('formValue', '2.13')
    })
    act(() => result.current.saveForm())
    await waitFor(() =>
      expect(addCreditMock).toHaveBeenCalledWith(expect.objectContaining({ scope: 'historic', value: 2.13 })),
    )
  })

  it('delete_credit_calls_api_and_updates_asset', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const updatedAsset = { ...ASSET_DETAILS, credits: [CREDIT_B] }
    deleteCreditMock.mockResolvedValue(updatedAsset)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.deleteCredit('aaa'))
    await waitFor(() =>
      expect(deleteCreditMock).toHaveBeenCalledWith({
        brokerName: 'XPI',
        portfolioName: 'Acoes',
        assetName: 'KLBN4',
        scope: 'active',
        id: 'aaa',
      }),
    )
    await waitFor(() => expect(result.current.credits).toEqual([CREDIT_B]))
  })

  it('delete_failure_sets_delete_error', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    deleteCreditMock.mockRejectedValue(new Error('Delete failed'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.deleteCredit('aaa'))
    await waitFor(() => expect(result.current.deleteError).toBe('Delete failed'))
    expect(result.current.credits).toHaveLength(2)
  })

  it('clears an earlier delete error once a later save succeeds', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    deleteCreditMock.mockRejectedValue(new Error('Delete failed'))
    addCreditMock.mockResolvedValue({ ...ASSET_DETAILS, credits: [CREDIT_A, CREDIT_B] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.deleteCredit('aaa'))
    await waitFor(() => expect(result.current.deleteError).toBe('Delete failed'))

    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formValue', '50')
    })
    act(() => result.current.saveForm())

    await waitFor(() => expect(addCreditMock).toHaveBeenCalledOnce())
    await waitFor(() => expect(result.current.deleteError).toBeNull())
  })

  it('falls back to a delete-failure message with a recovery hint', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    deleteCreditMock.mockRejectedValue('no message')
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))

    act(() => result.current.deleteCredit('aaa'))

    await waitFor(() => expect(result.current.deleteError).toBe('The credit could not be deleted. Try again.'))
  })

  it('aggregateByMonth_computesByTypeDynamically', async () => {
    const creditA: CreditDto = {
      id: 'sm1', date: '2024-03-05T00:00:00', type: 'Dividend', value: 100, withheld: 0, intermediationFee: 0, netAmount: 100, currency: 'GBP', fxRateSnapshot: null, ...NO_DIVIDEND_ATTRIBUTION,
    }
    const creditB: CreditDto = {
      id: 'sm2', date: '2024-03-20T00:00:00', type: 'SecuritiesLendingIncome', value: 50, withheld: 0, intermediationFee: 0, netAmount: 50, currency: 'GBP', fxRateSnapshot: null, ...NO_DIVIDEND_ATTRIBUTION,
    }
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, credits: [creditA, creditB] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.setFilter('all-time'))

    const bucket = result.current.chartData.find((b) => b.month === '03/2024')
    expect(bucket?.byType.Dividend).toBe(100)
    expect(bucket?.byType.SecuritiesLendingIncome).toBe(50)
  })

  it('aggregateByMonth_computesTotalAsSumOfByType', async () => {
    const creditA: CreditDto = {
      id: 'sm1', date: '2024-03-05T00:00:00', type: 'Dividend', value: 100, withheld: 0, intermediationFee: 0, netAmount: 100, currency: 'GBP', fxRateSnapshot: null, ...NO_DIVIDEND_ATTRIBUTION,
    }
    const creditB: CreditDto = {
      id: 'sm2', date: '2024-03-20T00:00:00', type: 'SecuritiesLendingIncome', value: 50, withheld: 0, intermediationFee: 0, netAmount: 50, currency: 'GBP', fxRateSnapshot: null, ...NO_DIVIDEND_ATTRIBUTION,
    }
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, credits: [creditA, creditB] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.setFilter('all-time'))

    const bucket = result.current.chartData.find((b) => b.month === '03/2024')
    expect(bucket?.total).toBe(150)
  })

  it('aggregateByMonth_supportsAThirdCreditType', async () => {
    const creditA: CreditDto = {
      id: 'sm1', date: '2024-03-05T00:00:00', type: 'Dividend', value: 100, withheld: 0, intermediationFee: 0, netAmount: 100, currency: 'GBP', fxRateSnapshot: null, ...NO_DIVIDEND_ATTRIBUTION,
    }
    const creditB: CreditDto = {
      id: 'sm2', date: '2024-03-20T00:00:00', type: 'SecuritiesLendingIncome', value: 50, withheld: 0, intermediationFee: 0, netAmount: 50, currency: 'GBP', fxRateSnapshot: null, ...NO_DIVIDEND_ATTRIBUTION,
    }
    const creditC: CreditDto = {
      id: 'sm3', date: '2024-03-10T00:00:00', type: 'Interest', value: 30, withheld: 0, intermediationFee: 0, netAmount: 30, currency: 'GBP', fxRateSnapshot: null, ...NO_DIVIDEND_ATTRIBUTION,
    }
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, credits: [creditA, creditB, creditC] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(3))
    act(() => result.current.setFilter('all-time'))

    expect(result.current.creditTypes).toEqual(['Dividend', 'Interest', 'SecuritiesLendingIncome'])
    const bucket = result.current.chartData.find((b) => b.month === '03/2024')
    expect(bucket?.byType.Interest).toBe(30)
    expect(bucket?.total).toBe(180)
  })

  it('setChartType_defaultsToBar', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    expect(result.current.selectedChartType).toBe('Bar')
  })

  it('setChartType_persistsSelectionPerNode', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })

    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.setChartType('Line'))
    expect(result.current.selectedChartType).toBe('Line')

    setNode(ASSET_NODE_B)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'TASA4', 'active'))
    expect(result.current.selectedChartType).toBe('Bar')

    setNode(ASSET_NODE)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4', 'active'))
    await waitFor(() => expect(result.current.selectedChartType).toBe('Line'))
  })

  it('setChartType_doesNotAffectSelectedMode_forSameNode', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    expect(result.current.selectedMode).toBe('Stacked')
    act(() => result.current.setChartType('Line'))
    expect(result.current.selectedMode).toBe('Stacked')
  })

  function credit(id: string, date: string): CreditDto {
    return {
      id, date, type: 'Dividend', value: 10, withheld: 0, intermediationFee: 0, netAmount: 10, currency: 'GBP', fxRateSnapshot: null, ...NO_DIVIDEND_ATTRIBUTION,
    }
  }

  async function renderLoadedAsset(credits: CreditDto[] = [CREDIT_A, CREDIT_B]) {
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, credits })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const rendered = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(rendered.result.current.credits).toHaveLength(credits.length))
    return rendered.result
  }

  it('builds a separate selection key for an asset, a portfolio and a broker', () => {
    expect(buildSelectionKey(ASSET_NODE)).toBe('Asset|XPI|Acoes|KLBN4')
    expect(buildSelectionKey({ nodeType: 'Asset', brokerName: 'XPI' })).toBe('Asset|XPI||')
    expect(buildSelectionKey(PORTFOLIO_NODE)).toBe('Portfolio|XPI|Acoes')
    expect(buildSelectionKey({ nodeType: 'Portfolio', brokerName: 'XPI' })).toBe('Portfolio|XPI|')
    expect(buildSelectionKey(BROKER_NODE)).toBe('Broker|XPI')
  })

  it('orders the monthly totals by year, then month', async () => {
    const result = await renderLoadedAsset([credit('c1', '2024-03-05T00:00:00'), credit('c2', '2023-11-05T00:00:00'), credit('c3', '2024-01-05T00:00:00'), credit('c4', '2023-02-05T00:00:00')])

    act(() => result.current.setFilter('all-time'))

    expect(result.current.chartData.map((bucket) => bucket.month)).toEqual(['02/2023', '11/2023', '01/2024', '03/2024'])
  })

  it('keeps credits from the first day of the chosen period and leaves earlier ones out', async () => {
    pinDate('2024-06-15T12:00:00Z')
    const result = await renderLoadedAsset([
      credit('recent', '2024-05-05T00:00:00'),
      credit('first-day', '2023-07-01T00:00:00'),
      credit('old', '2023-06-30T00:00:00'),
    ])

    act(() => result.current.setFilter('last-12-months'))

    expect(result.current.chartData.map((bucket) => bucket.month)).toEqual(['07/2023', '05/2024'])
  })

  it.each([
    ['broker', BROKER_NODE, () => getCreditsByBrokerMock],
    ['portfolio', PORTFOLIO_NODE, () => getCreditsByPortfolioMock],
    ['asset', ASSET_NODE, () => getAssetDetailsMock],
  ] as const)('shows the load message when the %s credits fail to load', async (_, node, mockOf) => {
    mockOf().mockRejectedValue('unexpected')
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })

    setNode(node)

    await waitFor(() => expect(result.current.error).toBe('Unable to load credits.'))
  })

  it('loads nothing for an asset or portfolio node missing its names', () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useCredits(), { wrapper })

    setNode({ nodeType: 'Asset', brokerName: 'XPI', portfolioName: 'Acoes' })
    setNode({ nodeType: 'Portfolio', brokerName: 'XPI' })

    expect(getAssetDetailsMock).not.toHaveBeenCalled()
    expect(getCreditsByPortfolioMock).not.toHaveBeenCalled()
  })

  it('refuses a whitespace-only date and non-positive shares', async () => {
    const result = await renderLoadedAsset()
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '  ')
      result.current.setFormField('formValue', '50')
      result.current.setFormField('formSharesForDividend', '0')
    })

    act(() => result.current.saveForm())

    expect(result.current.saveErrorFields).toMatchObject({ formDate: 'Date is required.', formSharesForDividend: 'Shares must be greater than zero.' })
    expect(addCreditMock).not.toHaveBeenCalled()
  })

  it.each([
    ['  ', '  ', 0, 0],
    ['2.5', '1.25', 2.5, 1.25],
  ])('sends withheld %j and fee %j as %d and %d', async (withheld, fee, expectedWithheld, expectedFee) => {
    addCreditMock.mockResolvedValue(ASSET_DETAILS)
    const result = await renderLoadedAsset()
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formValue', '50')
      result.current.setFormField('formWithheld', withheld)
      result.current.setFormField('formIntermediationFee', fee)
    })

    act(() => result.current.saveForm())

    await waitFor(() => expect(addCreditMock).toHaveBeenCalledWith(expect.objectContaining({ withheld: expectedWithheld, intermediationFee: expectedFee })))
  })

  it('treats whitespace-only shares as no shares', async () => {
    addCreditMock.mockResolvedValue(ASSET_DETAILS)
    const result = await renderLoadedAsset()
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formValue', '50')
      result.current.setFormField('formSharesForDividend', '  ')
    })

    act(() => result.current.saveForm())

    await waitFor(() => expect(addCreditMock).toHaveBeenCalledWith(expect.objectContaining({ sharesForDividend: null })))
  })

  it('shows the save fallback message when the save fails without one', async () => {
    addCreditMock.mockRejectedValue('unexpected')
    const result = await renderLoadedAsset()
    act(() => result.current.showNewForm())
    act(() => {
      result.current.setFormField('formDate', '2024-06-01')
      result.current.setFormField('formValue', '50')
    })

    act(() => result.current.saveForm())

    await waitFor(() => expect(result.current.saveError).toBe('Failed to save credit.'))
  })

  it('keeps the edit form open when a different credit is deleted', async () => {
    deleteCreditMock.mockResolvedValue({ ...ASSET_DETAILS, credits: [CREDIT_A] })
    const result = await renderLoadedAsset()
    act(() => result.current.showEditForm(CREDIT_A))

    act(() => result.current.deleteCredit(CREDIT_B.id))
    await waitFor(() => expect(result.current.deletingId).toBeNull())

    expect(result.current.isFormVisible).toBe(true)
  })

  it('does not delete while a portfolio is selected', async () => {
    getCreditsByPortfolioMock.mockResolvedValue([CREDIT_B])
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(1))

    act(() => result.current.deleteCredit(CREDIT_B.id))

    expect(deleteCreditMock).not.toHaveBeenCalled()
  })

  describe('responses for a node the user has already left', () => {
    const OTHER_DETAILS: AssetDetailsDto = { ...ASSET_DETAILS, name: 'TASA4', credits: [CREDIT_B] }
    const LEFT_NODE_DETAILS: AssetDetailsDto = { ...ASSET_DETAILS, credits: [] }

    async function renderOnAsset() {
      const { wrapper, setNode } = createSelectedNodeWrapper()
      const hook = renderHook(() => useCredits(), { wrapper })
      setNode(ASSET_NODE)
      await waitFor(() => expect(hook.result.current.credits).not.toHaveLength(0))
      return { result: hook.result, setNode }
    }

    async function startPendingLoad() {
      const pending = deferred<AssetDetailsDto>()
      getAssetDetailsMock.mockReturnValueOnce(pending.promise)
      const { wrapper, setNode } = createSelectedNodeWrapper()
      const { result } = renderHook(() => useCredits(), { wrapper })
      setNode(ASSET_NODE)
      await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledOnce())
      return { result, setNode, pending }
    }

    async function startPendingSave() {
      getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
      const pending = deferred<AssetDetailsDto>()
      addCreditMock.mockReturnValue(pending.promise)
      const { result, setNode } = await renderOnAsset()
      act(() => result.current.showNewForm())
      act(() => {
        result.current.setFormField('formDate', '2024-06-01')
        result.current.setFormField('formValue', '50')
      })
      act(() => result.current.saveForm())
      expect(addCreditMock).toHaveBeenCalledOnce()
      return { result, setNode, pending }
    }

    async function startPendingDelete() {
      getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
      const pending = deferred<AssetDetailsDto>()
      deleteCreditMock.mockReturnValue(pending.promise)
      const { result, setNode } = await renderOnAsset()
      act(() => result.current.deleteCredit('aaa'))
      expect(deleteCreditMock).toHaveBeenCalledOnce()
      return { result, setNode, pending }
    }

    async function moveToOtherAssetWithNewFormOpen({ result, setNode }: Awaited<ReturnType<typeof startPendingLoad>>) {
      getAssetDetailsMock.mockResolvedValue(OTHER_DETAILS)
      setNode(ASSET_NODE_B)
      await waitFor(() => expect(result.current.credits).toEqual([CREDIT_B]))
      act(() => result.current.showNewForm())
      act(() => result.current.setFormField('formValue', '7'))
    }

    function expectOtherAssetUntouched({ result }: Awaited<ReturnType<typeof startPendingLoad>>) {
      expect(result.current.credits).toEqual([CREDIT_B])
      expect(result.current.isFormVisible).toBe(true)
      expect(result.current.formValue).toBe('7')
      expect(result.current.isSaving).toBe(false)
      expect(result.current.saveError).toBeNull()
      expect(result.current.deletingId).toBeNull()
      expect(result.current.deleteError).toBeNull()
      expect(result.current.error).toBeNull()
    }

    it('ignores_a_late_load_success_for_the_previous_asset', async () => {
      const started = await startPendingLoad()
      await moveToOtherAssetWithNewFormOpen(started)
      await act(async () => started.pending.resolve(LEFT_NODE_DETAILS))
      expectOtherAssetUntouched(started)
    })

    it('ignores_a_late_load_failure_for_the_previous_asset', async () => {
      const started = await startPendingLoad()
      await moveToOtherAssetWithNewFormOpen(started)
      await act(async () => started.pending.reject(new Error('Load failed')))
      expectOtherAssetUntouched(started)
    })

    it('ignores_a_late_save_success_for_the_previous_asset', async () => {
      const started = await startPendingSave()
      await moveToOtherAssetWithNewFormOpen(started)
      await act(async () => started.pending.resolve(LEFT_NODE_DETAILS))
      expectOtherAssetUntouched(started)
    })

    it('ignores_a_late_save_failure_for_the_previous_asset', async () => {
      const started = await startPendingSave()
      await moveToOtherAssetWithNewFormOpen(started)
      await act(async () => started.pending.reject(new Error('Save failed')))
      expectOtherAssetUntouched(started)
    })

    it('ignores_a_late_delete_success_for_the_previous_asset', async () => {
      const started = await startPendingDelete()
      await moveToOtherAssetWithNewFormOpen(started)
      await act(async () => started.pending.resolve(LEFT_NODE_DETAILS))
      expectOtherAssetUntouched(started)
    })

    it('ignores_a_late_delete_failure_for_the_previous_asset', async () => {
      const started = await startPendingDelete()
      await moveToOtherAssetWithNewFormOpen(started)
      await act(async () => started.pending.reject(new Error('Delete failed')))
      expectOtherAssetUntouched(started)
    })

    it('still_applies_a_delete_that_finishes_on_the_same_asset_after_a_form_was_opened', async () => {
      const { result, pending } = await startPendingDelete()
      act(() => result.current.showNewForm())

      await act(async () => pending.resolve(LEFT_NODE_DETAILS))

      expect(result.current.credits).toEqual([])
      expect(result.current.deletingId).toBeNull()
      expect(result.current.isFormVisible).toBe(true)
    })
  })
})
