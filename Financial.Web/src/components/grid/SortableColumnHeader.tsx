import type { ReactNode } from 'react'
import { ArrowSortDownRegular, ArrowSortUpRegular } from '@fluentui/react-icons'
import type { SortDirection } from '../../hooks/useSortableRows'
import './SortableColumnHeader.css'

interface SortableColumnHeaderProps {
  label: ReactNode
  columnKey: string
  sortDirection?: SortDirection
  onSort: (columnKey: string) => void
  numeric?: boolean
  /** Slot for a column-filter control (F03) to render inside the same header cell. */
  children?: ReactNode
  /** For a header spanning multiple <tr> rows, e.g. a grouped sub-column layout. */
  rowSpan?: number
  /** Extra class alongside the base header classes, e.g. a group's accent/separator style. */
  className?: string
}

export default function SortableColumnHeader({
  label,
  columnKey,
  sortDirection,
  onSort,
  numeric,
  children,
  rowSpan,
  className,
}: SortableColumnHeaderProps) {
  return (
    <th
      rowSpan={rowSpan}
      className={['sortable-column-header', numeric ? 'data-table__col--numeric' : '', className ?? '']
        .filter(Boolean)
        .join(' ')}
      aria-sort={sortDirection ?? 'none'}
    >
      <button type="button" className="sortable-column-header__button" onClick={() => onSort(columnKey)}>
        <span className="sortable-column-header__label">{label}</span>
        {sortDirection === 'ascending' && (
          <ArrowSortUpRegular className="sortable-column-header__icon" aria-hidden="true" />
        )}
        {sortDirection === 'descending' && (
          <ArrowSortDownRegular className="sortable-column-header__icon" aria-hidden="true" />
        )}
      </button>
      {children}
    </th>
  )
}
