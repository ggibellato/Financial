import { useRef, type ReactNode } from 'react'
import { Table, TableBody, TableCell, TableHeader, TableRow } from '@fluentui/react-components'
import DataTableCell from './grid/DataTableCell'
import FilteredRowsNotice from './grid/FilteredRowsNotice'
import SortableColumnHeader from './grid/SortableColumnHeader'
import type { FilterSummary } from '../hooks/useColumnFilters'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import SectionTotals from './SectionTotals'
import type { SectionTotalItem } from '../utils/currencyFooterItems'
import { allRowsTotalLabel } from '../utils/allRowsTotalLabel'

interface TotalsGridColumn<T> {
  key: string
  header: string
  numeric?: boolean
  render: (row: T) => ReactNode
  sortAccessor: SortAccessor<T>
  filterSlot?: ReactNode
}

interface TotalsGridProps<T> {
  label: string
  columns: TotalsGridColumn<T>[]
  rows: T[]
  rowKey: (row: T) => string
  footerItems: SectionTotalItem[]
  filter?: FilterSummary
  emptyMessage: string
}

export default function TotalsGrid<T>({ label, columns, rows, rowKey, footerItems, filter, emptyMessage }: TotalsGridProps<T>) {
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
                  {filter?.isFiltered ? 'No rows match the current filters' : emptyMessage}
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
        </Table>
      </div>
      {footerItems.length > 0 && (
        <SectionTotals items={footerItems.map((item) => ({ ...item, label: allRowsTotalLabel(item.label, filter?.isFiltered) }))} />
      )}
    </section>
  )
}
