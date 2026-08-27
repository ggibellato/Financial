import { Fragment, type ReactNode } from 'react'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'

interface TotalsGridColumn<T> {
  key: string
  header: string
  numeric?: boolean
  render: (row: T) => ReactNode
  sortAccessor: SortAccessor<T>
  filterSlot?: ReactNode
}

interface TotalsGridFooterItem {
  label: string
  value: ReactNode
}

interface TotalsGridProps<T> {
  columns: TotalsGridColumn<T>[]
  rows: T[]
  rowKey: (row: T) => string
  footerItems: TotalsGridFooterItem[]
  /** True when `rows` has already been narrowed by an active column filter (used only to pick the empty-state message). */
  isFiltered?: boolean
}

export default function TotalsGrid<T>({ columns, rows, rowKey, footerItems, isFiltered }: TotalsGridProps<T>) {
  const accessors = Object.fromEntries(columns.map((col) => [col.key, col.sortAccessor]))
  const { sortedRows, sortState, requestSort } = useSortableRows(rows, accessors)

  return (
    <section className="monthly-page__section monthly-page__section--grid">
      <div className="monthly-page__table-scroll">
        <table className="monthly-page__table data-table">
          <thead>
            <tr>
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
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 && isFiltered ? (
              <tr>
                <td colSpan={columns.length}>No rows match the current filters</td>
              </tr>
            ) : (
              sortedRows.map((row) => (
                <tr key={rowKey(row)}>
                  {columns.map((col) => (
                    <td key={col.key} className={col.numeric ? 'data-table__col--numeric' : undefined}>
                      {col.render(row)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
      <p className="monthly-page__section-total">
        {footerItems.map((item, index) => (
          <Fragment key={item.label}>
            {index > 0 && ' · '}
            {item.label}: <strong>{item.value}</strong>
          </Fragment>
        ))}
      </p>
    </section>
  )
}
