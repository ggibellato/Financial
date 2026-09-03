/* eslint-disable react-refresh/only-export-components */
import { createContext, useCallback, useContext, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { getStoredColourMode, setStoredColourMode } from '../utils/colourModeStorage'
import type { ColourMode } from '../utils/colourModeStorage'

interface ColourModeContextValue {
  colourMode: ColourMode
  setColourMode: (mode: ColourMode) => void
  toggleColourMode: () => void
}

const ColourModeContext = createContext<ColourModeContextValue | null>(null)

export function ColourModeProvider({ children }: { children: ReactNode }) {
  const [colourMode, setColourModeState] = useState<ColourMode>(() => getStoredColourMode())

  const setColourMode = useCallback((mode: ColourMode) => {
    setColourModeState(mode)
    setStoredColourMode(mode)
  }, [])

  const toggleColourMode = useCallback(() => {
    setColourModeState((current) => {
      const next = current === 'light' ? 'dark' : 'light'
      setStoredColourMode(next)
      return next
    })
  }, [])

  const value = useMemo(
    () => ({ colourMode, setColourMode, toggleColourMode }),
    [colourMode, setColourMode, toggleColourMode],
  )
  return <ColourModeContext.Provider value={value}>{children}</ColourModeContext.Provider>
}

export function useColourMode(): ColourModeContextValue {
  const context = useContext(ColourModeContext)
  if (context === null) {
    throw new Error('useColourMode must be used within a ColourModeProvider')
  }
  return context
}
