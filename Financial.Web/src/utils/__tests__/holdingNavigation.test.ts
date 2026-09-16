import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { TreeNodeDto } from '../../api/types'
import { findAssetInTree, readPendingSelection, resolveHoldingLocation } from '../holdingNavigation'

const { getNavigationTreeMock } = vi.hoisted(() => ({
  getNavigationTreeMock: vi.fn<FinancialApiClient['getNavigationTree']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getNavigationTree: getNavigationTreeMock,
  } as Partial<FinancialApiClient>,
}))

function makeTree(brokerName: string, portfolioName: string, assetName: string): TreeNodeDto {
  return {
    nodeType: 'Investments',
    displayName: 'Investments',
    metadata: {},
    children: [
      {
        nodeType: 'Broker',
        displayName: brokerName,
        metadata: { BrokerName: brokerName },
        children: [
          {
            nodeType: 'Portfolio',
            displayName: portfolioName,
            metadata: { PortfolioName: portfolioName },
            children: [
              {
                nodeType: 'Asset',
                displayName: assetName,
                metadata: { AssetName: assetName },
                children: [],
              },
            ],
          },
        ],
      },
    ],
  }
}

const EMPTY_TREE: TreeNodeDto = {
  nodeType: 'Investments',
  displayName: 'Investments',
  metadata: {},
  children: [],
}

describe('resolveHoldingLocation', () => {
  beforeEach(() => {
    getNavigationTreeMock.mockReset()
  })

  it('resolves_the_active_scope_when_the_holding_is_in_the_active_tree', async () => {
    getNavigationTreeMock.mockResolvedValue(makeTree('Trading212', 'ISA', 'VUSA'))

    await expect(resolveHoldingLocation('Trading212', 'ISA', 'VUSA')).resolves.toEqual({
      scope: 'active',
      route: '/investments/active-investments',
    })
  })

  it('does_not_search_the_historic_tree_when_the_active_search_matches', async () => {
    getNavigationTreeMock.mockResolvedValue(makeTree('Trading212', 'ISA', 'VUSA'))

    await resolveHoldingLocation('Trading212', 'ISA', 'VUSA')

    expect(getNavigationTreeMock).toHaveBeenCalledTimes(1)
    expect(getNavigationTreeMock).toHaveBeenCalledWith('active')
  })

  it('falls_through_to_the_historic_tree_when_the_holding_is_not_active', async () => {
    getNavigationTreeMock
      .mockResolvedValueOnce(EMPTY_TREE)
      .mockResolvedValueOnce(makeTree('XPI', 'Acoes', 'KLBN4'))

    await expect(resolveHoldingLocation('XPI', 'Acoes', 'KLBN4')).resolves.toEqual({
      scope: 'historic',
      route: '/investments/historic-investments',
    })
    expect(getNavigationTreeMock).toHaveBeenNthCalledWith(2, 'historic')
  })

  it('resolves_null_when_the_holding_is_in_neither_tree', async () => {
    getNavigationTreeMock.mockResolvedValue(EMPTY_TREE)

    await expect(resolveHoldingLocation('XPI', 'Acoes', 'KLBN4')).resolves.toBeNull()
    expect(getNavigationTreeMock).toHaveBeenCalledTimes(2)
  })

  it('requires_the_broker_portfolio_and_asset_names_to_all_match', async () => {
    getNavigationTreeMock.mockResolvedValue(makeTree('Trading212', 'ISA', 'VUSA'))

    await expect(resolveHoldingLocation('Trading212', 'GIA', 'VUSA')).resolves.toBeNull()
  })
})

describe('findAssetInTree', () => {
  it('returns_the_matching_asset_node', () => {
    const tree = makeTree('Trading212', 'ISA', 'VUSA')

    expect(findAssetInTree(tree, { brokerName: 'Trading212', portfolioName: 'ISA', assetName: 'VUSA' })?.displayName).toBe(
      'VUSA',
    )
  })

  it('returns_null_when_no_asset_matches', () => {
    const tree = makeTree('Trading212', 'ISA', 'VUSA')

    expect(findAssetInTree(tree, { brokerName: 'Trading212', portfolioName: 'ISA', assetName: 'VWRL' })).toBeNull()
  })
})

describe('readPendingSelection', () => {
  it('reads_a_well_formed_pending_selection', () => {
    const state = { pendingSelection: { brokerName: 'XPI', portfolioName: 'Acoes', assetName: 'KLBN4' } }

    expect(readPendingSelection(state)).toEqual({ brokerName: 'XPI', portfolioName: 'Acoes', assetName: 'KLBN4' })
  })

  it.each([
    ['null state', null],
    ['a string state', 'pendingSelection'],
    ['an unrelated state', { other: true }],
    ['a partial selection', { pendingSelection: { brokerName: 'XPI' } }],
    ['a non-string field', { pendingSelection: { brokerName: 1, portfolioName: 'Acoes', assetName: 'KLBN4' } }],
  ])('returns_null_for_%s', (_label, state) => {
    expect(readPendingSelection(state)).toBeNull()
  })
})
