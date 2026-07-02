export interface CashFlow {
  date: Date
  amount: number
}

export function xirr(cashFlows: CashFlow[]): number | null {
  if (cashFlows.length < 2) return null

  const startTime = cashFlows[0].date.getTime()
  const days = cashFlows.map(cf => (cf.date.getTime() - startTime) / 86_400_000)
  const amounts = cashFlows.map(cf => cf.amount)

  let rate = 0.1

  for (let iter = 0; iter < 100; iter++) {
    let npv = 0
    let dnpv = 0

    for (let j = 0; j < amounts.length; j++) {
      const t = days[j] / 365
      const base = Math.pow(1 + rate, t)
      npv += amounts[j] / base
      dnpv -= (t * amounts[j]) / ((1 + rate) * base)
    }

    if (Math.abs(npv) < 1e-7) return rate
    if (Math.abs(dnpv) < 1e-10) return null

    rate -= npv / dnpv
  }

  return null
}
