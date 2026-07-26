import { useState, type ReactNode } from 'react'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useYearlySummary } from '../hooks/useYearlySummary'
import { formatN2 } from '../utils/formatters'
import { average } from '../utils/math'
import './YearlySummaryPage.css'

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
const SPACER_COL_SPAN = MONTH_LABELS.length + 3

type YearlySummaryTabId = 'categoryTotals' | 'investments' | 'historicAverage'

const TABS: { id: YearlySummaryTabId; label: string }[] = [
  { id: 'categoryTotals', label: 'Category Totals' },
  { id: 'investments', label: 'Investments' },
  { id: 'historicAverage', label: 'Historic Average' },
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

function InvestmentRow({
  label,
  monthlyValues,
  emphasized = false,
}: {
  label: string
  monthlyValues: (number | null)[]
  emphasized?: boolean
}) {
  const cell = (content: ReactNode) => (emphasized ? <strong>{content}</strong> : content)
  return (
    <tr className={emphasized ? 'yearly-summary-page__emphasized-row' : undefined}>
      <td>{cell(label)}</td>
      {monthlyValues.map((v, i) => (
        <td key={i} className="data-table__col--numeric">
          {v === null ? null : cell(formatN2(v))}
        </td>
      ))}
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
              <h2>Investments</h2>
              <table className="yearly-summary-page__table data-table">
                <thead>
                  <tr>
                    <th>Account</th>
                    {MONTH_LABELS.map((m) => (
                      <th key={m} className="data-table__col--numeric">
                        {m}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {investmentDiffs.accounts.map((a) => (
                    <InvestmentRow
                      key={a.account}
                      label={`${a.account}${a.isLiability ? ' (-)' : ''}`}
                      monthlyValues={a.monthlyValues}
                    />
                  ))}
                  <InvestmentRow label="Total" monthlyValues={investmentDiffs.netPosition.monthlyValues} emphasized />
                  <InvestmentRow
                    label="Month Result"
                    monthlyValues={investmentDiffs.netPosition.monthlyDiffs}
                    emphasized
                  />
                </tbody>
              </table>

              <div className="yearly-summary-page__investment-totals">
                <div className="yearly-summary-page__investment-total">
                  <span>Year Progress</span>
                  <strong>{formatN2(investmentDiffs.netPosition.fullYearNetChange)}</strong>
                </div>
                <div className="yearly-summary-page__investment-total">
                  <span>Average Month Result</span>
                  <strong>{formatN2(investmentDiffs.netPosition.averageMonthResult)}</strong>
                </div>
                <div className="yearly-summary-page__investment-total">
                  <span>Sum of Month Results</span>
                  <strong>{formatN2(investmentDiffs.netPosition.sumOfMonthResults)}</strong>
                </div>
              </div>
            </section>
          )}

          {activeTab === 'historicAverage' && (
            <section className="yearly-summary-page__section">
              <h2>Historic Average</h2>
              <p>This is the historic average section.</p>
            </section>
          )}
        </div>
      )}
    </div>
  )
}
