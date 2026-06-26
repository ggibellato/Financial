import { act, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { createElement } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { AssetDetailsDto, CreditDto, SelectedNode } from '../api/types'
import { SelectedNodeProvider, useSelectedNode } from '../context/SelectedNodeContext'
import { useCredits } from './useCredits'

const getAssetDetailsMock = vi.fn<FinancialApiClient['getAssetDetails']>()
const getCreditsByBrokerMock = vi.fn<FinancialApiClient['getCreditsByBroker']>()
const getCreditsByPortfolioMock = vi.fn<FinancialApiClient['getCreditsByPortfolio']>()
const addCreditMock = vi.fn<FinancialApiClient['addCredit']>()
const updateCreditMock = vi.fn<FinancialApiClient['updateCredit']>()
const deleteCreditMock = vi.fn<FinancialApiClient['deleteCredit']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getAssetDetails: getAssetDetailsMock,
    getCreditsByBroker: getCreditsByBrokerMock,
    getCreditsByPortfolio: getCreditsByPortfolioMock,
    addCredit: addCreditMock,
    updateCredit: updateCreditMock,
    deleteCredit: deleteCreditMock,
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
  isActive: true,
}

const CREDIT_A: CreditDto = {
  id: 'aaa',
  date: '2024-03-15T00:00:00',
  type: 'Dividend',
  value: 120.5,
}

const CREDIT_B: CreditDto = {
  id: 'bbb',
  date: '2024-01-10T00:00:00',
  type: 'Rent',
  value: 350.0,
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
  totalCredits: 470.5,
  transactions: [],
  credits: [CREDIT_A, CREDIT_B],
}

function createWrapper() {
  let setNodeRef: ((node: SelectedNode | null) => void) | undefined

  function NodeControl() {
    const { setSelectedNode } = useSelectedNode()
    setNodeRef = setSelectedNode
    return null
  }

  function Wrapper({ children }: { children: ReactNode }) {
    return createElement(SelectedNodeProvider, null, createElement(NodeControl), children)
  }

  return {
    wrapper: Wrapper,
    setNode: (node: SelectedNode | null) =>
      act(() => {
        setNodeRef?.(node)
      }),
  }
}

