import type { IncomeTotal } from '../hooks/useMonthly'
import type { TitheSummaryDto } from '../api/types'
import { formatN2 } from '../utils/formatters'

interface IncomingGridProps {
  incomeTotals: IncomeTotal[]
  totalIncoming: number
  titheSummary: TitheSummaryDto | null
}

export default function IncomingGrid({ incomeTotals, totalIncoming, titheSummary }: IncomingGridProps) {
  return (
    <section className="monthly-page__section monthly-page__section--grid">
      <div className="monthly-page__table-scroll">
        <table className="monthly-page__table data-table">
          <thead>
            <tr>
              <th>Source</th>
              <th className="data-table__col--numeric">Gross</th>
              <th className="data-table__col--numeric">Net</th>
            </tr>
          </thead>
          <tbody>
            {incomeTotals.map((i) => (
              <tr key={i.source}>
                <td>{i.source}</td>
                <td className="data-table__col--numeric">
                  {i.grossValue != null ? formatN2(i.grossValue) : '—'}
                </td>
                <td className="data-table__col--numeric">{formatN2(i.netValue)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="monthly-page__section-total">
        Total Incoming: <strong>{formatN2(totalIncoming)}</strong>
        {titheSummary && (
          <>
            {' '}
            · Calculated Tithe: <strong>{formatN2(titheSummary.calculatedTithe)}</strong> · Tithe Balance:{' '}
            <strong>{formatN2(titheSummary.titheBalance)}</strong>
          </>
        )}
      </p>
    </section>
  )
}
