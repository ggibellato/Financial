import type { BankDto, CardStatementDto } from '../api/types'
import { formatN2 } from '../utils/formatters'

interface CardsGridProps {
  cardStatements: CardStatementDto[]
  banks: BankDto[]
  adjustmentTotal: number
  markPaidSources: Record<string, string>
  setMarkPaidSource: (id: string, bank: string) => void
  markStatementPaid: (id: string, paymentSource: string) => void
  unmarkStatementPaid: (id: string) => void
}

export default function CardsGrid({
  cardStatements,
  banks,
  adjustmentTotal,
  markPaidSources,
  setMarkPaidSource,
  markStatementPaid,
  unmarkStatementPaid,
}: CardsGridProps) {
  return (
    <section className="monthly-page__section monthly-page__section--grid">
      <div className="monthly-page__table-scroll">
        <table className="monthly-page__table data-table">
          <thead>
            <tr>
              <th>Card</th>
              <th className="data-table__col--numeric">Outstanding</th>
              <th>Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {cardStatements.map((s) => (
              <tr key={s.id}>
                <td>{s.creditCardName}</td>
                <td className="data-table__col--numeric">{formatN2(s.outstandingTotal)}</td>
                <td>{s.isPaid ? 'Paid' : 'Unpaid'}</td>
                <td>
                  {s.isPaid ? (
                    <button type="button" onClick={() => unmarkStatementPaid(s.id)}>
                      Unmark Paid
                    </button>
                  ) : (
                    <>
                      <select
                        aria-label={`Paying bank for ${s.creditCardName}`}
                        value={markPaidSources[s.id] ?? ''}
                        onChange={(e) => setMarkPaidSource(s.id, e.target.value)}
                      >
                        <option value="">Bank…</option>
                        {banks.map((b) => (
                          <option key={b.id} value={b.id}>
                            {b.name}
                          </option>
                        ))}
                      </select>{' '}
                      <button
                        type="button"
                        disabled={!markPaidSources[s.id]}
                        onClick={() => markStatementPaid(s.id, markPaidSources[s.id])}
                      >
                        Mark Paid
                      </button>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="monthly-page__section-total">
        Combined adjustment figure: <strong>{formatN2(adjustmentTotal)}</strong>
      </p>
    </section>
  )
}
