import { Fragment, useState, type ReactNode } from 'react'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useAnnualSummary } from '../hooks/useAnnualSummary'
import { formatN2 } from '../utils/formatters'
import { average } from '../utils/math'
import './AnnualSummaryPage.css'

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
const SPACER_COL_SPAN = MONTH_LABELS.length + 3

type AnnualSummaryTabId = 'categoryTotals' | 'investments' | 'historicSummaryAverage'

const TABS: { id: AnnualSummaryTabId; label: string }[] = [
  { id: 'categoryTotals', label: 'Category Totals' },
  { id: 'investments', label: 'Investments' },
  { id: 'historicSummaryAverage', label: 'Historic Summary Average' },
]

function AnnualSummaryRow({
  label,
  monthlyValues,
  annualTotal,
  emphasized = false,
}: {
  label: string
  monthlyValues: number[]
  annualTotal: number
  emphasized?: boolean
}) {
  const cell = (content: ReactNode) => (emphasized ? <strong>{content}</strong> : content)
  return (
    <tr className={emphasized ? 'annual-summary-page__emphasized-row' : undefined}>
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
        <strong>{formatN2(annualTotal)}</strong>
      </td>
    </tr>
  )
}


const optionalEmphasize = (content: ReactNode, emphasized?: boolean) => (emphasized ? <strong>{content}</strong> : content)

function InvestmentRow({
  label,
  monthlyValues,
  emphasized = false,
}: {
  label: string
  monthlyValues: (number | null)[]
  emphasized?: boolean
}) {
  return (
    <tr className={emphasized ? 'annual-summary-page__emphasized-row' : undefined}>
      <td>{optionalEmphasize(label, emphasized)}</td>
      {monthlyValues.map((v, i) => (
        <td key={i} className="data-table__col--numeric">
          {v === null ? null : optionalEmphasize(formatN2(v), emphasized)}
        </td>
      ))}
    </tr>
  )
}

