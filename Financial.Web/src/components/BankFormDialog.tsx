import { useState } from 'react'
import { Field, Input, Switch } from '@fluentui/react-components'
import type { BankDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'

interface BankFormDialogProps {
  bank: BankDto | null
  onCancel: () => void
  onSubmit: (name: string, roundUpEnabled: boolean) => Promise<unknown>
}

export default function BankFormDialog({ bank, onCancel, onSubmit }: BankFormDialogProps) {
  const isEditing = bank !== null
  const [name, setName] = useState(bank?.name ?? '')
  const [roundUpEnabled, setRoundUpEnabled] = useState(bank?.roundUpEnabled ?? false)
  const { isSaving, error, submit } = useDialogSubmit('The bank could not be saved.')

  const trimmedName = name.trim()
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const canSubmit = validationMessage.length === 0 && !isSaving

  const handleSubmit = () => {
    if (!canSubmit) return
    void submit(() => onSubmit(trimmedName, roundUpEnabled))
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit Bank' : 'Create Bank'}
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

      <Field label="Round-up">
        <Switch
          checked={roundUpEnabled}
          onChange={(e) => setRoundUpEnabled(e.target.checked)}
          disabled={isSaving}
        />
      </Field>
    </EntityFormDialog>
  )
}
