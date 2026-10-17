import type { SelectedNode } from '../api/types'

export type AssetSelection = SelectedNode & { nodeType: 'Asset'; portfolioName: string; assetName: string }

export function isAssetSelection(node: SelectedNode | null): node is AssetSelection {
  return node !== null && node.nodeType === 'Asset' && !!node.portfolioName && !!node.assetName
}
