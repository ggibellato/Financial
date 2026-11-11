import type { ReactNode } from 'react'
import { Button, Spinner, TableCell } from '@fluentui/react-components'
import { DeleteRegular, EditRegular } from '@fluentui/react-icons'

interface RowActionsCellProps {
  itemName: string
  editDisabled?: boolean
  isDeleting?: boolean
  onEdit?: (button: HTMLButtonElement) => void
  onDelete?: () => void
  children?: ReactNode
  className?: string
}

export default function RowActionsCell({
  itemName,
  editDisabled,
  isDeleting = false,
  onEdit,
  onDelete,
  children,
  className = 'data-table__col--action',
}: RowActionsCellProps) {
  return (
    <TableCell className={className}>
      <div className="data-table__actions-cell">
        {children}
        {onEdit && (
          <Button
            appearance="subtle"
            size="small"
            icon={<EditRegular />}
            aria-label={`Edit ${itemName}`}
            disabled={editDisabled}
            onClick={(event) => onEdit(event.currentTarget)}
          />
        )}
        {onDelete && (
          <Button
            appearance="subtle"
            size="small"
            icon={isDeleting ? <Spinner size="tiny" /> : <DeleteRegular />}
            aria-label={isDeleting ? `Deleting ${itemName}` : `Delete ${itemName}`}
            disabledFocusable={isDeleting}
            onClick={onDelete}
          />
        )}
      </div>
    </TableCell>
  )
}
