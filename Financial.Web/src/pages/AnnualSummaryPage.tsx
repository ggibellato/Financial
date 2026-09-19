import { Fragment, useState, type ReactNode } from 'react'
import { Tab, TabList, Table, TableBody, TableCell, TableHeader, TableRow } from '@fluentui/react-components'
import type { SelectTabData, SelectTabEvent } from '@fluentui/react-components'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import SortableColumnHeader from '../components/grid/SortableColumnHeader'
import ColumnFilterMenu from '../components/grid/ColumnFilterMenu'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useColumnFilters } from '../hooks/useColumnFilters'
import { useAnnualSummary } from '../hooks/useAnnualSummary'
import type { CategoryAnnualAverageDto, CategoryAnnualTotalDto, InvestmentAnnualResultDto } from '../api/types'
import { formatN2 } from '../utils/formatters'
import './AnnualSummaryPage.css'

type InvestmentAccount = InvestmentAnnualResultDto['accounts'][number]
type HistoricCategoryRow = CategoryAnnualAverageDto['annualAverages'][number]

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
const SPACER_COL_SPAN = MONTH_LABELS.length + 3

function monthAccessors<T>(monthlyValuesOf: (row: T) => (number | null)[]): Record<string, SortAccessor<T>> {
  return Object.fromEntries(MONTH_LABELS.map((_, i) => [`month-${i}`, (row: T) => monthlyValuesOf(row)[i]]))
}

type AnnualSummaryTabId = 'categoryTotals' | 'investments' | 'historicSummaryAverage'

const TABS: { id: AnnualSummaryTabId; label: string }[] = [
  { id: 'categoryTotals', label: 'Category Totals' },
  { id: 'investments', label: 'Investments' },
  { id: 'historicSummaryAverage', label: 'Historic Summary Average' },
]

const optionalEmphasize = (content: ReactNode, emphasized?: boolean) => (emphasized ? <strong>{content}</strong> : content)

