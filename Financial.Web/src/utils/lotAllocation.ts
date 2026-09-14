import type { OpenLotDto } from '../api/types'
import { parseValidatedNumber } from './formatters'

// Quantities go to 8 decimal places (formatN8, step="0.0001"), so splitting one sale across
// multiple lots can hit ordinary floating-point drift (0.1 + 0.2 !== 0.3) - compare with a
// tolerance well below the smallest representable unit instead of exact equality, the same
// pattern reserveBucketSplit.ts already uses for "parts must sum to the whole".
const QUANTITY_TOLERANCE = 0.000000001

export function sumAllocations(allocations: Record<string, string>): number {
  return Object.values(allocations).reduce((sum, value) => sum + (parseValidatedNumber(value) ?? 0), 0)
}

export function isAllocationExact(allocated: number, saleQuantity: number): boolean {
  return saleQuantity > 0 && Math.abs(allocated - saleQuantity) <= QUANTITY_TOLERANCE
}

export function isLotOverAllocated(lot: OpenLotDto, rawValue: string): boolean {
  const value = parseValidatedNumber(rawValue)
  return value !== null && value - lot.remainingQuantity > QUANTITY_TOLERANCE
}

export function hasOverAllocatedLot(openLots: OpenLotDto[], allocations: Record<string, string>): boolean {
  return openLots.some((lot) => isLotOverAllocated(lot, allocations[lot.sourceTransactionId] ?? ''))
}
