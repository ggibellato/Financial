/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState } from 'react'
import type { ReactNode } from 'react'
import type { SelectedNode, SelectedNodeContextValue } from '../api/types'

const SelectedNodeContext = createContext<SelectedNodeContextValue | null>(null)

export function SelectedNodeProvider({ children }: { children: ReactNode }) {
  const [selectedNode, setSelectedNode] = useState<SelectedNode | null>(null)
  return (
    <SelectedNodeContext.Provider value={{ selectedNode, setSelectedNode }}>
      {children}
    </SelectedNodeContext.Provider>
  )
}

export function useSelectedNode(): SelectedNodeContextValue {
  const context = useContext(SelectedNodeContext)
  if (context === null) {
    throw new Error('useSelectedNode must be used within a SelectedNodeProvider')
  }
  return context
}
