import { Fragment, useMemo, useRef, useState } from 'react'
import { Tab, Table, TableBody, TableCell, TableHeader, TableRow } from '@fluentui/react-components'
import type { SelectTabData, SelectTabEvent } from '@fluentui/react-components'
import PageTabList from '../components/PageTabList'
import { useTabPanel } from '../hooks/useTabPanel'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import DataTableCell from '../components/grid/DataTableCell'
import SortableColumnHeader from '../components/grid/SortableColumnHeader'
import ColumnFilterMenu from '../components/grid/ColumnFilterMenu'
import FilteredRowsNotice from '../components/grid/FilteredRowsNotice'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { NO_FILTER_MATCH_MESSAGE, useColumnFilters } from '../hooks/useColumnFilters'
import { useAnnualSummary } from '../hooks/useAnnualSummary'
import type { CategoryAnnualAverageDto, CategoryAnnualTotalDto, InvestmentAnnualResultDto } from '../api/types'
import { formatN2 } from '../utils/formatters'
import { allItemsTotalLabel } from '../utils/allItemsTotalLabel'
import { RESULTADO_LABEL, splitHistoricRows, TOTAL_DESPESAS_LABEL, type HistoricAverageOf, type HistoricYear } from '../utils/annualSummaryTotals'
import './AnnualSummaryPage.css'

type InvestmentAccount = InvestmentAnnualResultDto['accounts'][number]
type HistoricCategoryRow = CategoryAnnualAverageDto['annualAverages'][number]

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
const SPACER_COL_SPAN = MONTH_LABELS.length + 3
const HISTORIC_SUMMARY_AVERAGE_SPACER_AFTER = new Set(['Tax difference', 'Dividendo/Juros', 'Reserva'])
const TOTAL_ROW_LABEL = 'Total'

function monthAccessors<T>(monthlyValuesOf: (row: T) => (number | null)[]): Record<string, SortAccessor<T>> {
  return Object.fromEntries(MONTH_LABELS.map((_, i) => [`month-${i}`, (row: T) => monthlyValuesOf(row)[i]]))
}

type AnnualSummaryTabId = 'categoryTotals' | 'investments' | 'historicSummaryAverage'

const TABS: { id: AnnualSummaryTabId; label: string }[] = [
  { id: 'categoryTotals', label: 'Category totals' },
  { id: 'investments', label: 'Investments' },
  { id: 'historicSummaryAverage', label: 'Historic summary average' },
]

function AnnualSummaryRow({
  label,
  monthlyValues,
  average,
  annualTotal,
  isTotal = false,
}: {
  label: string
  monthlyValues: number[]
  average: number
  annualTotal: number
  isTotal?: boolean
}) {
  return (
    <TableRow>
      <DataTableCell label={isTotal ? TOTAL_ROW_LABEL : 'Category'}>{label}</DataTableCell>
      {monthlyValues.map((v, i) => (
        <DataTableCell key={i} label={MONTH_LABELS[i]} className="data-table__col--numeric">
          {formatN2(v)}
        </DataTableCell>
      ))}
      <DataTableCell label="Average" className="data-table__col--numeric">
        <strong>{formatN2(average)}</strong>
      </DataTableCell>
      <DataTableCell label="Annual total" className="data-table__col--numeric">
        <strong>{formatN2(annualTotal)}</strong>
      </DataTableCell>
    </TableRow>
  )
}

function InvestmentRow({ label, monthlyValues, isTotal = false }: { label: string; monthlyValues: (number | null)[]; isTotal?: boolean }) {
  return (
    <TableRow>
      <DataTableCell label={isTotal ? TOTAL_ROW_LABEL : 'Account'}>{label}</DataTableCell>
      {monthlyValues.map((v, i) => (
        <DataTableCell key={i} label={MONTH_LABELS[i]} className="data-table__col--numeric">
          {v === null ? null : formatN2(v)}
        </DataTableCell>
      ))}
    </TableRow>
  )
}

