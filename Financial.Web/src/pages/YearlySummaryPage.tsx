import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useYearlySummary } from '../hooks/useYearlySummary'
import { formatN2 } from '../utils/formatters'
import './YearlySummaryPage.css'

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

export default function YearlySummaryPage() {
  const { year, setYear, categoryTotals, investmentDiffs, isLoading, error, retry } = useYearlySummary()

  return (
    <div className="yearly-summary-page">
      <div className="yearly-summary-page__year-picker">
        <label htmlFor="yearly-summary-year">Year</label>
        <input
          id="yearly-summary-year"
          type="number"
          value={year}
          onChange={(e) => setYear(Number(e.target.value))}
        />
      </div>

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <>
          <section className="yearly-summary-page__section">
            <h2>Category Totals</h2>
            <table className="yearly-summary-page__table data-table">
              <thead>
                <tr>
                  <th>Category</th>
                  {MONTH_LABELS.map((m) => (
                    <th key={m} className="data-table__col--numeric">
                      {m}
                    </th>
                  ))}
                  <th className="data-table__col--numeric">Yearly Total</th>
                </tr>
              </thead>
              <tbody>
                {categoryTotals.map((c) => (
                  <tr key={c.category}>
                    <td>{c.category}</td>
                    {c.monthlyTotals.map((total, i) => (
                      <td key={i} className="data-table__col--numeric">
                        {formatN2(total)}
                      </td>
                    ))}
                    <td className="data-table__col--numeric">
                      <strong>{formatN2(c.yearlyTotal)}</strong>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>

          {investmentDiffs && (
            <section className="yearly-summary-page__section">
              <h2>Investment Diffs</h2>
              <table className="yearly-summary-page__table data-table">
                <thead>
                  <tr>
                    <th>Account</th>
                    <th className="data-table__col--numeric">Jan</th>
                    {MONTH_LABELS.slice(1).map((m) => (
                      <th key={m} className="data-table__col--numeric">
                        {m} Δ
                      </th>
                    ))}
                    <th className="data-table__col--numeric">Full Year Net Change</th>
                  </tr>
                </thead>
                <tbody>
                  {investmentDiffs.accounts.map((a) => (
                    <tr key={a.account}>
                      <td>
                        {a.account}
                        {a.isLiability ? ' (liability)' : ''}
                      </td>
                      <td className="data-table__col--numeric">{formatN2(a.monthlyValues[0])}</td>
                      {a.monthlyDiffs.map((diff, i) => (
                        <td key={i} className="data-table__col--numeric">
                          {formatN2(diff)}
                        </td>
                      ))}
                      <td className="data-table__col--numeric" />
                    </tr>
                  ))}
                  <tr className="yearly-summary-page__net-position-row">
                    <td>
                      <strong>Net Position</strong>
                    </td>
                    <td className="data-table__col--numeric">
                      <strong>{formatN2(investmentDiffs.netPosition.monthlyValues[0])}</strong>
                    </td>
                    {investmentDiffs.netPosition.monthlyDiffs.map((diff, i) => (
                      <td key={i} className="data-table__col--numeric">
                        <strong>{formatN2(diff)}</strong>
                      </td>
                    ))}
                    <td className="data-table__col--numeric">
                      <strong>{formatN2(investmentDiffs.netPosition.fullYearNetChange)}</strong>
                    </td>
                  </tr>
                </tbody>
              </table>
            </section>
          )}
        </>
      )}
    </div>
  )
}
