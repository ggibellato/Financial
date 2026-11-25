import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BrokerHoldingsDto, SelectedNode } from '../../api/types'
import { createSelectedNodeWrapper } from '../../test-utils/selectedNodeTestWrapper'
import { useBrokerHoldings } from '../useBrokerHoldings'

const { getBrokerHoldingsMock } = vi.hoisted(() => ({
  getBrokerHoldingsMock: vi.fn<FinancialApiClient['getBrokerHoldings']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { getBrokerHoldings: getBrokerHoldingsMock } as Partial<FinancialApiClient>,
}))

const BROKER_NODE: SelectedNode = { nodeType: 'Broker', brokerName: 'XPI', currency: 'BRL' }
const PORTFOLIO_NODE: SelectedNode = { nodeType: 'Portfolio', brokerName: 'XPI', portfolioName: 'Acoes' }

const EMPTY_TOTALS = {
  holdingCount: 0,
  unvaluedHoldingCount: 0,
  invested: 0,
  marketValue: null,
  investedShareOfBase: null,
  marketShareOfBase: null,
  profitAmount: null,
  profitPercent: null,
  profitWithCreditsPercent: null,
  totalCredits: 0,
  creditsPercentOfInvested: null,
  lastCreditAmount: 0,
  lastCreditPercentOfMarketValue: null,
  lastMonthStartValue: null,
  thisMonthStartValue: null,
  monthChangePercent: null,
  monthStartExcludedCount: 0,
}
const MONTH = { date: '2026-11-01', earliestPriceDate: '2026-10-25' }
const HOLDINGS: BrokerHoldingsDto = { currencies: ['BRL'], lastMonthStart: MONTH, thisMonthStart: MONTH, groups: [], total: EMPTY_TOTALS }

describe('useBrokerHoldings', () => {
  beforeEach(() => {
    getBrokerHoldingsMock.mockReset()
  })

  it('loads_the_holdings_of_the_selected_broker', async () => {
    getBrokerHoldingsMock.mockResolvedValue(HOLDINGS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useBrokerHoldings(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.holdings).toEqual(HOLDINGS))
    expect(getBrokerHoldingsMock).toHaveBeenCalledWith('XPI')
  })

  it('does_not_fetch_for_a_portfolio_node', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useBrokerHoldings(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await act(async () => {})
    expect(getBrokerHoldingsMock).not.toHaveBeenCalled()
  })

  it('reloads_when_the_tree_reselects_the_same_broker', async () => {
    getBrokerHoldingsMock.mockResolvedValue(HOLDINGS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useBrokerHoldings(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.holdings).toEqual(HOLDINGS))

    setNode({ ...BROKER_NODE })

    await waitFor(() => expect(getBrokerHoldingsMock).toHaveBeenCalledTimes(2))
  })

  it('reports_a_failed_load', async () => {
    getBrokerHoldingsMock.mockRejectedValue(new Error('Network error'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useBrokerHoldings(), { wrapper })
    setNode(BROKER_NODE)
    await waitFor(() => expect(result.current.error).toBe('Network error'))
    expect(result.current.holdings).toBeNull()
  })
})
