import { Button, Dialog, DialogBody, DialogContent, DialogSurface, DialogTitle } from '@fluentui/react-components'
import { useFormPanelStyles } from './formPanelStyles'

interface DiscardChangesDialogProps {
  open: boolean
  onKeepEditing: () => void
  onDiscard: () => void
}

export default function DiscardChangesDialog({ open, onKeepEditing, onDiscard }: DiscardChangesDialogProps) {
  const styles = useFormPanelStyles()

  return (
    <Dialog
      open={open}
      onOpenChange={(_, data) => {
        if (!data.open) onKeepEditing()
      }}
    >
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>Discard unsaved changes?</DialogTitle>
          <DialogContent>
            <p>Your changes to this form will be lost.</p>
          </DialogContent>
          <div className={styles.actions}>
            <Button appearance="primary" onClick={onDiscard}>
              Discard
            </Button>
            <Button appearance="secondary" onClick={onKeepEditing}>
              Keep editing
            </Button>
          </div>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
