import type { RefObject } from 'react'
import { Button, MessageBar, MessageBarBody, Spinner } from '@fluentui/react-components'
import { DeleteRegular } from '@fluentui/react-icons'
import { useFormPanelStyles } from './formPanelStyles'

interface PotFormFooterProps {
  saveRef: RefObject<HTMLButtonElement | null>
  saveLabel: string
  deleteLabel: string | null
  isSaving: boolean
  isSaveDisabled: boolean
  error: string | null
  onDelete: () => void
  onCancel: () => void
}

export default function PotFormFooter({ saveRef, saveLabel, deleteLabel, isSaving, isSaveDisabled, error, onDelete, onCancel }: PotFormFooterProps) {
  const styles = useFormPanelStyles()

  return (
    <>
      <div className={styles.actions}>
        <Button
          ref={saveRef}
          type="submit"
          appearance="primary"
          disabled={isSaving || isSaveDisabled}
          icon={isSaving ? <Spinner size="tiny" /> : undefined}
        >
          {isSaving ? 'Saving...' : saveLabel}
        </Button>
        <Button appearance="secondary" onClick={onCancel}>
          Cancel
        </Button>
        {deleteLabel && (
          <Button className={styles.deleteAction} appearance="primary" icon={<DeleteRegular />} onClick={onDelete}>
            {deleteLabel}
          </Button>
        )}
      </div>

      {error && (
        <MessageBar role="alert" intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}
    </>
  )
}