function HistoricRow({
  category,
  label = category,
  isTotal = false,
  years,
  averageOf,
}: {
  category: string
  label?: string
  isTotal?: boolean
  years: HistoricYear[]
  averageOf: HistoricAverageOf
}) {
  return (
    <TableRow>
      <DataTableCell label={isTotal ? TOTAL_ROW_LABEL : 'Category'}>{label}</DataTableCell>
      {years.map((y) => (
        <DataTableCell key={y.year} label={String(y.year)} className="data-table__col--numeric">
          {formatN2(averageOf(category, y.year))}
        </DataTableCell>
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
  const tabPanel = useTabPanel(activeTab)
  const filterableTableRef = useRef<HTMLTableElement>(null)

  const {
    filteredRows: filteredCategoryTotals,
    availableValues: categoryTotalsAvailableValues,
    selectedValues: categoryTotalsSelectedValues,
    toggleValue: toggleCategoryTotalsValue,
    toggleAll: toggleCategoryTotalsAll,
    isColumnFiltered: isCategoryTotalsColumnFiltered,
    filterSummary: categoryTotalsFilterSummary,
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

  const historic = useMemo(() => splitHistoricRows(historicSummaryAverage), [historicSummaryAverage])
  const {
    filteredRows: filteredHistoricCategoryRows,
    availableValues: historicAvailableValues,
    selectedValues: historicSelectedValues,
    toggleValue: toggleHistoricValue,
    toggleAll: toggleHistoricAll,
    isColumnFiltered: isHistoricColumnFiltered,
    filterSummary: historicFilterSummary,
  } = useColumnFilters(historic.categoryRows, { category: (row: HistoricCategoryRow) => row.category })
  const {
    sortedRows: sortedHistoricCategoryRows,
    sortState: historicSortState,
    requestSort: requestHistoricSort,
  } = useSortableRows<HistoricCategoryRow>(filteredHistoricCategoryRows, {
    category: (row) => row.category,
    ...Object.fromEntries(
      historicSummaryAverage.map((y) => [
        `year-${y.year}`,
        (row: HistoricCategoryRow) => historic.averageOf(row.category, y.year),
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

      <PageTabList
        aria-label="Annual summary views"
        selectedValue={activeTab}
        onTabSelect={(_event: SelectTabEvent, data: SelectTabData) => setActiveTab(data.value as AnnualSummaryTabId)}
      >
        {TABS.map((tab) => (
          <Tab key={tab.id} value={tab.id} {...tabPanel.tabProps(tab.id)}>
            {tab.label}
          </Tab>
        ))}
      </PageTabList>

      <div className="annual-summary-page__panel" {...tabPanel.panelProps}>
        {isLoading ? (
          <LoadingState />
        ) : error ? (
          <ErrorState message={error} onRetry={retry} />
        ) : (
          <div className="annual-summary-page__content">
            {activeTab === 'categoryTotals' && (
              <section className="annual-summary-page__section">
                <FilteredRowsNotice {...categoryTotalsFilterSummary} tableRef={filterableTableRef} />
                <Table ref={filterableTableRef} tabIndex={-1} className="annual-summary-page__table data-table" aria-label="Category totals">
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
                        label="Annual total"
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
                        <TableCell className="data-table__cell--message" colSpan={SPACER_COL_SPAN}>
                          {NO_FILTER_MATCH_MESSAGE}
                        </TableCell>
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
                  </TableBody>
                  <tfoot>
                    {incomeSummary && (
                      <AnnualSummaryRow
                        label={allItemsTotalLabel(RESULTADO_LABEL, 'categories', categoryTotalsFilterSummary.isFiltered)}
                        isTotal
                        monthlyValues={resultadoMonthly}
                        average={resultadoAverage}
                        annualTotal={resultadoAnnualTotal}
                      />
                    )}
                    <AnnualSummaryRow
                      label={allItemsTotalLabel(TOTAL_DESPESAS_LABEL, 'categories', categoryTotalsFilterSummary.isFiltered)}
                      isTotal
                      monthlyValues={totalDespesasMonthly}
                      average={totalDespesasAverage}
                      annualTotal={totalDespesasAnnualTotal}
                    />
                  </tfoot>
                </Table>
              </section>
            )}

            {activeTab === 'investments' && investmentAnnualResult && (
              <section className="annual-summary-page__section">
                <Table className="annual-summary-page__table data-table" aria-label="Investments">
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
                  </TableBody>
                  <tfoot>
                    <InvestmentRow label="Total" isTotal monthlyValues={investmentAnnualResult.netPosition.monthlyValues} />
                    <InvestmentRow
                      label="Month result"
                      isTotal
                      monthlyValues={investmentAnnualResult.netPosition.monthlyDiffs}
                    />
                  </tfoot>
                </Table>

                <div className="annual-summary-page__investment-totals" role="group" aria-label="Totals for investments">
                  <div className="annual-summary-page__investment-total">
                    <span>Year progress</span>
                    <strong>{formatN2(investmentAnnualResult.netPosition.fullYearNetChange)}</strong>
                  </div>
                  <div className="annual-summary-page__investment-total">
                    <span>Average month result</span>
                    <strong>{formatN2(investmentAnnualResult.netPosition.averageMonthResult)}</strong>
                  </div>
                  <div className="annual-summary-page__investment-total">
                    <span>Sum of month results</span>
                    <strong>{formatN2(investmentAnnualResult.netPosition.sumOfMonthResults)}</strong>
                  </div>
                </div>
              </section>
            )}

            {activeTab === 'historicSummaryAverage' && (
              <section className="annual-summary-page__section">
                <FilteredRowsNotice {...historicFilterSummary} tableRef={filterableTableRef} />
                <Table ref={filterableTableRef} tabIndex={-1} className="annual-summary-page__table data-table" aria-label="Historic summary average">
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
                      {historicSummaryAverage.map((y) => (
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
                      ))}
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {sortedHistoricCategoryRows.length === 0 && isHistoricColumnFiltered('category') ? (
                      <TableRow>
                        <TableCell className="data-table__cell--message" colSpan={historicSummaryAverage.length + 1}>
                          {NO_FILTER_MATCH_MESSAGE}
                        </TableCell>
                      </TableRow>
                    ) : (
                      sortedHistoricCategoryRows.map((a) => (
                        <Fragment key={a.category}>
                          <HistoricRow category={a.category} years={historicSummaryAverage} averageOf={historic.averageOf} />
                          {HISTORIC_SUMMARY_AVERAGE_SPACER_AFTER.has(a.category) && (
                            <TableRow>
                              <TableCell colSpan={historicSummaryAverage.length + 1} />
                            </TableRow>
                          )}
                        </Fragment>
                      ))
                    )}
                  </TableBody>
                  {historic.totalRows.length > 0 && (
                    <tfoot>
                      {historic.totalRows.map((total) => (
                        <HistoricRow
                          key={total.category}
                          category={total.category}
                          label={allItemsTotalLabel(total.category, 'categories', historicFilterSummary.isFiltered)}
                          isTotal
                          years={historicSummaryAverage}
                          averageOf={historic.averageOf}
                        />
                      ))}
                    </tfoot>
                  )}
                </Table>
              </section>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
