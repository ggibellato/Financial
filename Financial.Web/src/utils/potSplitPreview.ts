import type { PotBalanceDto } from '../api/types'
import { toPence } from './formatters'

export const REQUIRED_ACTIVE_TOTAL = 100
const TOTAL_ROUNDING = 10_000

export interface PotSplitLine {
  pot: PotBalanceDto
  amount: number
}

export function roundSplitTotal(total: number): number {
  return Math.round(total * TOTAL_ROUNDING) / TOTAL_ROUNDING
}

export function activePotsTotal(pots: readonly PotBalanceDto[]): number {
  return roundSplitTotal(pots.filter((pot) => pot.isActive).reduce((sum, pot) => sum + pot.splitPercentage, 0))
}

export function canSplitAcrossActivePots(pots: readonly PotBalanceDto[]): boolean {
  return activePotsTotal(pots) === REQUIRED_ACTIVE_TOTAL
}

function indexOfLargestSplit(pots: readonly PotBalanceDto[]): number {
  return pots.reduce((largest, pot, index) => (pot.splitPercentage > pots[largest].splitPercentage ? index : largest), 0)
}

export function previewPotSplit(pots: readonly PotBalanceDto[], amount: number): PotSplitLine[] {
  const activePots = pots.filter((pot) => pot.isActive)
  const amountInPence = toPence(amount)
  const pence = activePots.map((pot) => Math.round((amountInPence * pot.splitPercentage) / REQUIRED_ACTIVE_TOTAL))
  const residual = amountInPence - pence.reduce((sum, value) => sum + value, 0)
  const residualIndex = indexOfLargestSplit(activePots)
  return activePots.map((pot, index) => ({
    pot,
    amount: (pence[index] + (index === residualIndex ? residual : 0)) / 100,
  }))
}
