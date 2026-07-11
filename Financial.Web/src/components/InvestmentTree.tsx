import { useCallback, useEffect, useMemo, useState } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { SelectedNode, TreeNodeDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import './InvestmentTree.css'

const ASSET_CLASS_OPTIONS: { value: number; label: string }[] = [
  { value: 1, label: 'Equity' },
  { value: 2, label: 'Real Estate' },
  { value: 3, label: 'Bond' },
  { value: 4, label: 'Fund' },
  { value: 5, label: 'ETF' },
  { value: 6, label: 'Cash' },
  { value: 7, label: 'Pension' },
  { value: 8, label: 'Other' },
  { value: 9, label: 'Cryptocurrency' },
]

const ALL_CLASSES = 'all'

function getMetaString(metadata: Record<string, unknown>, key: string): string {
  const v = metadata[key]
  return typeof v === 'string' ? v : ''
}

function getMetaBool(metadata: Record<string, unknown>, key: string): boolean {
  return metadata[key] === true
}

function getMetaNumber(metadata: Record<string, unknown>, key: string): number {
  const v = metadata[key]
  return typeof v === 'number' ? v : -1
}

interface NodeMatch {
  brokerName: string
  portfolioName?: string
}

function nodeMatchesSelected(selected: SelectedNode | null, nodeType: string, match: NodeMatch & { assetName?: string }): boolean {
  if (!selected) return false
  if (selected.nodeType !== nodeType) return false
  if (selected.brokerName !== match.brokerName) return false
  if (nodeType === 'Portfolio' && selected.portfolioName !== match.portfolioName) return false
  if (nodeType === 'Asset' && (selected.portfolioName !== match.portfolioName || selected.assetName !== match.assetName)) return false
  return true
}

interface AssetNodeProps {
  node: TreeNodeDto
  brokerName: string
  portfolioName: string
  filterClass: string
}

function AssetNode({ node, brokerName, portfolioName, filterClass }: AssetNodeProps) {
  const { selectedNode, setSelectedNode } = useSelectedNode()
  const assetName = getMetaString(node.metadata, 'AssetName')
  const ticker = getMetaString(node.metadata, 'Ticker')
  const exchange = getMetaString(node.metadata, 'Exchange')
  const isActive = getMetaBool(node.metadata, 'IsActive')
  const assetClass = getMetaNumber(node.metadata, 'GlobalAssetClass')

  if (filterClass !== ALL_CLASSES && String(assetClass) !== filterClass) return null

  const isSelected = nodeMatchesSelected(selectedNode, 'Asset', { brokerName, portfolioName, assetName })
  const prefix = isActive ? '●' : '○'

  const handleClick = () => {
    setSelectedNode({
      nodeType: 'Asset',
      brokerName,
      portfolioName,
      assetName,
      ticker,
      exchange,
      isActive,
      assetClass: ASSET_CLASS_OPTIONS.find((o) => o.value === assetClass)?.label,
    })
  }

  return (
    <li>
      <button
        className={`investment-tree__node investment-tree__node--asset${isSelected ? ' investment-tree__node--selected' : ''}`}
        onClick={handleClick}
        type="button"
      >
        <span className={`investment-tree__status-icon investment-tree__status-icon--${isActive ? 'active' : 'inactive'}`}>{prefix}</span> {node.displayName}
      </button>
    </li>
  )
}

interface PortfolioNodeProps {
  node: TreeNodeDto
  brokerName: string
  filterClass: string
}

function PortfolioNode({ node, brokerName, filterClass }: PortfolioNodeProps) {
  const { selectedNode, setSelectedNode } = useSelectedNode()
  const [expanded, setExpanded] = useState(false)
  const portfolioName = getMetaString(node.metadata, 'PortfolioName')

  const visibleAssets = node.children.filter((child) => {
    if (child.nodeType !== 'Asset') return false
    if (filterClass === ALL_CLASSES) return true
    return String(getMetaNumber(child.metadata, 'GlobalAssetClass')) === filterClass
  })

  if (filterClass !== ALL_CLASSES && visibleAssets.length === 0) return null

  const isSelected = nodeMatchesSelected(selectedNode, 'Portfolio', { brokerName, portfolioName })

  const handleClick = () => {
    setSelectedNode({ nodeType: 'Portfolio', brokerName, portfolioName })
  }

  return (
    <li>
      <div className="investment-tree__row">
        <button
          className="investment-tree__chevron"
          onClick={() => setExpanded((e) => !e)}
          aria-label={expanded ? 'Collapse' : 'Expand'}
          type="button"
        >
          {expanded ? '▾' : '▸'}
        </button>
        <button
          className={`investment-tree__node${isSelected ? ' investment-tree__node--selected' : ''}`}
          onClick={handleClick}
          type="button"
        >
          {node.displayName}
        </button>
      </div>
      {expanded && visibleAssets.length > 0 && (
        <ul className="investment-tree__list investment-tree__list--children">
          {node.children.map((child) =>
            child.nodeType === 'Asset' ? (
              <AssetNode
                key={child.displayName}
                node={child}
                brokerName={brokerName}
                portfolioName={portfolioName}
                filterClass={filterClass}
              />
            ) : null,
          )}
        </ul>
      )}
    </li>
  )
}

interface BrokerNodeProps {
  node: TreeNodeDto
  filterClass: string
}

function BrokerNode({ node, filterClass }: BrokerNodeProps) {
  const { selectedNode, setSelectedNode } = useSelectedNode()
  const [expanded, setExpanded] = useState(true)
  const brokerName = getMetaString(node.metadata, 'BrokerName')
  const currency = getMetaString(node.metadata, 'Currency')

  const visiblePortfolios = node.children.filter((child) => {
    if (child.nodeType !== 'Portfolio') return false
    if (filterClass === ALL_CLASSES) return true
    return child.children.some(
      (asset) => asset.nodeType === 'Asset' && String(getMetaNumber(asset.metadata, 'GlobalAssetClass')) === filterClass,
    )
  })

  if (filterClass !== ALL_CLASSES && visiblePortfolios.length === 0) return null

  const isSelected = nodeMatchesSelected(selectedNode, 'Broker', { brokerName })

  const handleClick = () => {
    setSelectedNode({ nodeType: 'Broker', brokerName, currency })
  }

  return (
    <li>
      <div className="investment-tree__row">
        <button
          className="investment-tree__chevron"
          onClick={() => setExpanded((e) => !e)}
          aria-label={expanded ? 'Collapse' : 'Expand'}
          type="button"
        >
          {expanded ? '▾' : '▸'}
        </button>
        <button
          className={`investment-tree__node investment-tree__node--broker${isSelected ? ' investment-tree__node--selected' : ''}`}
          onClick={handleClick}
          type="button"
        >
          {node.displayName}
        </button>
      </div>
      {expanded && (
        <ul className="investment-tree__list investment-tree__list--children">
          {node.children.map((child) =>
            child.nodeType === 'Portfolio' ? (
              <PortfolioNode
                key={child.displayName}
                node={child}
                brokerName={brokerName}
                filterClass={filterClass}
              />
            ) : null,
          )}
        </ul>
      )}
    </li>
  )
}

export default function InvestmentTree() {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [tree, setTree] = useState<TreeNodeDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [retryCount, setRetryCount] = useState(0)
  const [filterClass, setFilterClass] = useState(ALL_CLASSES)

  useEffect(() => {
    apiClient
      .getNavigationTree()
      .then((data) => {
        setTree(data)
        setError(null)
      })
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : 'Unable to load investments.')
      })
      .finally(() => setIsLoading(false))
  }, [apiClient, retryCount])

  const handleRetry = useCallback(() => {
    setIsLoading(true)
    setError(null)
    setRetryCount((c) => c + 1)
  }, [])

  return (
    <div className="investment-tree">
      <h2 className="investment-tree__heading">Investments</h2>
      <div className="investment-tree__filter">
        <label htmlFor="asset-class-filter" className="investment-tree__filter-label">
          Asset class
        </label>
        <select
          id="asset-class-filter"
          className="investment-tree__filter-select"
          value={filterClass}
          onChange={(e) => setFilterClass(e.target.value)}
        >
          <option value={ALL_CLASSES}>All</option>
          {ASSET_CLASS_OPTIONS.map((opt) => (
            <option key={opt.value} value={String(opt.value)}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>
      {isLoading ? (
        <LoadingState message="Loading investments..." />
      ) : error ? (
        <ErrorState message={error} onRetry={handleRetry} />
      ) : tree ? (
        <ul className="investment-tree__list">
          {tree.children.map((child) =>
            child.nodeType === 'Broker' ? (
              <BrokerNode key={child.displayName} node={child} filterClass={filterClass} />
            ) : null,
          )}
        </ul>
      ) : null}
    </div>
  )
}
