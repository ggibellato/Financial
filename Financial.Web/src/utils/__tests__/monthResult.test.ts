import { describe, expect, it } from 'vitest'
import { monthResult } from '../monthResult'

const AC_03 = '[AC P61-F01-accounts-overview-tabs-and-monthly-fold-03]'

describe('monthResult', () => {
  it(`${AC_03} is a Surplus when income exceeds expenses`, () => {
    expect(monthResult(4185.2, 2840.1)).toEqual({ value: 1345.1, label: 'Surplus' })
  })

  it(`${AC_03} is a Deficit when expenses exceed income`, () => {
    expect(monthResult(100, 250.75)).toEqual({ value: -150.75, label: 'Deficit' })
  })

  it(`${AC_03} is Balanced when income equals expenses`, () => {
    expect(monthResult(0, 0)).toEqual({ value: 0, label: 'Balanced' })
    expect(monthResult(12.3, 12.3)).toEqual({ value: 0, label: 'Balanced' })
  })

  it('subtracts in pence so float error never leaves a stray cent or flips the label', () => {
    expect(monthResult(0.3, 0.1)).toEqual({ value: 0.2, label: 'Surplus' })
    expect(monthResult(0.1 + 0.2, 0.3)).toEqual({ value: 0, label: 'Balanced' })
  })
})
