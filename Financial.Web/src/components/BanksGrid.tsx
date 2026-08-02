import type { BankTotal } from '../hooks/useMonthly'
import { formatN2 } from '../utils/formatters'

interface BanksGridProps {
  bankTotals: BankTotal[]
  bankTotalsSum: number
  roundUpTotalsSum: number
}

/** Read-only Summary balances table: Bank, Balance, Round-Up plus a totals row. No interaction. */
export default function BanksGrid({ bankTotals, bankTotalsSum, roundUpTotalsSum }: BanksGridProps) {
  return (
    <section className="monthly-page__section monthly-page__section--grid">
      <div className="monthly-page__table-scroll">
        <table className="monthly-page__table data-table">
          <thead>
            <tr>
              <th>Bank</th>
              <th className="data-table__col--numeric">Bank Balance</th>
              <th className="data-table__col--numeric">Round-Up</th>
            </tr>
          </thead>
          <tbody>
            {bankTotals.map((b) => (
              <tr key={b.bank}>
                <td>{b.bank}</td>
                <td className="data-table__col--numeric">{formatN2(b.balance)}</td>
                <td className="data-table__col--numeric">{formatN2(b.roundUpTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="monthly-page__section-total">
        Bank Balance: <strong>{formatN2(bankTotalsSum)}</strong> · Round-Up: <strong>{formatN2(roundUpTotalsSum)}</strong>
      </p>
    </section>
  )
}
