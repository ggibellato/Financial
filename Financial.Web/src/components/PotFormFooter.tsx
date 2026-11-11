import { Button } from '@fluentui/react-components'
import { DeleteRegular } from '@fluentui/react-icons'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'

interface PotFormFooterProps {
  saveLabel: string
  deleteLabel: string | null
  isSaving: boolean
  isSaveDisabled: boolean
  onDelete: () => void
  onCancel: () => void
}

export default function PotFormFooter({ saveLabel, deleteLabel, isSaving, isSaveDisabled, onDelete, onCancel }: PotFormFooterProps) {
  const styles = useFormPanelStyles()

  return (
    <FormSaveActions saveLabel={saveLabel} isSaving={isSaving} isSaveDisabled={isSaveDisabled} onCancel={onCancel}>
      {deleteLabel && (
        <Button className={styles.deleteAction} appearance="primary" icon={<DeleteRegular />} disabled={isSaving} onClick={onDelete}>
          {deleteLabel}
        </Button>
      )}
    </FormSaveActions>
  )
}
