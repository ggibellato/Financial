export function amountsCaption(subject: string, currencies: readonly string[]): string {
  return currencies.length === 1
    ? `${subject}, amounts in ${currencies[0]}`
    : `${subject}, amounts in each holding's own currency (${currencies.join(', ')}); subtotals and totals add them without conversion`
}

interface ValuationCounts {
  holdingCount: number
  unvaluedHoldingCount: number
}

export function incompleteTotalNotice({ holdingCount, unvaluedHoldingCount }: ValuationCounts): string | null {
  return unvaluedHoldingCount === 0
    ? null
    : `${unvaluedHoldingCount} of ${holdingCount} holdings could not be valued; the total is incomplete.`
}