describe('useCredits', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset()
    getCreditsByBrokerMock.mockReset()
    getCreditsByPortfolioMock.mockReset()
    addCreditMock.mockReset()
    updateCreditMock.mockReset()
    deleteCreditMock.mockReset()
    vi.mocked(window.confirm).mockReturnValue(true)
  })

  it('returns_initial_empty_state', () => {
    const { wrapper } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    expect(result.current.isLoading).toBe(false)
    expect(result.current.credits).toEqual([])
    expect(result.current.error).toBeNull()
    expect(result.current.selectedFilter).toBe('last-year')
    expect(result.current.selectedMode).toBe('Stacked')
  })

  it('fetches_asset_credits_on_asset_selection', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4'))
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(result.current.credits).toHaveLength(2)
  })

  it('fetches_broker_credits_on_broker_selection', async () => {
    getCreditsByBrokerMock.mockResolvedValue([CREDIT_A])
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() =>
      expect(getCreditsByBrokerMock).toHaveBeenCalledWith('XPI'),
    )
    await waitFor(() => expect(result.current.credits).toHaveLength(1))
  })

  it('fetches_portfolio_credits_on_portfolio_selection', async () => {
    getCreditsByPortfolioMock.mockResolvedValue([CREDIT_B])
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() =>
      expect(getCreditsByPortfolioMock).toHaveBeenCalledWith('XPI', 'Acoes'),
    )
    await waitFor(() => expect(result.current.credits).toHaveLength(1))
  })

  it('resets_credits_when_node_is_null', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createWrapper()
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
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.error).toBe('Network error'))
    act(() => result.current.retry())
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    expect(getAssetDetailsMock).toHaveBeenCalledTimes(2)
  })

  it('set_filter_updates_selected_filter', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.setFilter('last-3-months'))
    expect(result.current.selectedFilter).toBe('last-3-months')
  })

  it('set_mode_updates_selected_mode', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.setMode('Grouped'))
    expect(result.current.selectedMode).toBe('Grouped')
  })

  it('persists_filter_and_mode_per_selection_key', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })

    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.setFilter('last-3-months'))
    act(() => result.current.setMode('Grouped'))
    expect(result.current.selectedFilter).toBe('last-3-months')
    expect(result.current.selectedMode).toBe('Grouped')

    setNode(ASSET_NODE_B)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'TASA4'))
    expect(result.current.selectedFilter).toBe('last-year')
    expect(result.current.selectedMode).toBe('Stacked')

    setNode(ASSET_NODE)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4'))
    await waitFor(() => expect(result.current.selectedFilter).toBe('last-3-months'))
    expect(result.current.selectedMode).toBe('Grouped')
  })

  it('defaults_to_last_year_stacked_on_first_selection', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(result.current.selectedFilter).toBe('last-year')
    expect(result.current.selectedMode).toBe('Stacked')
  })

  it('show_new_form_opens_blank_form', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showNewForm())
    expect(result.current.isFormVisible).toBe(true)
    expect(result.current.editingId).toBeNull()
    expect(result.current.formDate).toBe('')
    expect(result.current.formType).toBe('Dividend')
    expect(result.current.formValue).toBe('')
  })

  it('show_edit_form_populates_fields', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createWrapper()
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
    const { wrapper, setNode } = createWrapper()
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
    const { wrapper, setNode } = createWrapper()
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
        date: '2024-06-01',
        type: 'Dividend',
        value: 120.5,
      }),
    )
    await waitFor(() => expect(result.current.isFormVisible).toBe(false))
    expect(result.current.credits).toEqual([CREDIT_A])
  })

  it('save_edit_credit_calls_update', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const updatedAsset = { ...ASSET_DETAILS }
    updateCreditMock.mockResolvedValue(updatedAsset)
    const { wrapper, setNode } = createWrapper()
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
    expect(result.current.credits).toEqual(ASSET_DETAILS.credits.slice().sort(
      (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime(),
    ))
  })

  it('save_sets_error_on_api_failure', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    addCreditMock.mockRejectedValue(new Error('Server error'))
    const { wrapper, setNode } = createWrapper()
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
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.showNewForm())
    act(() => result.current.setFormField('formValue', '50'))
    act(() => result.current.saveForm())
    expect(result.current.saveError).not.toBeNull()
    expect(addCreditMock).not.toHaveBeenCalled()
  })

  it('save_validates_value_greater_than_zero', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createWrapper()
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

  it('delete_credit_calls_api_and_updates_asset', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const updatedAsset = { ...ASSET_DETAILS, credits: [CREDIT_B] }
    deleteCreditMock.mockResolvedValue(updatedAsset)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.deleteCredit('aaa'))
    await waitFor(() =>
      expect(deleteCreditMock).toHaveBeenCalledWith({
        brokerName: 'XPI',
        portfolioName: 'Acoes',
        assetName: 'KLBN4',
        id: 'aaa',
      }),
    )
    await waitFor(() => expect(result.current.credits).toEqual([CREDIT_B]))
  })

  it('delete_failure_sets_delete_error', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    deleteCreditMock.mockRejectedValue(new Error('Delete failed'))
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    act(() => result.current.deleteCredit('aaa'))
    await waitFor(() => expect(result.current.deleteError).toBe('Delete failed'))
    expect(result.current.credits).toHaveLength(2)
  })

  it('sorts_credits_by_date_descending', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useCredits(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.credits).toHaveLength(2))
    expect(result.current.credits[0].id).toBe('aaa')
    expect(result.current.credits[1].id).toBe('bbb')
  })
})
