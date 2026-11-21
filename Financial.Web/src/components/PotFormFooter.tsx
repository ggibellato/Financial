import { Button, Tooltip } from '@fluentui/react-components'
import { DeleteRegular } from '@fluentui/react-icons'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'

interface PotFormFooterProps {
  saveLabel: string
  deleteLabel: string | null
  deleteBlockedReason?: string
  isSaving: boolean
  isSaveDisabled: boolean
  onDelete: () => void
  onCancel: () => void
}

export default function PotFormFooter({ saveLabel, deleteLabel, deleteBlockedReason, isSaving, isSaveDisabled, onDelete, onCancel }: PotFormFooterProps) {
  const styles = useFormPanelStyles()
  const deleteButton = deleteLabel && (
    <Button
      className={styles.deleteAction}
      appearance="primary"
      icon={<DeleteRegular />}
      disabled={isSaving}
      disabledFocusable={deleteBlockedReason !== undefined}
      onClick={onDelete}
    >
      {deleteLabel}
    </Button>
  )

  return (
    <FormSaveActions saveLabel={saveLabel} isSaving={isSaving} isSaveDisabled={isSaveDisabled} onCancel={onCancel}>
      {deleteButton && deleteBlockedReason ? (
        <Tooltip content={deleteBlockedReason} relationship="description" withArrow>
          {deleteButton}
        </Tooltip>
      ) : (
        deleteButton
      )}
    </FormSaveActions>
  )
}
