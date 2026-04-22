import { useCallback, useEffect, useMemo, useState } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { TreeNodeDto } from '../api/types'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'

interface TreeNodeProps {
  node: TreeNodeDto
}

interface NavigationTreePanelProps {
  title?: string
  className?: string
}

function TreeNode({ node }: TreeNodeProps) {
  return (
    <li className="nav-tree__node">
      <span className="nav-tree__label">
        {node.displayName} <em>({node.nodeType})</em>
      </span>
      {node.children.length > 0 ? (
        <ul className="nav-tree__list">
          {node.children.map((child) => (
            <TreeNode key={`${child.nodeType}-${child.displayName}`} node={child} />
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
          <TreeNode node={tree} />
        </ul>
      ) : (
        <p>No navigation data available.</p>
      )}
    </div>
  )
}
