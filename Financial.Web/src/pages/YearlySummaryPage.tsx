import { useState } from 'react'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useYearlySummary } from '../hooks/useYearlySummary'
import { formatN2 } from '../utils/formatters'
import './YearlySummaryPage.css'

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

type YearlySummaryTabId = 'categoryTotals' | 'investments'

const TABS: { id: YearlySummaryTabId; label: string }[] = [
  { id: 'categoryTotals', label: 'Category Totals' },
  { id: 'investments', label: 'Investments' },
]

export default function YearlySummaryPage() {
  const { year, setYear, categoryTotals, investmentDiffs, incomeSummary, isLoading, error, retry } = useYearlySummary()

  const [activeTab, setActiveTab] = useState<YearlySummaryTabId>('categoryTotals')

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

      <div className="yearly-summary-page__tabs">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            className={`yearly-summary-page__tab${activeTab === tab.id ? ' yearly-summary-page__tab--active' : ''}`}
            onClick={() => setActiveTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <div className="yearly-summary-page__content">
          {activeTab === 'categoryTotals' && (
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

              {incomeSummary && (
                <section className="yearly-summary-page__section">
                  <h2>Income Summary</h2>
                  <table className="yearly-summary-page__table data-table">
                    <thead>
                      <tr>
                        <th />
                        {MONTH_LABELS.map((m) => (
                          <th key={m} className="data-table__col--numeric">
                            {m}
                          </th>
                        ))}
                        <th className="data-table__col--numeric">Yearly Total</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr>
                        <td colSpan={MONTH_LABELS.length + 2}>
                          <strong>Income</strong>
                        </td>
                      </tr>
                      <tr>
                        <td>Salary</td>
                        {incomeSummary.salaryMonthly.map((v, i) => (
                          <td key={i} className="data-table__col--numeric">
                            {formatN2(v)}
                          </td>
                        ))}
                        <td className="data-table__col--numeric">
                          <strong>{formatN2(incomeSummary.salaryYearlyTotal)}</strong>
                        </td>
                      </tr>
                      <tr>
                        <td>Salary after taxes</td>
                        {incomeSummary.salaryAfterTaxesMonthly.map((v, i) => (
                          <td key={i} className="data-table__col--numeric">
                            {formatN2(v)}
                          </td>
                        ))}
                        <td className="data-table__col--numeric">
                          <strong>{formatN2(incomeSummary.salaryAfterTaxesYearlyTotal)}</strong>
                        </td>
                      </tr>
                      <tr>
                        <td>Tax difference</td>
                        {incomeSummary.taxDifferenceMonthly.map((v, i) => (
                          <td key={i} className="data-table__col--numeric">
                            {formatN2(v)}
                          </td>
                        ))}
                        <td className="data-table__col--numeric">
                          <strong>{formatN2(incomeSummary.taxDifferenceYearlyTotal)}</strong>
                        </td>
                      </tr>
                      <tr>
                        <td colSpan={MONTH_LABELS.length + 2} />
                      </tr>
                      <tr>
                        <td>Dividendo/Juros</td>
                        {incomeSummary.dividendoJurosMonthly.map((v, i) => (
                          <td key={i} className="data-table__col--numeric">
                            {formatN2(v)}
                          </td>
                        ))}
                        <td className="data-table__col--numeric">
                          <strong>{formatN2(incomeSummary.dividendoJurosYearlyTotal)}</strong>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </section>
              )}
            </>
          )}

          {activeTab === 'investments' && investmentDiffs && (
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
        </div>
      )}
    </div>
  )
}
