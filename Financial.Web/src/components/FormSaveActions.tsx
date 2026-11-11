import { useState, type MouseEvent, type ReactNode } from 'react'
import { Button, Spinner } from '@fluentui/react-components'
import { useFormPanelStyles } from './formPanelStyles'
import { useVisuallyHiddenStyles } from '../styles/visuallyHidden'
import { SAVING_LABEL } from '../utils/formatters'

interface FormSaveActionsProps {
  saveLabel: string
  isSaving: boolean
  isSaveDisabled?: boolean
  isBusy?: boolean
  onCancel: () => void
  children?: ReactNode
}

function blockSubmit(event: MouseEvent<HTMLButtonElement>) {
  event.preventDefault()
}

function takeFocusOnSubmit(event: MouseEvent<HTMLButtonElement>) {
  event.currentTarget.focus()
}

export default function FormSaveActions({ saveLabel, isSaving, isSaveDisabled = false, isBusy = isSaving, onCancel, children }: FormSaveActionsProps) {
  const styles = useFormPanelStyles()
  const hidden = useVisuallyHiddenStyles()
  const [isSaveFocused, setIsSaveFocused] = useState(false)
  // A focused button already announces its own "Saving…" name change, so the status region speaks only when focus is elsewhere.
  const announcesSaving = isSaving && !isSaveFocused

  return (
    <div className={styles.actions}>
      <Button
        type="submit"
        onFocus={() => setIsSaveFocused(true)}
        onBlur={() => setIsSaveFocused(false)}
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
        {announcesSaving ? SAVING_LABEL : ''}
      </span>
    </div>
  )
}
