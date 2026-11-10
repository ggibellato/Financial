import type { MouseEvent } from 'react'
import { Button, Spinner } from '@fluentui/react-components'
import { useFormPanelStyles } from './formPanelStyles'

interface FormSaveActionsProps {
  saveLabel: string
  isSaving: boolean
  isSaveDisabled?: boolean
  onCancel: () => void
}

function blockSubmit(event: MouseEvent<HTMLButtonElement>) {
  event.preventDefault()
}

export default function FormSaveActions({ saveLabel, isSaving, isSaveDisabled = false, onCancel }: FormSaveActionsProps) {
  const styles = useFormPanelStyles()

  return (
    <div className={styles.actions}>
      <Button
        type="submit"
        appearance="primary"
        disabled={!isSaving && isSaveDisabled}
        disabledFocusable={isSaving}
        // Fluent drops its own click guard on a disabledFocusable <button>, so a click would still submit the form.
        onClickCapture={isSaving ? blockSubmit : undefined}
        icon={isSaving ? <Spinner size="tiny" /> : undefined}
      >
        {isSaving ? 'Saving...' : saveLabel}
      </Button>
      <Button appearance="secondary" disabled={isSaving} onClick={onCancel}>
        Cancel
      </Button>
    </div>
  )
}
