import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react'
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
} from '@fluentui/react-components'
import ActionErrorAlert from './ActionErrorAlert'
import DiscardChangesDialog from './DiscardChangesDialog'
import FieldErrorSummary from './FieldErrorSummary'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'
import { useDiscardPrompt } from '../hooks/useDiscardPrompt'
import { useFieldErrorSummary } from '../hooks/useFieldErrorSummary'
import { EDITABLE_FIELD } from '../utils/focus'

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
  const content = useRef<HTMLDivElement | null>(null)
  const discardPrompt = useDiscardPrompt(isDirty)
  const { summaryRef, summarizedErrors, submitWithSummary } = useFieldErrorSummary(content)

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

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!canSave || isSaving) return
    submitWithSummary(onSave, content.current)
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
              <DialogContent ref={content} className={styles.dialogContent}>
                {summarizedErrors.length > 0 && <FieldErrorSummary ref={summaryRef} errors={summarizedErrors} />}
                <ActionErrorAlert message={error} className={styles.dialogError} />
                <fieldset disabled={isSaving} className={styles.savingFieldset} role="presentation">
                  {children}
                </fieldset>
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
