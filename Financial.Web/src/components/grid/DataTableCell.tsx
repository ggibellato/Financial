import type { ReactNode } from 'react'
import { TableCell } from '@fluentui/react-components'

interface DataTableCellProps {
  label: string
  className?: string
  colSpan?: number
  children?: ReactNode
}

/**
 * A `.data-table` TableCell with a leading label, read on the ≤599px mobile
 * card layout (see src/styles/data-table.css) where there's no header row to
 * align a value against.
 */
export default function DataTableCell({ label, className, colSpan, children }: DataTableCellProps) {
  return (
    <TableCell className={className} colSpan={colSpan}>
      <span className="data-table__cell-label">{label}:</span>
      {children}
    </TableCell>
  )
}
