import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react'
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components'
import FormSaveActions from './FormSaveActions'

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
  const errorBar = useRef<HTMLDivElement | null>(null)

  const [opener] = useState(() => document.activeElement as HTMLElement | null)

  useEffect(
    () => () => {
      if (opener?.isConnected) opener.focus()
    },
    [opener],
  )

  useEffect(() => {
    if (!error) return
    errorBar.current?.scrollIntoView?.({ block: 'nearest' })
    errorBar.current?.focus()
  }, [error])

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (canSave) onSave()
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open && !isSaving) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <form noValidate onSubmit={submit}>
          <DialogBody>
            <DialogTitle>{title}</DialogTitle>
            <DialogContent>
              {children}

              {error && (
                <MessageBar ref={errorBar} role="alert" intent="error" tabIndex={-1}>
                  <MessageBarBody>{error}</MessageBarBody>
                </MessageBar>
              )}
            </DialogContent>
            <FormSaveActions saveLabel={saveLabel} isSaving={isSaving} isSaveDisabled={!canSave} onCancel={onCancel} />
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
