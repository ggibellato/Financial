import { useCallback, useEffect, useMemo, useState } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { TreeNodeDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'

interface TreeNodeProps {
  node: TreeNodeDto
}

function TreeNode({ node }: TreeNodeProps) {
  return (
    <li>
      <span>
        {node.displayName} <em>({node.nodeType})</em>
      </span>
      {node.children.length > 0 ? (
        <ul>
          {node.children.map((child) => (
            <TreeNode key={`${child.nodeType}-${child.displayName}`} node={child} />
          ))}
        </ul>
      ) : null}
    </li>
  )
}

export default function NavigationTreePage() {
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

  if (isLoading) {
    return <LoadingState message="Loading navigation tree..." />
  }

  if (error) {
    return <ErrorState message={error} onRetry={loadTree} />
  }

  if (!tree) {
    return <p>No navigation data available.</p>
  }

  return (
    <section>
      <h2>Navigation Tree</h2>
      <ul>
        <TreeNode node={tree} />
      </ul>
    </section>
  )
}
