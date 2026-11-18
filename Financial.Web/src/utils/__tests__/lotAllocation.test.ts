import { describe, expect, it } from 'vitest'
import type { OpenLotDto } from '../../api/types'
import { hasOverAllocatedLot, isAllocationExact } from '../lotAllocation'

function lot(sourceTransactionId: string, remainingQuantity: number): OpenLotDto {
  return { sourceTransactionId, remainingQuantity, date: '2026-01-01', unitCost: 1 }
}

describe('isAllocationExact', () => {
  it('accepts lots that add up to the sale despite floating-point drift', () => {
    expect(isAllocationExact(0.1 + 0.2, 0.3)).toBe(true)
  })

  it.each([
    [0, 0],
    [1, 2],
  ])('refuses %s allocated for a sale of %s', (allocated, saleQuantity) => {
    expect(isAllocationExact(allocated, saleQuantity)).toBe(false)
  })
})

describe('hasOverAllocatedLot', () => {
  const lots = [lot('a', 5), lot('b', 5)]

  it('finds a lot given more than it has left, among others that are fine', () => {
    expect(hasOverAllocatedLot(lots, { a: '1', b: '6' })).toBe(true)
  })

  it('finds none when every lot is within what it has left', () => {
    expect(hasOverAllocatedLot(lots, { a: '5', b: '1' })).toBe(false)
  })
})
