import type { MouseEvent, ReactNode } from 'react'
import { Button, Spinner } from '@fluentui/react-components'
import { useFormPanelStyles } from './formPanelStyles'
import { useVisuallyHiddenStyles } from '../styles/visuallyHidden'

interface FormSaveActionsProps {
  saveLabel: string
  isSaving: boolean
  isSaveDisabled?: boolean
  isBusy?: boolean
  onCancel: () => void
  children?: ReactNode
}

const SAVING_LABEL = 'Saving…'

function blockSubmit(event: MouseEvent<HTMLButtonElement>) {
  event.preventDefault()
}

function takeFocusOnSubmit(event: MouseEvent<HTMLButtonElement>) {
  event.currentTarget.focus()
}

export default function FormSaveActions({ saveLabel, isSaving, isSaveDisabled = false, isBusy = isSaving, onCancel, children }: FormSaveActionsProps) {
  const styles = useFormPanelStyles()
  const hidden = useVisuallyHiddenStyles()

  return (
    <div className={styles.actions}>
      <Button
        type="submit"
        appearance="primary"
        disabled={!isSaving && (isSaveDisabled || isBusy)}
        disabledFocusable={isSaving}
        // While saving, Fluent drops its own click guard on a disabledFocusable <button>, so a click would still submit the form.
        onClickCapture={isSaving ? blockSubmit : takeFocusOnSubmit}
        icon={isSaving ? <Spinner size="tiny" /> : undefined}
      >
        {isSaving ? SAVING_LABEL : saveLabel}
      </Button>
      <Button appearance="secondary" disabled={isBusy} onClick={onCancel}>
        Cancel
      </Button>
      {children}
      <span role="status" className={hidden.visuallyHidden}>
        {isSaving ? SAVING_LABEL : ''}
      </span>
    </div>
  )
}
