import type { BankTotal } from '../hooks/useMonthly'
import { formatN2 } from '../utils/formatters'
import TotalsGrid from './TotalsGrid'

interface BanksGridProps {
  bankTotals: BankTotal[]
  bankTotalsSum: number
  roundUpTotalsSum: number
}

export default function BanksGrid({ bankTotals, bankTotalsSum, roundUpTotalsSum }: BanksGridProps) {
  return (
    <TotalsGrid
      columns={[
        { key: 'bank', header: 'Bank', render: (b: BankTotal) => b.bank },
        { key: 'balance', header: 'Bank Balance', numeric: true, render: (b: BankTotal) => formatN2(b.balance) },
        { key: 'roundUp', header: 'Round-Up', numeric: true, render: (b: BankTotal) => formatN2(b.roundUpTotal) },
      ]}
      rows={bankTotals}
      rowKey={(b) => b.bank}
      footerItems={[
        { label: 'Bank Balance', value: formatN2(bankTotalsSum) },
        { label: 'Round-Up', value: formatN2(roundUpTotalsSum) },
      ]}
    />
  )
}
