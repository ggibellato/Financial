import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Switch,
} from '@fluentui/react-components'
import type { CreditCardDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface CreditCardFormDialogProps {
  creditCard: CreditCardDto | null
  onCancel: () => void
  onSubmit: (name: string, isActive: boolean, nextInvoiceDueDate: string | null) => Promise<unknown>
}

export default function CreditCardFormDialog({ creditCard, onCancel, onSubmit }: CreditCardFormDialogProps) {
  const isEditing = creditCard !== null
  const [name, setName] = useState(creditCard?.name ?? '')
  const [isActive, setIsActive] = useState(creditCard?.isActive ?? true)
  const [nextInvoiceDueDate, setNextInvoiceDueDate] = useState(creditCard?.nextInvoiceDueDate ?? '')
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const trimmedName = name.trim()
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const canSubmit = validationMessage.length === 0 && !isSaving

  const handleSubmit = async () => {
    if (!canSubmit) return

    setIsSaving(true)
    setError(null)
    try {
      await onSubmit(trimmedName, isActive, nextInvoiceDueDate === '' ? null : nextInvoiceDueDate)
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'The credit card could not be saved.'))
      setIsSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>{isEditing ? 'Edit Credit Card' : 'Create Credit Card'}</DialogTitle>
          <DialogContent>
            <Field
              label="Name"
              required
              validationState={validationMessage ? 'error' : 'none'}
              validationMessage={validationMessage}
            >
              <Input value={name} onChange={(e) => setName(e.target.value)} disabled={isSaving} autoFocus />
            </Field>

            <Field label="Next Invoice Due Date">
              <Input
                type="date"
                value={nextInvoiceDueDate}
                onChange={(e) => setNextInvoiceDueDate(e.target.value)}
                disabled={isSaving}
              />
            </Field>

            <Field label="Active">
              <Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} disabled={isSaving} />
            </Field>

            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={() => void handleSubmit()} disabled={!canSubmit}>
              Save
            </Button>
            <Button appearance="secondary" onClick={onCancel} disabled={isSaving}>
              Cancel
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
