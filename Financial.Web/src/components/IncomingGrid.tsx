import type { IncomeSourceTotalDto } from '../api/types'
import { formatN2 } from '../utils/formatters'
import { orMissing } from '../utils/orMissing'
import TotalsGrid from './TotalsGrid'

interface IncomingGridProps {
  incomeTotals: IncomeSourceTotalDto[]
  totalIncoming: number
}

export default function IncomingGrid({ incomeTotals, totalIncoming }: IncomingGridProps) {
  return (
    <TotalsGrid
      label="Income totals"
      totalsLabel="Totals for income"
      columns={[
        {
          key: 'source',
          header: 'Source',
          render: (i: IncomeSourceTotalDto) => i.name,
          sortAccessor: (i: IncomeSourceTotalDto) => i.name,
        },
        {
          key: 'gross',
          header: 'Gross',
          numeric: true,
          render: (i: IncomeSourceTotalDto) => orMissing(i.grossValue, formatN2),
          sortAccessor: (i: IncomeSourceTotalDto) => i.grossValue,
        },
        {
          key: 'net',
          header: 'Net',
          numeric: true,
          render: (i: IncomeSourceTotalDto) => formatN2(i.netValue),
          sortAccessor: (i: IncomeSourceTotalDto) => i.netValue,
        },
      ]}
      rows={incomeTotals}
      rowKey={(i) => i.incomeSourceId}
      emptyMessage="No income recorded for this month."
      footerItems={[{ label: 'Total incoming', value: formatN2(totalIncoming) }]}
    />
  )
}
