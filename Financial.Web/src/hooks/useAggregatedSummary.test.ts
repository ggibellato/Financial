import { act, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { createElement } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { AggregatedSummaryDto, SelectedNode } from '../api/types'
import { SelectedNodeProvider, useSelectedNode } from '../context/SelectedNodeContext'
import { useAggregatedSummary } from './useAggregatedSummary'

const getSummaryByBrokerMock = vi.fn<FinancialApiClient['getSummaryByBroker']>()
const getSummaryByPortfolioMock = vi.fn<FinancialApiClient['getSummaryByPortfolio']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getSummaryByBroker: getSummaryByBrokerMock,
    getSummaryByPortfolio: getSummaryByPortfolioMock,
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

const SUMMARY_DTO: AggregatedSummaryDto = {
  totalBought: 15420.5,
  totalSold: 3200.0,
  totalCredits: 842.3,
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
    setNode: (node: SelectedNode | null) => act(() => { setNodeRef?.(node) }),
  }
}

describe('useAggregatedSummary', () => {
  beforeEach(() => {
    getSummaryByBrokerMock.mockReset()
    getSummaryByPortfolioMock.mockReset()
  })

  it('calls_getSummaryByBroker_on_broker_node_selection', async () => {
    getSummaryByBrokerMock.mockResolvedValue(SUMMARY_DTO)
    const { wrapper, setNode } = createWrapper()
    renderHook(() => useAggregatedSummary(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => {
      expect(getSummaryByBrokerMock).toHaveBeenCalledWith('XPI')
    })
    expect(getSummaryByPortfolioMock).not.toHaveBeenCalled()
  })

  it('calls_getSummaryByPortfolio_on_portfolio_node_selection', async () => {
    getSummaryByPortfolioMock.mockResolvedValue(SUMMARY_DTO)
    const { wrapper, setNode } = createWrapper()
    renderHook(() => useAggregatedSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => {
      expect(getSummaryByPortfolioMock).toHaveBeenCalledWith('XPI', 'Acoes')
    })
    expect(getSummaryByBrokerMock).not.toHaveBeenCalled()
  })

  it('sets_isLoading_true_while_fetch_is_in_progress', async () => {
    getSummaryByBrokerMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAggregatedSummary(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(true))
  })

  it('populates_summary_on_successful_fetch', async () => {
    getSummaryByBrokerMock.mockResolvedValue(SUMMARY_DTO)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAggregatedSummary(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.summary).not.toBeNull())
    expect(result.current.summary?.totalBought).toBe(15420.5)
    expect(result.current.summary?.totalSold).toBe(3200.0)
    expect(result.current.summary?.totalCredits).toBe(842.3)
  })

  it('sets_error_on_fetch_failure', async () => {
    getSummaryByBrokerMock.mockRejectedValue(new Error('Network error'))
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAggregatedSummary(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.error).toBe('Network error'))
    expect(result.current.summary).toBeNull()
  })

  it('resets_state_on_node_change', async () => {
    getSummaryByBrokerMock.mockResolvedValue(SUMMARY_DTO)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAggregatedSummary(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.summary).not.toBeNull())

    getSummaryByPortfolioMock.mockReturnValue(new Promise(() => {}))
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(true))
    expect(result.current.summary).toBeNull()
  })

  it('retry_re_triggers_fetch', async () => {
    getSummaryByBrokerMock.mockRejectedValueOnce(new Error('Fail'))
    getSummaryByBrokerMock.mockResolvedValue(SUMMARY_DTO)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAggregatedSummary(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.error).toBe('Fail'))

    act(() => result.current.retry())
    await waitFor(() => expect(result.current.summary).not.toBeNull())
    expect(getSummaryByBrokerMock).toHaveBeenCalledTimes(2)
  })

  it('does_not_fetch_when_asset_node_selected', async () => {
    const { wrapper, setNode } = createWrapper()
    renderHook(() => useAggregatedSummary(), { wrapper })
    setNode(ASSET_NODE)
    await act(async () => {})
    expect(getSummaryByBrokerMock).not.toHaveBeenCalled()
    expect(getSummaryByPortfolioMock).not.toHaveBeenCalled()
  })

  it('does_not_fetch_when_no_node_selected', async () => {
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAggregatedSummary(), { wrapper })
    setNode(null)
    await act(async () => {})
    expect(getSummaryByBrokerMock).not.toHaveBeenCalled()
    expect(result.current.summary).toBeNull()
  })
})
