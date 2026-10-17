import { useState } from 'react'
import { Field, Input, Switch } from '@fluentui/react-components'
import type { CreditCardDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'

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
  const { isSaving, error, submit } = useDialogSubmit('The credit card could not be saved.')

  const trimmedName = name.trim()
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const canSubmit = validationMessage.length === 0 && !isSaving

  const handleSubmit = () => {
    if (!canSubmit) return
    void submit(() => onSubmit(trimmedName, isActive, nextInvoiceDueDate === '' ? null : nextInvoiceDueDate))
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit Credit Card' : 'Create Credit Card'}
      error={error}
      isSaving={isSaving}
      canSave={canSubmit}
      onSave={handleSubmit}
      onCancel={onCancel}
    >
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
    </EntityFormDialog>
  )
}
