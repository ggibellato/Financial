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
}

export default function SortableColumnHeader({
  label,
  columnKey,
  sortDirection,
  onSort,
  numeric,
  children,
}: SortableColumnHeaderProps) {
  return (
    <th
      className={`sortable-column-header${numeric ? ' data-table__col--numeric' : ''}`}
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
