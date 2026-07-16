import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { PortfolioBreakdownItemDto, SelectedNode } from '../api/types'
import { createSelectedNodeWrapper } from '../test-utils/selectedNodeTestWrapper'
import { useBrokerBreakdown } from './useBrokerBreakdown'

const getBrokerBreakdownMock = vi.fn<FinancialApiClient['getBrokerBreakdown']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getBrokerBreakdown: getBrokerBreakdownMock,
  }),
}))

const BROKER_NODE: SelectedNode = {
  nodeType: 'Broker',
  brokerName: 'XPI',
  currency: 'BRL',
}

const PORTFOLIO_NODE: SelectedNode = {
  nodeType: 'Portfolio',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
}

const ASSET_NODE: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetName: 'KLBN4',
  ticker: 'KLBN4',
  exchange: 'BVMF',
}

const BREAKDOWN_DTO: PortfolioBreakdownItemDto[] = [
  {
    portfolioName: 'Acoes',
    totalInvested: 38639.49,
    assets: [
      { assetName: 'BBAS3', totalInvested: 9850.4 },
      { assetName: 'KLBN4', totalInvested: 3737.48 },
    ],
  },
]

describe('useBrokerBreakdown', () => {
  beforeEach(() => {
    getBrokerBreakdownMock.mockReset()
  })

  it('calls_getBrokerBreakdown_on_broker_node_selection', async () => {
    getBrokerBreakdownMock.mockResolvedValue(BREAKDOWN_DTO)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useBrokerBreakdown(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => {
      expect(getBrokerBreakdownMock).toHaveBeenCalledWith('XPI', 'active')
    })
  })

  it('does_not_fetch_on_portfolio_node_selection', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useBrokerBreakdown(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await act(async () => {})
    expect(getBrokerBreakdownMock).not.toHaveBeenCalled()
  })

  it('does_not_fetch_on_asset_node_selection', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useBrokerBreakdown(), { wrapper })
    setNode(ASSET_NODE)
    await act(async () => {})
    expect(getBrokerBreakdownMock).not.toHaveBeenCalled()
  })

  it('sets_isLoading_true_while_fetch_is_in_progress', async () => {
    getBrokerBreakdownMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useBrokerBreakdown(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(true))
  })

  it('populates_breakdown_on_successful_fetch', async () => {
    getBrokerBreakdownMock.mockResolvedValue(BREAKDOWN_DTO)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useBrokerBreakdown(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.breakdown).not.toBeNull())
    expect(result.current.breakdown).toEqual(BREAKDOWN_DTO)
  })

  it('sets_error_on_fetch_failure', async () => {
    getBrokerBreakdownMock.mockRejectedValue(new Error('Network error'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useBrokerBreakdown(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.error).toBe('Network error'))
    expect(result.current.breakdown).toBeNull()
  })

  it('retry_re_triggers_fetch', async () => {
    getBrokerBreakdownMock.mockRejectedValueOnce(new Error('Fail'))
    getBrokerBreakdownMock.mockResolvedValue(BREAKDOWN_DTO)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useBrokerBreakdown(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.error).toBe('Fail'))

    act(() => result.current.retry())
    await waitFor(() => expect(result.current.breakdown).not.toBeNull())
    expect(getBrokerBreakdownMock).toHaveBeenCalledTimes(2)
  })

  it('resets_state_on_node_change_to_non_broker', async () => {
    getBrokerBreakdownMock.mockResolvedValue(BREAKDOWN_DTO)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useBrokerBreakdown(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.breakdown).not.toBeNull())

    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.breakdown).toBeNull())
  })

  it('does_not_fetch_when_no_node_selected', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useBrokerBreakdown(), { wrapper })
    setNode(null)
    await act(async () => {})
    expect(getBrokerBreakdownMock).not.toHaveBeenCalled()
    expect(result.current.breakdown).toBeNull()
  })
})
