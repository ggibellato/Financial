import { Button, TableCell } from '@fluentui/react-components'
import { DeleteRegular, EditRegular } from '@fluentui/react-icons'

interface RowActionsCellProps {
  itemName: string
  deleteDisabled: boolean
  onEdit: () => void
  onDelete: () => void
}

export default function RowActionsCell({ itemName, deleteDisabled, onEdit, onDelete }: RowActionsCellProps) {
  return (
    <TableCell className="data-table__col--action">
      <div className="data-table__actions-cell">
        <Button appearance="subtle" size="small" icon={<EditRegular />} aria-label={`Edit ${itemName}`} onClick={onEdit} />
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
