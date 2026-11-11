import { useState } from 'react'
import { Field, Input, Switch } from '@fluentui/react-components'
import type { CategoryDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { useEntityForm } from '../hooks/useEntityForm'
import { useDuplicateNameField } from '../hooks/useDuplicateNameField'

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
  const { isSaving, error, failure, submit } = useDialogSubmit('The category could not be saved.')
  const form = useEntityForm({ name, active, isInvestment, isTithe })

  const trimmedName = name.trim()
  const { nameRef, nameError, summaryError } = useDuplicateNameField(failure, error, trimmedName)
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const nameMessage = form.shown(validationMessage) || nameError
  const canSubmit = validationMessage.length === 0

  const handleSubmit = () => {
    form.revealValidation()
    if (!canSubmit) return
    void submit(() => onSubmit(trimmedName, active, isInvestment, isTithe))
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit category' : 'New category'}
      saveLabel={isEditing ? 'Save' : 'Add category'}
      error={summaryError}
      isSaving={isSaving}
      isDirty={form.isDirty}
      onSave={handleSubmit}
      onCancel={onCancel}
    >
      <Field
        label="Name"
        required
        validationState={nameMessage ? 'error' : 'none'}
        validationMessage={nameMessage}
      >
        <Input ref={nameRef} value={name} onChange={(e) => setName(e.target.value)} />
      </Field>

      <Field label="Active">
        <Switch checked={active} onChange={(e) => setActive(e.target.checked)} />
      </Field>

      <Field label="Investment">
        <Switch
          checked={isInvestment}
          onChange={(e) => setIsInvestment(e.target.checked)}
        />
      </Field>

      <Field label="Tithe">
        <Switch checked={isTithe} onChange={(e) => setIsTithe(e.target.checked)} />
      </Field>
    </EntityFormDialog>
  )
}
