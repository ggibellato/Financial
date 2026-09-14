import { renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { OpenLotDto, SelectedNode } from '../../api/types'
import { createSelectedNodeWrapper } from '../../test-utils/selectedNodeTestWrapper'
import { useOpenLots } from '../useOpenLots'

const { getOpenLotsMock } = vi.hoisted(() => ({
  getOpenLotsMock: vi.fn<FinancialApiClient['getOpenLots']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getOpenLots: getOpenLotsMock,
  } as Partial<FinancialApiClient>,
}))

const ASSET_NODE: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'Trading 212',
  portfolioName: 'ISA',
  assetName: 'AAA',
  ticker: 'AAA',
  exchange: 'LSE',
  positionType: 'Long',
}

const BROKER_NODE: SelectedNode = {
  nodeType: 'Broker',
  brokerName: 'Trading 212',
}

const OPEN_LOT: OpenLotDto = {
  sourceTransactionId: 'lot-1',
  date: '2024-06-01T00:00:00',
  remainingQuantity: 15,
  unitCost: 12.5,
}

describe('useOpenLots', () => {
  beforeEach(() => {
    getOpenLotsMock.mockReset()
  })

  it('does not fetch while disabled', () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useOpenLots(false), { wrapper })
    setNode(ASSET_NODE)

    expect(getOpenLotsMock).not.toHaveBeenCalled()
    expect(result.current.openLots).toEqual([])
  })

  it('fetches open lots once enabled for the selected asset', async () => {
    getOpenLotsMock.mockResolvedValue([OPEN_LOT])
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useOpenLots(true), { wrapper })
    setNode(ASSET_NODE)

    await waitFor(() => expect(getOpenLotsMock).toHaveBeenCalledWith('Trading 212', 'ISA', 'AAA', 'active'))
    await waitFor(() => expect(result.current.openLots).toEqual([OPEN_LOT]))
  })

  it('does not fetch for a non-asset node even when enabled', () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useOpenLots(true), { wrapper })
    setNode(BROKER_NODE)

    expect(getOpenLotsMock).not.toHaveBeenCalled()
  })

  it('resets to empty when disabled again', async () => {
    getOpenLotsMock.mockResolvedValue([OPEN_LOT])
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result, rerender } = renderHook(({ enabled }) => useOpenLots(enabled), {
      wrapper,
      initialProps: { enabled: true },
    })
    setNode(ASSET_NODE)

    await waitFor(() => expect(result.current.openLots).toEqual([OPEN_LOT]))

    rerender({ enabled: false })

    await waitFor(() => expect(result.current.openLots).toEqual([]))
  })

  it('surfaces a fetch error', async () => {
    getOpenLotsMock.mockRejectedValue(new Error('Network down'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useOpenLots(true), { wrapper })
    setNode(ASSET_NODE)

    await waitFor(() => expect(result.current.error).toBe('Network down'))
    expect(result.current.isLoading).toBe(false)
  })
})