function AnnualSummaryRow({
  label,
  monthlyValues,
  average,
  annualTotal,
  emphasized = false,
}: {
  label: string
  monthlyValues: number[]
  average: number
  annualTotal: number
  emphasized?: boolean
}) {
  return (
    <TableRow className={emphasized ? 'annual-summary-page__emphasized-row' : undefined}>
      <TableCell>{optionalEmphasize(label, emphasized)}</TableCell>
      {monthlyValues.map((v, i) => (
        <TableCell key={i} className="data-table__col--numeric">
          {optionalEmphasize(formatN2(v), emphasized)}
        </TableCell>
      ))}
      <TableCell className="data-table__col--numeric">
        <strong>{formatN2(average)}</strong>
      </TableCell>
      <TableCell className="data-table__col--numeric">
        <strong>{formatN2(annualTotal)}</strong>
      </TableCell>
    </TableRow>
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
  return (
    <TableRow className={emphasized ? 'annual-summary-page__emphasized-row' : undefined}>
      <TableCell>{optionalEmphasize(label, emphasized)}</TableCell>
      {monthlyValues.map((v, i) => (
        <TableCell key={i} className="data-table__col--numeric">
          {v === null ? null : optionalEmphasize(formatN2(v), emphasized)}
        </TableCell>
      ))}
    </TableRow>
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
    totalDespesasAverage,
    resultadoMonthly,
    resultadoAnnualTotal,
    resultadoAverage,
    isLoading,
    error,
    retry,
  } = useAnnualSummary()

  const [activeTab, setActiveTab] = useState<AnnualSummaryTabId>('categoryTotals')
  const HISTORIC_SUMMARY_AVERAGE_SPACER_AFTER = new Set(['Tax difference', 'Dividendo/Juros', 'Reserva'])
  const HISTORIC_SUMMARY_AVERAGE_EMPHASIZED = new Set(['Resultado (R-D-Inv)', 'Total despesas'])

  // Only the homogeneous data rows (category totals / accounts / historic categories) are
  // sortable/filterable; the fixed income-summary lines, spacer rows, and emphasized total rows
  // on the Category Totals and Investments tabs render outside these arrays and stay pinned in
  // place regardless of sort or filter state.
  const {
    filteredRows: filteredCategoryTotals,
    availableValues: categoryTotalsAvailableValues,
    selectedValues: categoryTotalsSelectedValues,
    toggleValue: toggleCategoryTotalsValue,
    toggleAll: toggleCategoryTotalsAll,
    isColumnFiltered: isCategoryTotalsColumnFiltered,
  } = useColumnFilters(categoryTotals, { category: (c: CategoryAnnualTotalDto) => c.category })
  const {
    sortedRows: sortedCategoryTotals,
    sortState: categoryTotalsSortState,
    requestSort: requestCategoryTotalsSort,
  } = useSortableRows<CategoryAnnualTotalDto>(filteredCategoryTotals, {
    category: (c) => c.category,
    average: (c) => c.average,
    annualTotal: (c) => c.annualTotal,
    ...monthAccessors<CategoryAnnualTotalDto>((c) => c.monthlyTotals),
  })

  const investmentAccounts = investmentAnnualResult?.accounts ?? []
  const {
    sortedRows: sortedInvestmentAccounts,
    sortState: investmentsSortState,
    requestSort: requestInvestmentsSort,
  } = useSortableRows<InvestmentAccount>(investmentAccounts, {
    account: (a) => a.account,
    ...monthAccessors<InvestmentAccount>((a) => a.monthlyValues),
  })

  const historicCategoryRows = historicSummaryAverage[0]?.annualAverages ?? []
  const {
    filteredRows: filteredHistoricCategoryRows,
    availableValues: historicAvailableValues,
    selectedValues: historicSelectedValues,
    toggleValue: toggleHistoricValue,
    toggleAll: toggleHistoricAll,
    isColumnFiltered: isHistoricColumnFiltered,
  } = useColumnFilters(historicCategoryRows, { category: (row: HistoricCategoryRow) => row.category })
  const {
    sortedRows: sortedHistoricCategoryRows,
    sortState: historicSortState,
    requestSort: requestHistoricSort,
  } = useSortableRows<HistoricCategoryRow>(filteredHistoricCategoryRows, {
    category: (row) => row.category,
    ...Object.fromEntries(
      historicSummaryAverage.map((y) => [
        `year-${y.year}`,
        (row: HistoricCategoryRow) => y.annualAverages.find((d) => d.category === row.category)?.value ?? 0,
      ]),
    ),
  })

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

      <TabList
        selectedValue={activeTab}
        onTabSelect={(_event: SelectTabEvent, data: SelectTabData) => setActiveTab(data.value as AnnualSummaryTabId)}
      >
        {TABS.map((tab) => (
          <Tab key={tab.id} value={tab.id}>
            {tab.label}
          </Tab>
        ))}
      </TabList>

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <div className="annual-summary-page__content">
          {activeTab === 'categoryTotals' && (
            <section className="annual-summary-page__section">
              <Table className="annual-summary-page__table data-table">
                <TableHeader>
                  <TableRow>
                    <SortableColumnHeader
                      label="Category"
                      columnKey="category"
                      sortDirection={categoryTotalsSortState?.columnKey === 'category' ? categoryTotalsSortState.direction : undefined}
                      onSort={requestCategoryTotalsSort}
                    >
                      <ColumnFilterMenu
                        columnKey="category"
                        label="Category"
                        availableValues={categoryTotalsAvailableValues.category}
                        selectedValues={categoryTotalsSelectedValues.category}
                        onToggleValue={toggleCategoryTotalsValue}
                        onToggleAll={toggleCategoryTotalsAll}
                        isFiltered={isCategoryTotalsColumnFiltered('category')}
                      />
                    </SortableColumnHeader>
                    {MONTH_LABELS.map((m, i) => (
                      <SortableColumnHeader
                        key={m}
                        label={m}
                        columnKey={`month-${i}`}
                        numeric
                        sortDirection={
                          categoryTotalsSortState?.columnKey === `month-${i}` ? categoryTotalsSortState.direction : undefined
                        }
                        onSort={requestCategoryTotalsSort}
                      />
                    ))}
                    <SortableColumnHeader
                      label="Average"
                      columnKey="average"
                      numeric
                      sortDirection={categoryTotalsSortState?.columnKey === 'average' ? categoryTotalsSortState.direction : undefined}
                      onSort={requestCategoryTotalsSort}
                    />
                    <SortableColumnHeader
                      label="Annual Total"
                      columnKey="annualTotal"
                      numeric
                      sortDirection={
                        categoryTotalsSortState?.columnKey === 'annualTotal' ? categoryTotalsSortState.direction : undefined
                      }
                      onSort={requestCategoryTotalsSort}
                    />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {incomeSummary && (
                    <>
                      <AnnualSummaryRow
                        label="Salary"
                        monthlyValues={incomeSummary.salaryMonthly}
                        average={incomeSummary.salaryAverage}
                        annualTotal={incomeSummary.salaryAnnualTotal}
                      />
                      <AnnualSummaryRow
                        label="Salary after taxes"
                        monthlyValues={incomeSummary.salaryAfterTaxesMonthly}
                        average={incomeSummary.salaryAfterTaxesAverage}
                        annualTotal={incomeSummary.salaryAfterTaxesAnnualTotal}
                      />
                      <AnnualSummaryRow
                        label="Tax difference"
                        monthlyValues={incomeSummary.taxDifferenceMonthly}
                        average={incomeSummary.taxDifferenceAverage}
                        annualTotal={incomeSummary.taxDifferenceAnnualTotal}
                      />
                      <TableRow>
                        <TableCell colSpan={SPACER_COL_SPAN} />
                      </TableRow>
                      <AnnualSummaryRow
                        label="Dividendo/Juros"
                        monthlyValues={incomeSummary.dividendoJurosMonthly}
                        average={incomeSummary.dividendoJurosAverage}
                        annualTotal={incomeSummary.dividendoJurosAnnualTotal}
                      />
                    </>
                  )}

                  <TableRow>
                    <TableCell colSpan={SPACER_COL_SPAN} />
                  </TableRow>

                  {sortedCategoryTotals.length === 0 && isCategoryTotalsColumnFiltered('category') ? (
                    <TableRow>
                      <TableCell colSpan={SPACER_COL_SPAN}>No rows match the current filters</TableCell>
                    </TableRow>
                  ) : (
                    sortedCategoryTotals.map((c) => (
                      <AnnualSummaryRow
                        key={c.category}
                        label={c.category}
                        monthlyValues={c.monthlyTotals}
                        average={c.average}
                        annualTotal={c.annualTotal}
                      />
                    ))
                  )}

                  <TableRow>
                    <TableCell colSpan={SPACER_COL_SPAN} />
                  </TableRow>

                  {incomeSummary && (
                    <AnnualSummaryRow
                      label="Resultado (R-D-Inv)"
                      monthlyValues={resultadoMonthly}
                      average={resultadoAverage}
                      annualTotal={resultadoAnnualTotal}
                      emphasized
                    />
                  )}
                  <AnnualSummaryRow
                    label="Total despesas"
                    monthlyValues={totalDespesasMonthly}
                    average={totalDespesasAverage}
                    annualTotal={totalDespesasAnnualTotal}
                    emphasized
                  />
                </TableBody>
              </Table>
            </section>
          )}

          {activeTab === 'investments' && investmentAnnualResult && (
            <section className="annual-summary-page__section">
              <Table className="annual-summary-page__table data-table">
                <TableHeader>
                  <TableRow>
                    <SortableColumnHeader
                      label="Account"
                      columnKey="account"
                      sortDirection={investmentsSortState?.columnKey === 'account' ? investmentsSortState.direction : undefined}
                      onSort={requestInvestmentsSort}
                    />
                    {MONTH_LABELS.map((m, i) => (
                      <SortableColumnHeader
                        key={m}
                        label={m}
                        columnKey={`month-${i}`}
                        numeric
                        sortDirection={
                          investmentsSortState?.columnKey === `month-${i}` ? investmentsSortState.direction : undefined
                        }
                        onSort={requestInvestmentsSort}
                      />
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {sortedInvestmentAccounts.map((a) => (
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
                </TableBody>
              </Table>

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
              <Table className="annual-summary-page__table data-table">
                <TableHeader>
                  <TableRow>
                    <SortableColumnHeader
                      label="Category"
                      columnKey="category"
                      sortDirection={historicSortState?.columnKey === 'category' ? historicSortState.direction : undefined}
                      onSort={requestHistoricSort}
                    >
                      <ColumnFilterMenu
                        columnKey="category"
                        label="Category"
                        availableValues={historicAvailableValues.category}
                        selectedValues={historicSelectedValues.category}
                        onToggleValue={toggleHistoricValue}
                        onToggleAll={toggleHistoricAll}
                        isFiltered={isHistoricColumnFiltered('category')}
                      />
                    </SortableColumnHeader>
                    {historicSummaryAverage && (
                      historicSummaryAverage.map((y) => (
                        <SortableColumnHeader
                          key={y.year}
                          label={y.year}
                          columnKey={`year-${y.year}`}
                          numeric
                          sortDirection={
                            historicSortState?.columnKey === `year-${y.year}` ? historicSortState.direction : undefined
                          }
                          onSort={requestHistoricSort}
                        />
                      ))
                    )}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {historicSummaryAverage && sortedHistoricCategoryRows.length === 0 && isHistoricColumnFiltered('category') ? (
                    <TableRow>
                      <TableCell colSpan={historicSummaryAverage.length + 1}>No rows match the current filters</TableCell>
                    </TableRow>
                  ) : (
                    historicSummaryAverage &&
                    sortedHistoricCategoryRows.map((a) => {
                      const isEmphasized = HISTORIC_SUMMARY_AVERAGE_EMPHASIZED.has(a.category)
                      return (
                        <Fragment key={a.category}>
                          <TableRow className={isEmphasized ? 'annual-summary-page__emphasized-row' : undefined}>
                            <TableCell>{optionalEmphasize(a.category, isEmphasized)}</TableCell>
                            {historicSummaryAverage.map((y) => (
                              <TableCell key={y.year} className="data-table__col--numeric">
                                {optionalEmphasize(formatN2(y.annualAverages.find((d) => d.category === a.category)?.value ?? 0),
                                  isEmphasized)}
                              </TableCell>
                            ))}
                          </TableRow>
                          {HISTORIC_SUMMARY_AVERAGE_SPACER_AFTER.has(a.category) && (
                            <TableRow>
                              <TableCell colSpan={historicSummaryAverage.length + 1} />
                            </TableRow>
                          )}
                        </Fragment>
                      )
                    })
                  )}
                </TableBody>
              </Table>
            </section>
          )}
        </div>
      )}
    </div>
  )
}
