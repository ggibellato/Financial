import type { AssetAdminDto } from '../api/types'

type ValuationMethod = AssetAdminDto['valuationMethod']

export type TrackingMode = 'quantity' | 'balance'

export const TRACKING_MODE_ORDER: readonly TrackingMode[] = ['quantity', 'balance']

export const TRACKING_MODE_LABELS: Record<TrackingMode, string> = {
  quantity: 'Quantity and price',
  balance: 'Balance',
}

export function initialTrackingMode(asset: AssetAdminDto | null): TrackingMode {
  return asset?.isTrackedByBalance ? 'balance' : 'quantity'
}

export function valuationMethodForCreate(mode: TrackingMode): ValuationMethod | null {
  return mode === 'balance' ? 'ProviderValue' : null
}

export function valuationMethodForUpdate(initial: TrackingMode, chosen: TrackingMode): ValuationMethod | null {
  if (initial === chosen) return null
  return chosen === 'balance' ? 'ProviderValue' : 'Unspecified'
}
