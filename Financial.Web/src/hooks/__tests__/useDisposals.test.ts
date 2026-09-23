import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetDetailsDto, DisposalRecordDto, SelectedNode } from '../../api/types'
import { createSelectedNodeWrapper } from '../../test-utils/selectedNodeTestWrapper'
import { ALL_TAX_YEARS, useDisposals } from '../useDisposals'

const { getAssetDetailsMock } = vi.hoisted(() => ({
  getAssetDetailsMock: vi.fn<FinancialApiClient['getAssetDetails']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAssetDetails: getAssetDetailsMock,
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

function disposal(overrides: Partial<DisposalRecordDto>): DisposalRecordDto {
  return {
    id: 'd1',
    transactionId: 't1',
    date: '2025-11-03T00:00:00',
    method: 'AverageCost',
    lotsConsumed: [{ sourceTransactionId: null, quantity: 5, unitCost: 100 }],
    quantityDisposed: 5,
    proceeds: 600,
    costBasis: 500,
    gainLoss: 100,
    currency: 'GBP',
    taxYear: '2025/26',
    status: 'Active',
    supersededByRecordId: null,
    createdAt: '2025-11-03T09:00:00Z',
    ...overrides,
  }
}

const ASSET_DETAILS: AssetDetailsDto = {
  name: 'AAA',
  brokerName: 'Trading 212',
  portfolioName: 'ISA',
  ticker: 'AAA',
  isin: 'ISIN-A',
  exchange: 'LSE',
  country: 'UK',
  localTypeCode: '',
  class: 'Equity',
  valuationMethod: 'Unspecified',
  incomePolicy: 'Unknown',
  quantity: 5,
  averagePrice: 100,
  averageSellPrice: null,
  positionType: 'Long',
  totalBought: 1000,
  totalSold: 600,
  totalCredits: 0,
  realizedGainLoss: 100,
  marketValue: null,
  costOfUnitsHeld: 500,
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
  corporateActions: [],
  costBasisMethod: 'AverageCost',
  taxJurisdictions: [],
}

describe('useDisposals', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset()
  })

  it('returns initial empty state', () => {
    const { wrapper } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useDisposals(), { wrapper })
    expect(result.current.isLoading).toBe(false)
    expect(result.current.chains).toEqual([])
    expect(result.current.error).toBeNull()
    expect(result.current.selectedTaxYear).toBe(ALL_TAX_YEARS)
  })

  it('fetches disposals on asset selection', async () => {
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, disposalRecords: [disposal({})] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useDisposals(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('Trading 212', 'ISA', 'AAA', 'active'))
    await waitFor(() => expect(result.current.chains).toHaveLength(1))
  })

  it('does not fetch on broker selection', () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useDisposals(), { wrapper })
    setNode(BROKER_NODE)
    expect(getAssetDetailsMock).not.toHaveBeenCalled()
    expect(result.current.chains).toEqual([])
  })

  it('resets chains when node is null', async () => {
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, disposalRecords: [disposal({})] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useDisposals(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.chains).toHaveLength(1))
    setNode(null)
    await waitFor(() => expect(result.current.chains).toEqual([]))
  })

  it('retry refetches after a failure', async () => {
    getAssetDetailsMock.mockRejectedValueOnce(new Error('Network error'))
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, disposalRecords: [disposal({})] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useDisposals(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.error).toBe('Network error'))
    act(() => result.current.retry())
    await waitFor(() => expect(result.current.chains).toHaveLength(1))
  })

  it('groups a superseded record and its replacement into one chain, sorted newest-created-first', async () => {
    const original = disposal({ id: 'd1', createdAt: '2025-11-03T09:00:00Z', status: 'Superseded', supersededByRecordId: 'd2' })
    const replacement = disposal({ id: 'd2', createdAt: '2025-12-01T09:00:00Z', status: 'Active', method: 'FIFO' })
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, disposalRecords: [original, replacement] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useDisposals(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.chains).toHaveLength(1))

    const chain = result.current.chains[0]
    expect(chain.active.id).toBe('d2')
    expect(chain.history).toEqual([original])
  })

  it('excludes a transaction group with no Active record (retired with nothing to show)', async () => {
    const retired = disposal({ id: 'd1', status: 'Superseded', supersededByRecordId: null })
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, disposalRecords: [retired] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useDisposals(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalled())
    expect(result.current.chains).toEqual([])
  })

  it('derives tax year options from the active disposals, newest first', async () => {
    const older = disposal({ id: 'd1', transactionId: 't1', taxYear: '2024/25', date: '2024-06-01T00:00:00' })
    const newer = disposal({ id: 'd2', transactionId: 't2', taxYear: '2025/26', date: '2025-11-03T00:00:00' })
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, disposalRecords: [older, newer] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useDisposals(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.chains).toHaveLength(2))
    expect(result.current.taxYearOptions).toEqual(['2025/26', '2024/25'])
  })

  it('setTaxYear filters chains to the selected tax year', async () => {
    const older = disposal({ id: 'd1', transactionId: 't1', taxYear: '2024/25', date: '2024-06-01T00:00:00' })
    const newer = disposal({ id: 'd2', transactionId: 't2', taxYear: '2025/26', date: '2025-11-03T00:00:00' })
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, disposalRecords: [older, newer] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useDisposals(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.chains).toHaveLength(2))

    act(() => result.current.setTaxYear('2024/25'))

    expect(result.current.filteredChains).toHaveLength(1)
    expect(result.current.filteredChains[0].active.id).toBe('d1')
  })

  it('toggleExpanded tracks expanded disposal ids', async () => {
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, disposalRecords: [disposal({ id: 'd1' })] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useDisposals(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.chains).toHaveLength(1))

    act(() => result.current.toggleExpanded('d1'))
    expect(result.current.expandedIds.has('d1')).toBe(true)

    act(() => result.current.toggleExpanded('d1'))
    expect(result.current.expandedIds.has('d1')).toBe(false)
  })
})
