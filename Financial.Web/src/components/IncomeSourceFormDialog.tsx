import { useState } from 'react'
import { Field, Input, Select, Switch } from '@fluentui/react-components'
import type { IncomeSourceDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'

const GROUP_OPTIONS = ['Salary', 'DividendoJuros', 'NonReportable']

interface IncomeSourceFormDialogProps {
  incomeSource: IncomeSourceDto | null
  onCancel: () => void
  onSubmit: (name: string, group: string, isActive: boolean, autoSplitToReserve: boolean) => Promise<unknown>
}

export default function IncomeSourceFormDialog({ incomeSource, onCancel, onSubmit }: IncomeSourceFormDialogProps) {
  const isEditing = incomeSource !== null
  const [name, setName] = useState(incomeSource?.name ?? '')
  const [group, setGroup] = useState(incomeSource?.group ?? GROUP_OPTIONS[0])
  const [isActive, setIsActive] = useState(incomeSource?.isActive ?? true)
  const [autoSplitToReserve, setAutoSplitToReserve] = useState(incomeSource?.autoSplitToReserve ?? false)
  const { isSaving, error, submit } = useDialogSubmit('The income source could not be saved.')

  const trimmedName = name.trim()
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const canSubmit = validationMessage.length === 0 && !isSaving

  const handleSubmit = () => {
    if (!canSubmit) return
    void submit(() => onSubmit(trimmedName, group, isActive, autoSplitToReserve))
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit Income Source' : 'Create Income Source'}
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

      <Field label="Group">
        <Select value={group} onChange={(e) => setGroup(e.target.value)} disabled={isSaving}>
          {GROUP_OPTIONS.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </Select>
      </Field>

      <Field label="Active">
        <Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} disabled={isSaving} />
      </Field>

      <Field label="Auto-split to reserve">
        <Switch
          checked={autoSplitToReserve}
          onChange={(e) => setAutoSplitToReserve(e.target.checked)}
          disabled={isSaving}
        />
      </Field>
    </EntityFormDialog>
  )
}
