import { useRef, type ReactNode } from 'react'
import { Table, TableBody, TableCell, TableHeader, TableRow } from '@fluentui/react-components'
import DataTableCell from './grid/DataTableCell'
import FilteredRowsNotice from './grid/FilteredRowsNotice'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { NO_FILTER_MATCH_MESSAGE, type FilterSummary } from '../hooks/useColumnFilters'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import SectionTotals from './SectionTotals'
import type { SectionTotalItem } from '../utils/currencyFooterItems'
import { allItemsTotalLabel } from '../utils/allItemsTotalLabel'

interface TotalsGridColumn<T> {
  key: string
  header: string
  numeric?: boolean
  render: (row: T) => ReactNode
  sortAccessor: SortAccessor<T>
  filterSlot?: ReactNode
}

export interface ColumnTotalsRow {
  label: string
  values: Partial<Record<string, ReactNode>>
}

interface TotalsGridProps<T> {
  label: string
  totalsLabel?: string
  columns: TotalsGridColumn<T>[]
  rows: T[]
  rowKey: (row: T) => string
  footerItems?: SectionTotalItem[]
  columnTotals?: ColumnTotalsRow[]
  filter?: FilterSummary & { itemsNoun: string }
  emptyMessage: string
}

export default function TotalsGrid<T>({ label, totalsLabel = `Totals for ${label}`, columns, rows, rowKey, footerItems = [], columnTotals = [], filter, emptyMessage }: TotalsGridProps<T>) {
  const accessors = Object.fromEntries(columns.map((col) => [col.key, col.sortAccessor]))
  const { sortedRows, sortState, requestSort } = useSortableRows(rows, accessors)
  const tableRef = useRef<HTMLTableElement>(null)

  return (
    <section className="monthly-page__section monthly-page__section--grid">
      {filter && <FilteredRowsNotice {...filter} tableRef={tableRef} />}
      <div className="monthly-page__table-scroll">
        <Table ref={tableRef} tabIndex={filter ? -1 : undefined} className="monthly-page__table data-table" aria-label={label}>
          <TableHeader>
            <TableRow>
              {columns.map((col) => (
                <SortableColumnHeader
                  key={col.key}
                  label={col.header}
                  columnKey={col.key}
                  numeric={col.numeric}
                  sortDirection={sortState?.columnKey === col.key ? sortState.direction : undefined}
                  onSort={requestSort}
                >
                  {col.filterSlot}
                </SortableColumnHeader>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {rows.length === 0 ? (
              <TableRow>
                <TableCell className="data-table__cell--message" colSpan={columns.length}>
                  {filter?.isFiltered ? NO_FILTER_MATCH_MESSAGE : emptyMessage}
                </TableCell>
              </TableRow>
            ) : (
              sortedRows.map((row) => (
                <TableRow key={rowKey(row)}>
                  {columns.map((col) => (
                    <DataTableCell
                      key={col.key}
                      label={col.header}
                      className={col.numeric ? 'data-table__col--numeric' : undefined}
                    >
                      {col.render(row)}
                    </DataTableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
          {columnTotals.length > 0 && rows.length > 0 && (
            <tfoot>
              {columnTotals.map((totals) => (
                <TableRow key={totals.label}>
                  {columns.map((col, index) => (
                    <DataTableCell
                      key={col.key}
                      label={index === 0 ? 'Total' : col.header}
                      className={[col.numeric && 'data-table__col--numeric', index > 0 && totals.values[col.key] === undefined && 'data-table__cell--no-total'].filter(Boolean).join(' ') || undefined}
                    >
                      {index === 0 ? totals.label : totals.values[col.key]}
                    </DataTableCell>
                  ))}
                </TableRow>
              ))}
            </tfoot>
          )}
        </Table>
      </div>
      {footerItems.length > 0 && (
        <SectionTotals
          label={totalsLabel}
          items={filter ? footerItems.map((item) => ({ ...item, label: allItemsTotalLabel(item.label, filter.itemsNoun, filter.isFiltered) })) : footerItems}
        />
      )}
    </section>
  )
}
