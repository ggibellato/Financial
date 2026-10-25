import type { ReactNode } from 'react'
import { Table, TableBody, TableCell, TableHeader, TableRow } from '@fluentui/react-components'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import SectionTotals from './SectionTotals'
import type { SectionTotalItem } from '../utils/currencyFooterItems'

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
  /** True when `rows` has already been narrowed by an active column filter (used only to pick the empty-state message). */
  isFiltered?: boolean
  emptyMessage: string
}

export default function TotalsGrid<T>({ label, columns, rows, rowKey, footerItems, isFiltered, emptyMessage }: TotalsGridProps<T>) {
  const accessors = Object.fromEntries(columns.map((col) => [col.key, col.sortAccessor]))
  const { sortedRows, sortState, requestSort } = useSortableRows(rows, accessors)

  return (
    <section className="monthly-page__section monthly-page__section--grid">
      <div className="monthly-page__table-scroll">
        <Table className="monthly-page__table data-table" aria-label={label}>
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
                  {isFiltered ? 'No rows match the current filters' : emptyMessage}
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
      <SectionTotals items={footerItems} />
    </section>
  )
}
