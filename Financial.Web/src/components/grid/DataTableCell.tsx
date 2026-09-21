import type { ReactNode } from 'react'
import { TableCell } from '@fluentui/react-components'

interface DataTableCellProps {
  label: string
  className?: string
  colSpan?: number
  children?: ReactNode
}

export default function DataTableCell({ label, className, colSpan, children }: DataTableCellProps) {
  return (
    <TableCell className={className} colSpan={colSpan}>
      <span className="data-table__cell-label">{label}:</span>
      {children}
    </TableCell>
  )
}
