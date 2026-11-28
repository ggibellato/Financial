import { describe, expect, it } from 'vitest'
import { initialTrackingMode, valuationMethodForCreate, valuationMethodForUpdate } from '../trackingMode'
import type { AssetAdminDto } from '../../api/types'

describe('trackingMode', () => {
  it('starts a new asset as Quantity and price and an existing one from its flag', () => {
    expect(initialTrackingMode(null)).toBe('quantity')
    expect(initialTrackingMode({ isTrackedByBalance: true } as AssetAdminDto)).toBe('balance')
    expect(initialTrackingMode({ isTrackedByBalance: false } as AssetAdminDto)).toBe('quantity')
  })

  it('creates a Balance holding as ProviderValue and leaves Quantity and price to the default', () => {
    expect(valuationMethodForCreate('balance')).toBe('ProviderValue')
    expect(valuationMethodForCreate('quantity')).toBeNull()
  })

  it('sends a method on update only when the mode changed', () => {
    expect(valuationMethodForUpdate('balance', 'balance')).toBeNull()
    expect(valuationMethodForUpdate('quantity', 'quantity')).toBeNull()
    expect(valuationMethodForUpdate('quantity', 'balance')).toBe('ProviderValue')
    expect(valuationMethodForUpdate('balance', 'quantity')).toBe('Unspecified')
  })
})
