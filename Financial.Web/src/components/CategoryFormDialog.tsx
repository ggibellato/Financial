import { useState } from 'react'
import { Field, Input, Switch } from '@fluentui/react-components'
import type { CategoryDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'

interface CategoryFormDialogProps {
  category: CategoryDto | null
  onCancel: () => void
  onSubmit: (name: string, active: boolean, isInvestment: boolean, isTithe: boolean) => Promise<unknown>
}

export default function CategoryFormDialog({ category, onCancel, onSubmit }: CategoryFormDialogProps) {
  const isEditing = category !== null
  const [name, setName] = useState(category?.name ?? '')
  const [active, setActive] = useState(category?.active ?? true)
  const [isInvestment, setIsInvestment] = useState(category?.isInvestment ?? false)
  const [isTithe, setIsTithe] = useState(category?.isTithe ?? false)
  const { isSaving, error, submit } = useDialogSubmit('The category could not be saved.')

  const trimmedName = name.trim()
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const canSubmit = validationMessage.length === 0 && !isSaving

  const handleSubmit = () => {
    if (!canSubmit) return
    void submit(() => onSubmit(trimmedName, active, isInvestment, isTithe))
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit Category' : 'Create Category'}
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

      <Field label="Active">
        <Switch checked={active} onChange={(e) => setActive(e.target.checked)} disabled={isSaving} />
      </Field>

      <Field label="Investment">
        <Switch
          checked={isInvestment}
          onChange={(e) => setIsInvestment(e.target.checked)}
          disabled={isSaving}
        />
      </Field>

      <Field label="Tithe">
        <Switch checked={isTithe} onChange={(e) => setIsTithe(e.target.checked)} disabled={isSaving} />
      </Field>
    </EntityFormDialog>
  )
}
