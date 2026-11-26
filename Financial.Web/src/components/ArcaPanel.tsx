import { useId } from 'react'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import DataTableCell from './grid/DataTableCell'
import { useBrokerArca } from '../hooks/useBrokerArca'
import type { ArcaAllocationDto, HoldingsTotalsDto } from '../api/types'
import { arcaBucketLabel } from '../utils/arcaBucket'
import { formatN2, formatPercentN2 } from '../utils/formatters'
import { amountsCaption, incompleteTotalNotice } from '../utils/holdingsTotals'
import { orMissing } from '../utils/orMissing'
import './ArcaPanel.css'

const NOT_PART_OF_ARCA = 'Not part of the ARCA percentages'

interface PanelRow {
  label: string
  totals: HoldingsTotalsDto
  outsideArca?: boolean
}

function PanelRowCells({ row }: { row: PanelRow }) {
  return (
    <TableRow>
      <TableHeaderCell scope="row">{row.label}</TableHeaderCell>
      <DataTableCell label="Current value" className="data-table__col--numeric">
        {orMissing(row.totals.marketValue, formatN2)}
      </DataTableCell>
      <DataTableCell label="% of ARCA" className="data-table__col--numeric">
        {orMissing(row.totals.marketShareOfBase, formatPercentN2, row.outsideArca ? NOT_PART_OF_ARCA : undefined)}
      </DataTableCell>
    </TableRow>
  )
}

function bodyRows(arca: ArcaAllocationDto): PanelRow[] {
  return [
    ...(arca.reserveFund ? [{ label: 'Reserve fund (3–12 months of expenses)', totals: arca.reserveFund, outsideArca: true }] : []),
    ...arca.buckets.map(bucket => ({ label: arcaBucketLabel(bucket.bucket), totals: bucket.totals })),
  ]
}

export default function ArcaPanel() {
  const { arca: response, isLoading, error, retry } = useBrokerArca()
  const headingId = useId()

  if (isLoading) return <LoadingState />
  if (error) return <ErrorState message="The ARCA allocation could not be loaded." onRetry={retry} />
  const arca = response?.arca
  if (!response || !arca) return null

  const notice = incompleteTotalNotice(arca.arcaAndReserveFundTotal)
  const hasValuedArca = arca.arcaTotal.holdingCount > arca.arcaTotal.unvaluedHoldingCount
  const footerRows: PanelRow[] = [
    { label: 'ARCA total', totals: arca.arcaTotal },
    { label: 'Grand total (ARCA + Reserve fund)', totals: arca.arcaAndReserveFundTotal, outsideArca: true },
  ]

  return (
    <section className="arca-panel" aria-labelledby={headingId}>
      <h3 id={headingId} className="arca-panel__heading">ARCA allocation</h3>
      <p className="arca-panel__caption">{amountsCaption('Current values', response.currencies)}</p>
      <div role="status">
        {!hasValuedArca && <p className="arca-panel__notice">No valued holdings in ARCA.</p>}
        {notice && <p className="arca-panel__notice">{notice}</p>}
      </div>
      <Table className="data-table arca-panel__table" aria-labelledby={headingId}>
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Bucket</TableHeaderCell>
            <TableHeaderCell className="data-table__col--numeric">Current value</TableHeaderCell>
            <TableHeaderCell className="data-table__col--numeric">% of ARCA</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {bodyRows(arca).map(row => <PanelRowCells key={row.label} row={row} />)}
        </TableBody>
        <tfoot>
          {footerRows.map(row => <PanelRowCells key={row.label} row={row} />)}
        </tfoot>
      </Table>
      {arca.notInArca && (
        <p className="arca-panel__caption">
          Portfolios not in ARCA ({orMissing(arca.notInArca.marketValue, formatN2)}) are left out of both totals.
        </p>
      )}
    </section>
  )
}