export default function AnnualSummaryPage() {
  const {
    year,
    setYear,
    categoryTotals,
    investmentAnnualResult,
    incomeSummary,
    historicSummaryAverage,
    totalDespesasMonthly,
    totalDespesasAnnualTotal,
    resultadoMonthly,
    resultadoAnnualTotal,
    isLoading,
    error,
    retry,
  } = useAnnualSummary()

  const [activeTab, setActiveTab] = useState<AnnualSummaryTabId>('categoryTotals')
  const HISTORIC_SUMMARY_AVERAGE_SPACER_AFTER = new Set(['Tax difference', 'Dividendo/Juros', 'Reserva'])
  const HISTORIC_SUMMARY_AVERAGE_EMPHASIZED = new Set(['Resultado (R-D-Inv)', 'Total despesas'])

  return (
    <div className="annual-summary-page">
      <div className="annual-summary-page__year-picker">
        <label htmlFor="annual-summary-year">Year</label>
        <input
          id="annual-summary-year"
          type="number"
          value={year}
          onChange={(e) => setYear(Number(e.target.value))}
        />
      </div>

      <div className="annual-summary-page__tabs">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            className={`annual-summary-page__tab${activeTab === tab.id ? ' annual-summary-page__tab--active' : ''}`}
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
        <div className="annual-summary-page__content">
          {activeTab === 'categoryTotals' && (
            <section className="annual-summary-page__section">
              <table className="annual-summary-page__table data-table">
                <thead>
                  <tr>
                    <th>Category</th>
                    {MONTH_LABELS.map((m) => (
                      <th key={m} className="data-table__col--numeric">
                        {m}
                      </th>
                    ))}
                    <th className="data-table__col--numeric">Average</th>
                    <th className="data-table__col--numeric">Annual Total</th>
                  </tr>
                </thead>
                <tbody>
                  {incomeSummary && (
                    <>
                      <AnnualSummaryRow
                        label="Salary"
                        monthlyValues={incomeSummary.salaryMonthly}
                        annualTotal={incomeSummary.salaryAnnualTotal}
                      />
                      <AnnualSummaryRow
                        label="Salary after taxes"
                        monthlyValues={incomeSummary.salaryAfterTaxesMonthly}
                        annualTotal={incomeSummary.salaryAfterTaxesAnnualTotal}
                      />
                      <AnnualSummaryRow
                        label="Tax difference"
                        monthlyValues={incomeSummary.taxDifferenceMonthly}
                        annualTotal={incomeSummary.taxDifferenceAnnualTotal}
                      />
                      <tr>
                        <td colSpan={SPACER_COL_SPAN} />
                      </tr>
                      <AnnualSummaryRow
                        label="Dividendo/Juros"
                        monthlyValues={incomeSummary.dividendoJurosMonthly}
                        annualTotal={incomeSummary.dividendoJurosAnnualTotal}
                      />
                    </>
                  )}

                  <tr>
                    <td colSpan={SPACER_COL_SPAN} />
                  </tr>

                  {categoryTotals.map((c) => (
                    <AnnualSummaryRow key={c.category} label={c.category} monthlyValues={c.monthlyTotals} annualTotal={c.annualTotal} />
                  ))}

                  <tr>
                    <td colSpan={SPACER_COL_SPAN} />
                  </tr>

                  {incomeSummary && (
                    <AnnualSummaryRow
                      label="Resultado (R-D-Inv)"
                      monthlyValues={resultadoMonthly}
                      annualTotal={resultadoAnnualTotal}
                      emphasized
                    />
                  )}
                  <AnnualSummaryRow
                    label="Total despesas"
                    monthlyValues={totalDespesasMonthly}
                    annualTotal={totalDespesasAnnualTotal}
                    emphasized
                  />
                </tbody>
              </table>
            </section>
          )}

          {activeTab === 'investments' && investmentAnnualResult && (
            <section className="annual-summary-page__section">
              <table className="annual-summary-page__table data-table">
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
                  {investmentAnnualResult.accounts.map((a) => (
                    <InvestmentRow
                      key={a.account}
                      label={`${a.account}${a.isLiability ? ' (-)' : ''}`}
                      monthlyValues={a.monthlyValues}
                    />
                  ))}
                  <InvestmentRow label="Total" monthlyValues={investmentAnnualResult.netPosition.monthlyValues} emphasized />
                  <InvestmentRow
                    label="Month Result"
                    monthlyValues={investmentAnnualResult.netPosition.monthlyDiffs}
                    emphasized
                  />
                </tbody>
              </table>

              <div className="annual-summary-page__investment-totals">
                <div className="annual-summary-page__investment-total">
                  <span>Year Progress</span>
                  <strong>{formatN2(investmentAnnualResult.netPosition.fullYearNetChange)}</strong>
                </div>
                <div className="annual-summary-page__investment-total">
                  <span>Average Month Result</span>
                  <strong>{formatN2(investmentAnnualResult.netPosition.averageMonthResult)}</strong>
                </div>
                <div className="annual-summary-page__investment-total">
                  <span>Sum of Month Results</span>
                  <strong>{formatN2(investmentAnnualResult.netPosition.sumOfMonthResults)}</strong>
                </div>
              </div>
            </section>
          )}

          {activeTab === 'historicSummaryAverage' && (
            <section className="annual-summary-page__section">
              <table className="annual-summary-page__table data-table">
                <thead>
                  <tr>
                    <th>Category</th>
                    {historicSummaryAverage && (
                      historicSummaryAverage.map((y) => (
                        <th key={y.year} className="data-table__col--numeric">
                          {y.year}
                        </th>
                      ))
                    )}
                  </tr>
                </thead>
                <tbody>
                  {historicSummaryAverage && historicSummaryAverage[0].annualAverages.map((a) => {
                    const isEmphasized = HISTORIC_SUMMARY_AVERAGE_EMPHASIZED.has(a.category)
                    return (
                      <Fragment key={a.category}>
                        <tr className={isEmphasized ? 'annual-summary-page__emphasized-row' : undefined}>
                          <td>{optionalEmphasize(a.category, isEmphasized)}</td>
                          {historicSummaryAverage.map((y) => (
                            <td key={y.year} className="data-table__col--numeric">
                              {optionalEmphasize(formatN2(y.annualAverages.find((d) => d.category === a.category)?.value ?? 0), 
                                isEmphasized)}
                            </td>
                          ))}
                        </tr>
                        {HISTORIC_SUMMARY_AVERAGE_SPACER_AFTER.has(a.category) && (
                          <tr>
                            <td colSpan={historicSummaryAverage.length + 1} />
                          </tr>
                        )}
                      </Fragment>
                  )})}
                </tbody>
              </table>  
            </section>
          )}
        </div>
      )}
    </div>
  )
}
