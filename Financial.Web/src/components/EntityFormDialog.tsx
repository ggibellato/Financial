import { useLayoutEffect, type ReactNode } from 'react'
import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  MessageBar,
  MessageBarBody,
  Spinner,
} from '@fluentui/react-components'
import { useFormPanelStyles } from './formPanelStyles'

interface EntityFormDialogProps {
  title: string
  saveLabel?: string
  error: string | null
  isSaving: boolean
  canSave: boolean
  onSave: () => void
  onCancel: () => void
  children: ReactNode
}

export default function EntityFormDialog({
  title,
  saveLabel = 'Save',
  error,
  isSaving,
  canSave,
  onSave,
  onCancel,
  children,
}: EntityFormDialogProps) {
  const styles = useFormPanelStyles()

  useLayoutEffect(() => {
    const opener = document.activeElement as HTMLElement | null
    return () => {
      if (opener?.isConnected) opener.focus()
    }
  }, [])

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>{title}</DialogTitle>
          <DialogContent>
            {children}

            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}
          </DialogContent>
          <div className={styles.actions}>
            <Button
              appearance="primary"
              onClick={onSave}
              disabled={!canSave}
              icon={isSaving ? <Spinner size="tiny" /> : undefined}
            >
              {saveLabel}
            </Button>
            <Button appearance="secondary" onClick={onCancel} disabled={isSaving}>
              Cancel
            </Button>
          </div>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
