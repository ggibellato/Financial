import type { MouseEvent, ReactElement, ReactNode } from 'react'
import { Button, Spinner, TableCell, Tooltip } from '@fluentui/react-components'
import { DeleteRegular, EditRegular } from '@fluentui/react-icons'

const OTHER_DELETE_RUNNING = 'Another delete is in progress.'

interface IconActionProps {
  label: string
  icon: ReactElement
  blockedReason?: string
  disabled?: boolean
  disabledFocusable?: boolean
  onClick: (event: MouseEvent<HTMLButtonElement>) => void
}

function IconAction({ label, icon, blockedReason, disabled, disabledFocusable, onClick }: IconActionProps) {
  return (
    <Tooltip
      content={blockedReason ?? label}
      relationship={blockedReason ? 'description' : 'inaccessible'}
      withArrow
    >
      <Button
        appearance="subtle"
        size="small"
        icon={icon}
        aria-label={label}
        disabled={disabled}
        disabledFocusable={disabledFocusable}
        onClick={onClick}
      />
    </Tooltip>
  )
}

interface RowActionsCellProps {
  itemName: string
  editDisabled?: boolean
  editBlockedReason?: string
  isDeleting?: boolean
  isOtherDeleting?: boolean
  onEdit?: (button: HTMLButtonElement) => void
  onDelete?: () => void
  children?: ReactNode
  className?: string
}

export default function RowActionsCell({
  itemName,
  editDisabled,
  editBlockedReason,
  isDeleting = false,
  isOtherDeleting = false,
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
          <IconAction
            label={`Edit ${itemName}`}
            icon={<EditRegular />}
            blockedReason={editBlockedReason}
            disabled={editDisabled}
            disabledFocusable={!!editBlockedReason}
            onClick={(event) => onEdit(event.currentTarget)}
          />
        )}
        {onDelete && (
          <IconAction
            label={isDeleting ? `Deleting ${itemName}` : `Delete ${itemName}`}
            icon={isDeleting ? <Spinner size="tiny" /> : <DeleteRegular />}
            blockedReason={isOtherDeleting ? OTHER_DELETE_RUNNING : undefined}
            disabledFocusable={isDeleting || isOtherDeleting}
            onClick={onDelete}
          />
        )}
      </div>
    </TableCell>
  )
}
