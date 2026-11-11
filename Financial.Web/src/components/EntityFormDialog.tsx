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
import DiscardChangesDialog from './DiscardChangesDialog'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'
import { useDiscardPrompt } from '../hooks/useDiscardPrompt'

const EDITABLE_FIELD =
  'input:not([type="hidden"]):not([readonly]):not([disabled]), select:not([disabled]), textarea:not([readonly]):not([disabled])'

interface EntityFormDialogProps {
  title: string
  saveLabel?: string
  error: string | null
  isSaving: boolean
  canSave?: boolean
  isDirty?: boolean
  onSave: () => void
  onCancel: () => void
  children: ReactNode
}

export default function EntityFormDialog({
  title,
  saveLabel = 'Save',
  error,
  isSaving,
  canSave = true,
  isDirty = false,
  onSave,
  onCancel,
  children,
}: EntityFormDialogProps) {
  const styles = useFormPanelStyles()
  const errorBar = useRef<HTMLDivElement | null>(null)
  const content = useRef<HTMLDivElement | null>(null)
  const discardPrompt = useDiscardPrompt(isDirty)
  const [saveAttempts, setSaveAttempts] = useState(0)

  const [opener] = useState(() => document.activeElement as HTMLElement | null)

  useEffect(
    () => () => {
      if (opener?.isConnected) opener.focus()
    },
    [opener],
  )

  useEffect(() => {
    content.current?.querySelector<HTMLElement>(EDITABLE_FIELD)?.focus()
  }, [])

  useEffect(() => {
    if (saveAttempts === 0 || content.current?.contains(document.activeElement)) return
    content.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus()
  }, [saveAttempts])

  useEffect(() => {
    if (!error) return
    errorBar.current?.scrollIntoView?.({ block: 'nearest' })
    errorBar.current?.focus()
  }, [error])

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!canSave || isSaving) return
    onSave()
    setSaveAttempts((attempts) => attempts + 1)
  }

  const requestClose = () => {
    if (!isSaving) discardPrompt.confirmDiscard(onCancel)
  }

  return (
    <>
      <Dialog open onOpenChange={(_, data) => { if (!data.open) requestClose() }}>
        <DialogSurface aria-describedby={undefined}>
          <form noValidate onSubmit={submit}>
            <DialogBody>
              <DialogTitle>{title}</DialogTitle>
              <DialogContent ref={content}>
                <fieldset disabled={isSaving} className={styles.savingFieldset}>
                  {children}
                </fieldset>

                {error && (
                  <MessageBar ref={errorBar} role="alert" intent="error" tabIndex={-1}>
                    <MessageBarBody>{error}</MessageBarBody>
                  </MessageBar>
                )}
              </DialogContent>
              <FormSaveActions saveLabel={saveLabel} isSaving={isSaving} isSaveDisabled={!canSave} onCancel={requestClose} />
            </DialogBody>
          </form>
        </DialogSurface>
      </Dialog>
      <DiscardChangesDialog
        open={discardPrompt.isPromptOpen}
        onKeepEditing={discardPrompt.keepEditing}
        onDiscard={discardPrompt.discard}
      />
    </>
  )
}
