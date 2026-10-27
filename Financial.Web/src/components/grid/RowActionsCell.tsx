import type { ReactNode } from 'react'
import { Button, TableCell } from '@fluentui/react-components'
import { DeleteRegular, EditRegular } from '@fluentui/react-icons'

interface RowActionsCellProps {
  itemName: string
  editDisabled?: boolean
  deleteDisabled?: boolean
  onEdit: (button: HTMLButtonElement) => void
  onDelete: () => void
  children?: ReactNode
  className?: string
}

export default function RowActionsCell({
  itemName,
  editDisabled,
  deleteDisabled,
  onEdit,
  onDelete,
  children,
  className = 'data-table__col--action',
}: RowActionsCellProps) {
  return (
    <TableCell className={className}>
      <div className="data-table__actions-cell">
        {children}
        <Button
          appearance="subtle"
          size="small"
          icon={<EditRegular />}
          aria-label={`Edit ${itemName}`}
          disabled={editDisabled}
          onClick={(event) => onEdit(event.currentTarget)}
        />
        <Button
          appearance="subtle"
          size="small"
          icon={<DeleteRegular />}
          aria-label={`Delete ${itemName}`}
          disabled={deleteDisabled}
          onClick={onDelete}
        />
      </div>
    </TableCell>
  )
}
