/* eslint-disable react-refresh/only-export-components */
import { createContext, useCallback, useContext, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import type { InvestmentScope, SelectedNode, SelectedNodeContextValue } from '../api/types'

const SelectedNodeContext = createContext<SelectedNodeContextValue | null>(null)

export function SelectedNodeProvider({ children, scope = 'active' }: { children: ReactNode; scope?: InvestmentScope }) {
  const [selectedNode, setSelectedNode] = useState<SelectedNode | null>(null)

  // The tree is built from a snapshot of the graph, so it does not observe a move. Bumping this
  // token is how a mutation asks for a fresh one.
  const [reloadToken, setReloadToken] = useState(0)
  const reload = useCallback(() => setReloadToken((token) => token + 1), [])

  const value = useMemo(
    () => ({ selectedNode, setSelectedNode, scope, reload, reloadToken }),
    [selectedNode, scope, reload, reloadToken],
  )
  return <SelectedNodeContext.Provider value={value}>{children}</SelectedNodeContext.Provider>
}

export function useSelectedNode(): SelectedNodeContextValue {
  const context = useContext(SelectedNodeContext)
  if (context === null) {
    throw new Error('useSelectedNode must be used within a SelectedNodeProvider')
  }
  return context
}
