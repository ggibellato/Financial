import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetDetailsDto, RealisedGainDto, SelectedNode } from '../../api/types'
import { createSelectedNodeWrapper } from '../../test-utils/selectedNodeTestWrapper'
import { ALL_TAX_YEARS, useRealisedGains } from '../useRealisedGains'

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

function realisedGain(overrides: Partial<RealisedGainDto>): RealisedGainDto {
  return {
    id: 'g1',
    transactionId: 't1',
    date: '2025-11-03T00:00:00',
    method: 'AverageCost',
    lotsConsumed: [{ sourceTransactionId: null, quantity: 5, unitCost: 100 }],
    quantitySold: 5,
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
  currency: 'GBP',
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
  realisedGainLoss: 100,
  realisedGainLossSharesOnly: 100,
  marketValue: null,
  costOfUnitsHeld: 500,
  unrealisedGain: null,
  resultPercent: null,
  resultWithCreditsPercent: null,
  priceAsOfDate: null,
  marketStatus: 'Current',
  priceOnlyReturn: null,
  totalReturn: null,
  transactions: [],
  credits: [],
  priceSnapshots: [],
  realisedGains: [],
  corporateActions: [],
  costBasisMethod: 'AverageCost',
  taxJurisdictions: [],
}

describe('useRealisedGains', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset()
  })

  it('returns initial empty state', () => {
    const { wrapper } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useRealisedGains(), { wrapper })
    expect(result.current.isLoading).toBe(false)
    expect(result.current.chains).toEqual([])
    expect(result.current.error).toBeNull()
    expect(result.current.selectedTaxYear).toBe(ALL_TAX_YEARS)
  })

  it('fetches realised gains on asset selection', async () => {
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, realisedGains: [realisedGain({})] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useRealisedGains(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalledWith('Trading 212', 'ISA', 'AAA', 'active'))
    await waitFor(() => expect(result.current.chains).toHaveLength(1))
  })

  it('does not fetch on broker selection', () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useRealisedGains(), { wrapper })
    setNode(BROKER_NODE)
    expect(getAssetDetailsMock).not.toHaveBeenCalled()
    expect(result.current.chains).toEqual([])
  })

  it('resets chains when node is null', async () => {
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, realisedGains: [realisedGain({})] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useRealisedGains(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.chains).toHaveLength(1))
    setNode(null)
    await waitFor(() => expect(result.current.chains).toEqual([]))
  })

  it('retry refetches after a failure', async () => {
    getAssetDetailsMock.mockRejectedValueOnce(new Error('Network error'))
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, realisedGains: [realisedGain({})] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useRealisedGains(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.error).toBe('Network error'))
    act(() => result.current.retry())
    await waitFor(() => expect(result.current.chains).toHaveLength(1))
  })

  it('groups a superseded record and its replacement into one chain, sorted newest-created-first', async () => {
    const first = realisedGain({ id: 'g0', createdAt: '2025-10-01T09:00:00Z', status: 'Superseded', supersededByRecordId: 'g1' })
    const original = realisedGain({ id: 'g1', createdAt: '2025-11-03T09:00:00Z', status: 'Superseded', supersededByRecordId: 'g2' })
    const replacement = realisedGain({ id: 'g2', createdAt: '2025-12-01T09:00:00Z', status: 'Active', method: 'FIFO' })
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, realisedGains: [first, replacement, original] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useRealisedGains(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.chains).toHaveLength(1))

    const chain = result.current.chains[0]
    expect(chain.active.id).toBe('g2')
    expect(chain.history).toEqual([original, first])
  })

  it('excludes a transaction group with no Active record (retired with nothing to show)', async () => {
    const retired = realisedGain({ id: 'g1', status: 'Superseded', supersededByRecordId: null })
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, realisedGains: [retired] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useRealisedGains(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(getAssetDetailsMock).toHaveBeenCalled())
    expect(result.current.chains).toEqual([])
  })

  it('[AC P50-F04-react-disposals-and-cost-basis-02] derives tax year options from the active realised gains, newest first', async () => {
    const older = realisedGain({ id: 'g1', transactionId: 't1', taxYear: '2024/25', date: '2024-06-01T00:00:00' })
    const newer = realisedGain({ id: 'g2', transactionId: 't2', taxYear: '2025/26', date: '2025-11-03T00:00:00' })
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, realisedGains: [older, newer] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useRealisedGains(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.chains).toHaveLength(2))
    expect(result.current.taxYearOptions).toEqual(['2025/26', '2024/25'])
  })

  it('setTaxYear filters chains to the selected tax year', async () => {
    const older = realisedGain({ id: 'g1', transactionId: 't1', taxYear: '2024/25', date: '2024-06-01T00:00:00' })
    const newer = realisedGain({ id: 'g2', transactionId: 't2', taxYear: '2025/26', date: '2025-11-03T00:00:00' })
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, realisedGains: [older, newer] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useRealisedGains(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.chains).toHaveLength(2))

    expect(result.current.filteredChains).toEqual(result.current.chains)

    act(() => result.current.setTaxYear('2024/25'))

    expect(result.current.filteredChains).toHaveLength(1)
    expect(result.current.filteredChains[0].active.id).toBe('g1')
  })

  it('toggleExpanded tracks expanded realised gain ids', async () => {
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, realisedGains: [realisedGain({ id: 'g1' })] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useRealisedGains(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.chains).toHaveLength(1))

    act(() => result.current.toggleExpanded('g1'))
    expect(result.current.expandedIds.has('g1')).toBe(true)

    act(() => result.current.toggleExpanded('g1'))
    expect(result.current.expandedIds.has('g1')).toBe(false)
  })

  it('starts from all years with nothing expanded when another asset is selected', async () => {
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, realisedGains: [realisedGain({ id: 'g1' })] })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useRealisedGains(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.chains).toHaveLength(1))
    act(() => result.current.setTaxYear('2024/25'))
    act(() => result.current.toggleExpanded('g1'))

    setNode({ ...ASSET_NODE, assetName: 'TRPL4' })

    await waitFor(() => expect(result.current.selectedTaxYear).toBe(ALL_TAX_YEARS))
    expect(result.current.expandedIds.size).toBe(0)
  })
})
