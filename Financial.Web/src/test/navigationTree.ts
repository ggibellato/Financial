import type { TreeNodeDto } from '../api/types'

export const EMPTY_TREE: TreeNodeDto = {
  nodeType: 'Investments',
  displayName: 'Investments',
  metadata: {},
  children: [],
}

export function treeWithAsset(brokerName: string, portfolioName: string, assetName: string): TreeNodeDto {
  return {
    ...EMPTY_TREE,
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
            children: [{ nodeType: 'Asset', displayName: assetName, metadata: { AssetName: assetName }, children: [] }],
          },
        ],
      },
    ],
  }
}
