import { useState, type ReactNode } from 'react'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useYearlySummary } from '../hooks/useYearlySummary'
import { formatN2 } from '../utils/formatters'
import { average } from '../utils/math'
import './YearlySummaryPage.css'

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
const SPACER_COL_SPAN = MONTH_LABELS.length + 3

type YearlySummaryTabId = 'categoryTotals' | 'investments'

const TABS: { id: YearlySummaryTabId; label: string }[] = [
  { id: 'categoryTotals', label: 'Category Totals' },
  { id: 'investments', label: 'Investments' },
]

function YearlySummaryRow({
  label,
  monthlyValues,
  yearlyTotal,
  emphasized = false,
}: {
  label: string
  monthlyValues: number[]
  yearlyTotal: number
  emphasized?: boolean
}) {
  const cell = (content: ReactNode) => (emphasized ? <strong>{content}</strong> : content)
  return (
    <tr className={emphasized ? 'yearly-summary-page__emphasized-row' : undefined}>
      <td>{cell(label)}</td>
      {monthlyValues.map((v, i) => (
        <td key={i} className="data-table__col--numeric">
          {cell(formatN2(v))}
        </td>
      ))}
      <td className="data-table__col--numeric">
        <strong>{formatN2(average(monthlyValues))}</strong>
      </td>
      <td className="data-table__col--numeric">
        <strong>{formatN2(yearlyTotal)}</strong>
      </td>
    </tr>
  )
}

export default function YearlySummaryPage() {
  const {
    year,
    setYear,
    categoryTotals,
    investmentDiffs,
    incomeSummary,
    totalDespesasMonthly,
    totalDespesasYearlyTotal,
    resultadoMonthly,
    resultadoYearlyTotal,
    isLoading,
    error,
    retry,
  } = useYearlySummary()

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
            <section className="yearly-summary-page__section">
              <h2>Category Totals</h2>
              <table className="yearly-summary-page__table data-table">
                <thead>
                  <tr>
                    <th />
                    {MONTH_LABELS.map((m) => (
                      <th key={m} className="data-table__col--numeric">
                        {m}
                      </th>
                    ))}
                    <th className="data-table__col--numeric">Average</th>
                    <th className="data-table__col--numeric">Yearly Total</th>
                  </tr>
                </thead>
                <tbody>
                  {incomeSummary && (
                    <>
                      <YearlySummaryRow
                        label="Salary"
                        monthlyValues={incomeSummary.salaryMonthly}
                        yearlyTotal={incomeSummary.salaryYearlyTotal}
                      />
                      <YearlySummaryRow
                        label="Salary after taxes"
                        monthlyValues={incomeSummary.salaryAfterTaxesMonthly}
                        yearlyTotal={incomeSummary.salaryAfterTaxesYearlyTotal}
                      />
                      <YearlySummaryRow
                        label="Tax difference"
                        monthlyValues={incomeSummary.taxDifferenceMonthly}
                        yearlyTotal={incomeSummary.taxDifferenceYearlyTotal}
                      />
                      <tr>
                        <td colSpan={SPACER_COL_SPAN} />
                      </tr>
                      <YearlySummaryRow
                        label="Dividendo/Juros"
                        monthlyValues={incomeSummary.dividendoJurosMonthly}
                        yearlyTotal={incomeSummary.dividendoJurosYearlyTotal}
                      />
                    </>
                  )}

                  <tr>
                    <td colSpan={SPACER_COL_SPAN} />
                  </tr>

                  {categoryTotals.map((c) => (
                    <YearlySummaryRow key={c.category} label={c.category} monthlyValues={c.monthlyTotals} yearlyTotal={c.yearlyTotal} />
                  ))}

                  <tr>
                    <td colSpan={SPACER_COL_SPAN} />
                  </tr>

                  {incomeSummary && (
                    <YearlySummaryRow
                      label="Resultado (R-D-Inv)"
                      monthlyValues={resultadoMonthly}
                      yearlyTotal={resultadoYearlyTotal}
                      emphasized
                    />
                  )}
                  <YearlySummaryRow
                    label="Total despesas"
                    monthlyValues={totalDespesasMonthly}
                    yearlyTotal={totalDespesasYearlyTotal}
                    emphasized
                  />
                </tbody>
              </table>
            </section>
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
                  <tr className="yearly-summary-page__emphasized-row">
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
