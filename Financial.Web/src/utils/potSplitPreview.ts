import type { PotBalanceDto } from '../api/types'

const REQUIRED_ACTIVE_TOTAL = 100
const TOTAL_ROUNDING = 10_000

export interface PotSplitLine {
  pot: PotBalanceDto
  amount: number
}

export function activePotsTotal(pots: readonly PotBalanceDto[]): number {
  const total = pots.filter((pot) => pot.isActive).reduce((sum, pot) => sum + pot.splitPercentage, 0)
  return Math.round(total * TOTAL_ROUNDING) / TOTAL_ROUNDING
}

export function canSplitAcrossActivePots(pots: readonly PotBalanceDto[]): boolean {
  return activePotsTotal(pots) === REQUIRED_ACTIVE_TOTAL
}

function indexOfLargestSplit(pots: readonly PotBalanceDto[]): number {
  return pots.reduce((largest, pot, index) => (pot.splitPercentage > pots[largest].splitPercentage ? index : largest), 0)
}

export function previewPotSplit(pots: readonly PotBalanceDto[], amount: number): PotSplitLine[] {
  const activePots = pots.filter((pot) => pot.isActive)
  const pence = activePots.map((pot) => Math.round(amount * pot.splitPercentage))
  const residual = Math.round(amount * REQUIRED_ACTIVE_TOTAL) - pence.reduce((sum, value) => sum + value, 0)
  const residualIndex = indexOfLargestSplit(activePots)
  return activePots.map((pot, index) => ({
    pot,
    amount: (pence[index] + (index === residualIndex ? residual : 0)) / 100,
  }))
}
