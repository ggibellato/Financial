import type { IncomeTotal } from '../hooks/useMonthly'
import type { TitheSummaryDto } from '../api/types'
import { formatN2 } from '../utils/formatters'
import TotalsGrid from './TotalsGrid'

interface IncomingGridProps {
  incomeTotals: IncomeTotal[]
  totalIncoming: number
  titheSummary: TitheSummaryDto | null
}

export default function IncomingGrid({ incomeTotals, totalIncoming, titheSummary }: IncomingGridProps) {
  return (
    <TotalsGrid
      columns={[
        {
          key: 'source',
          header: 'Source',
          render: (i: IncomeTotal) => i.source,
          sortAccessor: (i: IncomeTotal) => i.source,
        },
        {
          key: 'gross',
          header: 'Gross',
          numeric: true,
          render: (i: IncomeTotal) => (i.grossValue != null ? formatN2(i.grossValue) : '—'),
          sortAccessor: (i: IncomeTotal) => i.grossValue,
        },
        {
          key: 'net',
          header: 'Net',
          numeric: true,
          render: (i: IncomeTotal) => formatN2(i.netValue),
          sortAccessor: (i: IncomeTotal) => i.netValue,
        },
      ]}
      rows={incomeTotals}
      rowKey={(i) => i.source}
      footerItems={[
        { label: 'Total Incoming', value: formatN2(totalIncoming) },
        ...(titheSummary
          ? [
              { label: 'Calculated Tithe', value: formatN2(titheSummary.calculatedTithe) },
              { label: 'Tithe Balance', value: formatN2(titheSummary.titheBalance) },
            ]
          : []),
      ]}
    />
  )
}
