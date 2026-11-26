import type { HoldingsTotalsDto } from '../api/types'

export function holdingsTotals(overrides: Partial<HoldingsTotalsDto> = {}): HoldingsTotalsDto {
  return {
    holdingCount: 0,
    unvaluedHoldingCount: 0,
    invested: 0,
    marketValue: null,
    investedShareOfBase: null,
    marketShareOfBase: null,
    profitAmount: null,
    profitPercent: null,
    profitWithCreditsPercent: null,
    totalCredits: 0,
    creditsPercentOfInvested: null,
    lastCreditAmount: 0,
    lastCreditPercentOfMarketValue: null,
    lastMonthStartValue: null,
    thisMonthStartValue: null,
    monthChangePercent: null,
    monthStartExcludedCount: 0,
    ...overrides,
  }
}
