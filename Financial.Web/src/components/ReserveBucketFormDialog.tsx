import { useState } from 'react'
import { Field, Input, Switch } from '@fluentui/react-components'
import type { ReserveBucketDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'

interface ReserveBucketFormDialogProps {
  reserveBucket: ReserveBucketDto | null
  onCancel: () => void
  onSubmit: (name: string, splitPercentage: number, isActive: boolean) => Promise<ReserveBucketDto>
}

export default function ReserveBucketFormDialog({ reserveBucket, onCancel, onSubmit }: ReserveBucketFormDialogProps) {
  const isEditing = reserveBucket !== null
  const [name, setName] = useState(reserveBucket?.name ?? '')
  const [splitPercentage, setSplitPercentage] = useState(String(reserveBucket?.splitPercentage ?? ''))
  const [isActive, setIsActive] = useState(reserveBucket?.isActive ?? true)
  const { isSaving, error, submit } = useDialogSubmit('The reserve bucket could not be saved.')

  const trimmedName = name.trim()
  const parsedSplit = Number(splitPercentage)
  const nameError = trimmedName.length === 0 ? 'Name is required.' : ''
  const splitError =
    splitPercentage.trim().length === 0 || Number.isNaN(parsedSplit) || parsedSplit < 0 || parsedSplit > 100
      ? 'Split percentage must be between 0 and 100.'
      : ''

  const canSubmit = !nameError && !splitError && !isSaving

  const handleSubmit = () => {
    if (!canSubmit) return
    void submit(async () => {
      await onSubmit(trimmedName, parsedSplit, isActive)
      onCancel()
    })
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit Reserve Bucket' : 'Create Reserve Bucket'}
      error={error}
      isSaving={isSaving}
      canSave={canSubmit}
      onSave={handleSubmit}
      onCancel={onCancel}
    >
      <Field
        label="Name"
        required
        validationState={nameError ? 'error' : 'none'}
        validationMessage={nameError}
      >
        <Input value={name} onChange={(e) => setName(e.target.value)} disabled={isSaving} autoFocus />
      </Field>

      <Field
        label="Split Percentage"
        required
        validationState={splitError ? 'error' : 'none'}
        validationMessage={splitError}
      >
        <Input
          type="number"
          value={splitPercentage}
          onChange={(e) => setSplitPercentage(e.target.value)}
          disabled={isSaving}
        />
      </Field>

      <Field label="Active">
        <Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} disabled={isSaving} />
      </Field>
    </EntityFormDialog>
  )
}
