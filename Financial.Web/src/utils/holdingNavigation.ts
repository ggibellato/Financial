import { apiClient } from '../api/financialApiClient'
import type { InvestmentScope, TreeNodeDto } from '../api/types'
import { getMetaString } from './treeNodeMetadata'

export interface PendingSelection {
  brokerName: string
  portfolioName: string
  assetName: string
}

export interface HoldingLocation {
  scope: InvestmentScope
  route: string
}

// Findings carry no scope, so the tree that contains the holding is what decides it. Active is
// searched first because three of the four finding categories can only ever be active holdings.
const SCOPE_LOCATIONS: HoldingLocation[] = [
  { scope: 'active', route: '/investments/active-investments' },
  { scope: 'historic', route: '/investments/historic-investments' },
]

export function findAssetInTree(tree: TreeNodeDto, target: PendingSelection): TreeNodeDto | null {
  for (const broker of tree.children) {
    if (broker.nodeType !== 'Broker') continue
    if (getMetaString(broker.metadata, 'BrokerName') !== target.brokerName) continue

    for (const portfolio of broker.children) {
      if (portfolio.nodeType !== 'Portfolio') continue
      if (getMetaString(portfolio.metadata, 'PortfolioName') !== target.portfolioName) continue

      for (const asset of portfolio.children) {
        if (asset.nodeType !== 'Asset') continue
        if (getMetaString(asset.metadata, 'AssetName') === target.assetName) return asset
      }
    }
  }
  return null
}

export function readPendingSelection(state: unknown): PendingSelection | null {
  if (typeof state !== 'object' || state === null) return null
  const candidate = (state as { pendingSelection?: unknown }).pendingSelection
  if (typeof candidate !== 'object' || candidate === null) return null

  const { brokerName, portfolioName, assetName } = candidate as Partial<PendingSelection>
  if (typeof brokerName !== 'string' || typeof portfolioName !== 'string' || typeof assetName !== 'string') {
    return null
  }
  return { brokerName, portfolioName, assetName }
}

export function readPendingCorporateActionId(state: unknown): string | null {
  if (typeof state !== 'object' || state === null) return null
  const candidate = (state as { pendingCorporateActionId?: unknown }).pendingCorporateActionId
  return typeof candidate === 'string' ? candidate : null
}

export async function resolveHoldingLocation(
  brokerName: string,
  portfolioName: string,
  assetName: string,
): Promise<HoldingLocation | null> {
  const target: PendingSelection = { brokerName, portfolioName, assetName }

  for (const location of SCOPE_LOCATIONS) {
    const tree = await apiClient.getNavigationTree(location.scope)
    if (findAssetInTree(tree, target)) return location
  }
  return null
}
