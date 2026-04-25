import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { TreeNodeDto } from '../api/types'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'

interface TreeNodeProps {
  node: TreeNodeDto
  context: TreeNodeContext
}

interface NavigationTreePanelProps {
  title?: string
  className?: string
}

interface TreeNodeContext {
  brokerName?: string
  portfolioName?: string
}

const getMetadataString = (metadata: Record<string, unknown>, key: string) => {
  const value = metadata[key]
  return typeof value === 'string' ? value : undefined
}

function TreeNode({ node, context }: TreeNodeProps) {
  const brokerName =
    node.nodeType === 'Broker' ? getMetadataString(node.metadata, 'BrokerName') : context.brokerName
  const portfolioName =
    node.nodeType === 'Portfolio' ? getMetadataString(node.metadata, 'PortfolioName') : context.portfolioName
  const assetName = node.nodeType === 'Asset' ? getMetadataString(node.metadata, 'AssetName') : undefined
  const nextContext = { brokerName, portfolioName }
  const destination =
    node.nodeType === 'Investments'
      ? '/brokers'
      : node.nodeType === 'Broker' && brokerName
        ? `/brokers/${encodeURIComponent(brokerName)}`
        : node.nodeType === 'Portfolio' && brokerName
          ? `/brokers/${encodeURIComponent(brokerName)}`
          : node.nodeType === 'Asset' && brokerName && portfolioName && assetName
            ? `/assets/${encodeURIComponent(brokerName)}/${encodeURIComponent(
                portfolioName,
              )}/${encodeURIComponent(assetName)}`
            : undefined
  const label = (
    <>
      {node.displayName} <em>({node.nodeType})</em>
    </>
  )

  return (
    <li className="nav-tree__node">
      {destination ? (
        <Link
          className="nav-tree__link"
          to={destination}
          state={node.nodeType === 'Asset' ? { defaultTab: 'summary' } : undefined}
        >
          {label}
        </Link>
      ) : (
        <span className="nav-tree__label">{label}</span>
      )}
      {node.children.length > 0 ? (
        <ul className="nav-tree__list">
          {node.children.map((child) => (
            <TreeNode
              key={`${child.nodeType}-${child.displayName}`}
              node={child}
              context={nextContext}
            />
          ))}
        </ul>
      ) : null}
    </li>
  )
}

export default function NavigationTreePanel({ title = 'Navigation', className }: NavigationTreePanelProps) {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [tree, setTree] = useState<TreeNodeDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadTree = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    try {
      const data = await apiClient.getNavigationTree()
      setTree(data)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unable to load navigation tree.'
      setError(message)
    } finally {
      setIsLoading(false)
    }
  }, [apiClient])

  useEffect(() => {
    void loadTree()
  }, [loadTree])

  return (
    <div className={['nav-tree', className].filter(Boolean).join(' ')}>
      <div className="nav-tree__header">
        <h2>{title}</h2>
      </div>
      {isLoading ? (
        <LoadingState message="Loading navigation tree..." />
      ) : error ? (
        <ErrorState message={error} onRetry={loadTree} />
      ) : tree ? (
        <ul className="nav-tree__list">
          <TreeNode node={tree} context={{}} />
        </ul>
      ) : (
        <p>No navigation data available.</p>
      )}
    </div>
  )
}
