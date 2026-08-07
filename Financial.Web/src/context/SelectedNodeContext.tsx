/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import type { InvestmentScope, SelectedNode, SelectedNodeContextValue } from '../api/types'

const SelectedNodeContext = createContext<SelectedNodeContextValue | null>(null)

export function SelectedNodeProvider({ children, scope = 'active' }: { children: ReactNode; scope?: InvestmentScope }) {
  const [selectedNode, setSelectedNode] = useState<SelectedNode | null>(null)
  const value = useMemo(() => ({ selectedNode, setSelectedNode, scope }), [selectedNode, scope])
  return <SelectedNodeContext.Provider value={value}>{children}</SelectedNodeContext.Provider>
}

export function useSelectedNode(): SelectedNodeContextValue {
  const context = useContext(SelectedNodeContext)
  if (context === null) {
    throw new Error('useSelectedNode must be used within a SelectedNodeProvider')
  }
  return context
}
