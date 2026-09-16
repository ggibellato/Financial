import { useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { resolveHoldingLocation } from '../utils/holdingNavigation'

export interface HoldingNavigation {
  navigateToHolding: (brokerName: string, portfolioName: string, assetName: string) => Promise<boolean>
}

export function useHoldingNavigation(): HoldingNavigation {
  const navigate = useNavigate()

  const navigateToHolding = useCallback(
    async (brokerName: string, portfolioName: string, assetName: string) => {
      let location
      try {
        location = await resolveHoldingLocation(brokerName, portfolioName, assetName)
      } catch {
        return false
      }
      if (!location) return false

      // Router state, not a store: the tree page's selection provider is created fresh on every
      // mount, so the target holding has to travel with the navigation itself.
      navigate(location.route, { state: { pendingSelection: { brokerName, portfolioName, assetName } } })
      return true
    },
    [navigate],
  )

  return { navigateToHolding }
}
