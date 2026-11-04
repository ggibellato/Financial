import { describe, expect, it } from 'vitest'
import type { PotBalanceDto } from '../../api/types'
import { activePotsTotal, canSplitAcrossActivePots, previewPotSplit } from '../potSplitPreview'

const pot = (id: string, splitPercentage: number, isActive = true): PotBalanceDto => ({ id, name: id, splitPercentage, isActive, balance: 0 })

const LIVE_SHAPE = [pot('a', 33.33), pot('b', 33.33), pot('c', 16.67), pot('d', 16.67), pot('off', 50, false)]

describe('potSplitPreview', () => {
  it('totals only the active pots, without floating-point noise', () => {
    expect(activePotsTotal(LIVE_SHAPE)).toBe(100)
    expect(canSplitAcrossActivePots(LIVE_SHAPE)).toBe(true)
    expect(canSplitAcrossActivePots([pot('a', 99.99)])).toBe(false)
    expect(canSplitAcrossActivePots([pot('off', 100, false)])).toBe(false)
  })

  it('splits across the active pots and always adds up to the amount', () => {
    const lines = previewPotSplit(LIVE_SHAPE, 90)

    expect(lines.map((line) => [line.pot.id, line.amount])).toEqual([
      ['a', 30],
      ['b', 30],
      ['c', 15],
      ['d', 15],
    ])
  })

  it('gives the rounding residual to the first largest percentage', () => {
    const lines = previewPotSplit([pot('small', 16.67), pot('big', 33.33), pot('big2', 33.33), pot('small2', 16.67)], 0.1)

    expect(lines.map((line) => line.amount)).toEqual([0.02, 0.03, 0.03, 0.02])
    const uneven = previewPotSplit([pot('a', 33.33), pot('b', 33.33), pot('c', 33.34)], 10)
    expect(uneven.map((line) => line.amount)).toEqual([3.33, 3.33, 3.34])
    const residual = previewPotSplit([pot('a', 50), pot('b', 50)], 0.01)
    expect(residual.map((line) => line.amount)).toEqual([0, 0.01])
  })
})
