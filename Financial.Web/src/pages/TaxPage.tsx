import {
  Button,
  Field,
  Select,
  Table,
  TableBody,
  TableHeader,
  TableHeaderCell,
  TableRow,
} from '@fluentui/react-components'
import { DocumentArrowDownRegular } from '@fluentui/react-icons'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import StatusBadge from '../components/StatusBadge'
import DataTableCell from '../components/grid/DataTableCell'
import { useTaxWorkbook } from '../hooks/useTaxWorkbook'
import type { TaxCategoryTotalDto, TaxWorkbookEntryDto } from '../api/types'
import { formatN2, formatShortDateUtc } from '../utils/formatters'
import './TaxPage.css'

function amountCell(label: string, value: number | null | undefined) {
  return (
    <DataTableCell label={label} className="data-table__col--numeric">
      {value === null || value === undefined ? '—' : formatN2(value)}
    </DataTableCell>
  )
}

function EntryRow({ entry }: { entry: TaxWorkbookEntryDto }) {
  return (
    <TableRow>
      <DataTableCell label="Date">{formatShortDateUtc(entry.date)}</DataTableCell>
      <DataTableCell label="Category">{entry.eventCategory}</DataTableCell>
      {amountCell('Proceeds', entry.proceeds)}
      {amountCell('Cost Basis', entry.costBasis)}
      {amountCell('Gain/Loss', entry.gainLoss)}
      {amountCell('Gross', entry.grossAmount)}
      {amountCell('Withheld', entry.withheldAmount)}
      {amountCell('Net', entry.netAmount)}
      <DataTableCell label="Status">
        <StatusBadge status={entry.calculationStatus} />
      </DataTableCell>
      <DataTableCell label="Evidence" className="tax-page__evidence-cell">
        {entry.evidenceReference}
      </DataTableCell>
    </TableRow>
  )
}

function CategoryTotalRow({ total }: { total: TaxCategoryTotalDto }) {
  return (
    <TableRow>
      <DataTableCell label="Category">{total.eventCategory}</DataTableCell>
      {amountCell('Proceeds', total.totalProceeds)}
      {amountCell('Cost Basis', total.totalCostBasis)}
      {amountCell('Gain/Loss', total.totalGainLoss)}
      {amountCell('Gross', total.totalGrossAmount)}
      {amountCell('Withheld', total.totalWithheldAmount)}
      {amountCell('Net', total.totalNetAmount)}
    </TableRow>
  )
}

export default function TaxPage() {
  const {
    isLoadingOptions,
    optionsError,
    retryOptions,
    jurisdictions,
    taxYearsForJurisdiction,
    jurisdiction,
    taxYear,
    selectJurisdiction,
    selectTaxYear,
    workbook,
    isLoadingWorkbook,
    workbookError,
    retryWorkbook,
    canExportCsv,
    exportCsv,
  } = useTaxWorkbook()

  return (
    <section className="tax-page">
      <header className="tax-page__header">
        <h2>Tax</h2>
      </header>

      {isLoadingOptions ? (
        <LoadingState />
      ) : optionsError ? (
        <ErrorState message={optionsError} onRetry={retryOptions} />
      ) : jurisdictions.length === 0 ? (
        <p className="tax-page__empty">
          No classified disposals or income events yet — once one is recorded, its jurisdiction and tax
          year will appear here.
        </p>
      ) : (
        <>
          <div className="tax-page__filters">
            <Field label="Jurisdiction">
              <Select value={jurisdiction ?? ''} onChange={(e) => selectJurisdiction(e.target.value)}>
                {jurisdictions.map((j) => (
                  <option key={j} value={j}>
                    {j}
                  </option>
                ))}
              </Select>
            </Field>
            <Field label="Tax Year">
              <Select value={taxYear ?? ''} onChange={(e) => selectTaxYear(e.target.value)}>
                {taxYearsForJurisdiction.map((y) => (
                  <option key={y} value={y}>
                    {y}
                  </option>
                ))}
              </Select>
            </Field>
          </div>

          <div className="tax-page__toolbar">
            <Button
              appearance="primary"
              icon={<DocumentArrowDownRegular />}
              disabled={!canExportCsv}
              onClick={exportCsv}
            >
              Export CSV
            </Button>
            {workbook?.calculationStatus && (
              <span className="tax-page__aggregate-status">
                Overall status: <StatusBadge status={workbook.calculationStatus} />
              </span>
            )}
          </div>

          {isLoadingWorkbook ? (
            <LoadingState />
          ) : workbookError ? (
            <ErrorState message={workbookError} onRetry={retryWorkbook} />
          ) : workbook && workbook.entries.length === 0 ? (
            <p className="tax-page__empty">
              No classified events for {jurisdiction} {taxYear} yet.
            </p>
          ) : workbook ? (
            <>
              <div className="tax-page__table-scroll">
                <Table aria-label="Tax workbook entries" className="data-table">
                  <TableHeader>
                    <TableRow>
                      <TableHeaderCell>Date</TableHeaderCell>
                      <TableHeaderCell>Category</TableHeaderCell>
                      <TableHeaderCell className="data-table__col--numeric">Proceeds</TableHeaderCell>
                      <TableHeaderCell className="data-table__col--numeric">Cost Basis</TableHeaderCell>
                      <TableHeaderCell className="data-table__col--numeric">Gain/Loss</TableHeaderCell>
                      <TableHeaderCell className="data-table__col--numeric">Gross</TableHeaderCell>
                      <TableHeaderCell className="data-table__col--numeric">Withheld</TableHeaderCell>
                      <TableHeaderCell className="data-table__col--numeric">Net</TableHeaderCell>
                      <TableHeaderCell>Status</TableHeaderCell>
                      <TableHeaderCell>Evidence</TableHeaderCell>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {workbook.entries.map((entry) => (
                      <EntryRow key={entry.id} entry={entry} />
                    ))}
                  </TableBody>
                </Table>
              </div>

              <h3 className="tax-page__section-title">Category Totals</h3>
              <div className="tax-page__table-scroll">
                <Table aria-label="Tax category totals" className="data-table tax-page__totals-table">
                  <TableHeader>
                    <TableRow>
                      <TableHeaderCell>Category</TableHeaderCell>
                      <TableHeaderCell className="data-table__col--numeric">Proceeds</TableHeaderCell>
                      <TableHeaderCell className="data-table__col--numeric">Cost Basis</TableHeaderCell>
                      <TableHeaderCell className="data-table__col--numeric">Gain/Loss</TableHeaderCell>
                      <TableHeaderCell className="data-table__col--numeric">Gross</TableHeaderCell>
                      <TableHeaderCell className="data-table__col--numeric">Withheld</TableHeaderCell>
                      <TableHeaderCell className="data-table__col--numeric">Net</TableHeaderCell>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {workbook.categoryTotals.map((total) => (
                      <CategoryTotalRow key={total.eventCategory} total={total} />
                    ))}
                  </TableBody>
                </Table>
              </div>
            </>
          ) : null}
        </>
      )}
    </section>
  )
}
