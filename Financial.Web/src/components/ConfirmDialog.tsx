import { useEffect, useId, useLayoutEffect, useRef, type ReactNode } from 'react'
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

export interface ConfirmDialogProps {
  open: boolean
  title: string
  message: ReactNode
  confirmLabel?: string
  cancelLabel: string
  confirmDisabled?: boolean
  isBusy?: boolean
  error?: string | null
  onConfirm: () => void
  onCancel: () => void
}

export default function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel,
  cancelLabel,
  confirmDisabled,
  isBusy = false,
  error,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const styles = useFormPanelStyles()
  const descriptionId = useId()
  const restoreFocusTarget = useRef<HTMLElement | null>(null)

  useLayoutEffect(() => {
    if (open) restoreFocusTarget.current = document.activeElement as HTMLElement | null
  }, [open])

  useEffect(() => {
    if (open || !restoreFocusTarget.current) return
    const target = restoreFocusTarget.current
    restoreFocusTarget.current = null
    if (target.isConnected) target.focus()
  }, [open])

  return (
    <Dialog
      open={open}
      onOpenChange={(_, data) => {
        if (!data.open && !isBusy) onCancel()
      }}
    >
      <DialogSurface aria-describedby={descriptionId}>
        <DialogBody>
          <DialogTitle>{title}</DialogTitle>
          <DialogContent>
            <p id={descriptionId}>{message}</p>
            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}
          </DialogContent>
          <div className={styles.actions}>
            {confirmLabel && (
              <Button
                appearance="primary"
                onClick={onConfirm}
                disabled={confirmDisabled || isBusy}
                icon={isBusy ? <Spinner size="tiny" /> : undefined}
              >
                {confirmLabel}
              </Button>
            )}
            <Button appearance="secondary" onClick={onCancel} disabled={isBusy}>
              {cancelLabel}
            </Button>
          </div>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
