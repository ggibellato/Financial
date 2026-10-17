import type { ReactNode } from 'react'
import { Button, Dialog, DialogBody, DialogContent, DialogSurface, DialogTitle } from '@fluentui/react-components'
import { useFormPanelStyles } from './formPanelStyles'

interface ConfirmDeleteDialogProps {
  entityLabel: string
  message: ReactNode
  confirmDisabled?: boolean
  onConfirm: () => void
  onCancel: () => void
}

export default function ConfirmDeleteDialog({
  entityLabel,
  message,
  confirmDisabled,
  onConfirm,
  onCancel,
}: ConfirmDeleteDialogProps) {
  const styles = useFormPanelStyles()

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>{`Delete ${entityLabel}`}</DialogTitle>
          <DialogContent>
            <p>{message}</p>
          </DialogContent>
          <div className={styles.actions}>
            <Button appearance="primary" onClick={onConfirm} disabled={confirmDisabled}>
              Delete
            </Button>
            <Button appearance="secondary" onClick={onCancel}>
              Cancel
            </Button>
          </div>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
