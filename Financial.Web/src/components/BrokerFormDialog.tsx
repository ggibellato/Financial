import { useState } from 'react'
import { Field, InfoLabel, Input, Select } from '@fluentui/react-components'
import type { LabelProps } from '@fluentui/react-components'
import type { BrokerDto, CostBasisMethod } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { useDuplicateNameField } from '../hooks/useDuplicateNameField'
import { COST_BASIS_METHODS, COST_BASIS_METHOD_LABELS } from '../utils/costBasisMethod'
import { CURRENCIES } from '../utils/currency'

interface BrokerFormDialogProps {
  broker: BrokerDto | null
  onCancel: () => void
  onSubmit: (name: string, currency: string, costBasisMethod: CostBasisMethod) => Promise<unknown>
}

export default function BrokerFormDialog({ broker, onCancel, onSubmit }: BrokerFormDialogProps) {
  const isEditing = broker !== null
  const [name, setName] = useState(broker?.name ?? '')
  const [currency, setCurrency] = useState<string>(broker?.currency ?? CURRENCIES[0])
  const [costBasisMethod, setCostBasisMethod] = useState<CostBasisMethod>(broker?.costBasisMethod ?? 'AverageCost')
  const { isSaving, error, failure, submit } = useDialogSubmit('The broker could not be saved.')

  const trimmedName = name.trim()
  const { nameRef, nameError, summaryError } = useDuplicateNameField(failure, error, trimmedName)
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const nameMessage = validationMessage || nameError
  const canSubmit = validationMessage.length === 0 && !isSaving

  const handleSubmit = () => {
    if (!canSubmit) return
    void submit(() => onSubmit(trimmedName, currency, costBasisMethod))
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit broker' : 'New broker'}
      saveLabel={isEditing ? 'Save' : 'Add broker'}
      error={summaryError}
      isSaving={isSaving}
      canSave={canSubmit}
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

      <Field label="Currency" required>
        <Select value={currency} onChange={(e) => setCurrency(e.target.value)}>
          {CURRENCIES.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </Select>
      </Field>

      <Field
        label={{
          children: (_: unknown, props: LabelProps) => (
            <InfoLabel
              {...props}
              info="Changing this for an existing broker recalculates every disposal record under it."
            >
              Cost basis method
            </InfoLabel>
          ),
        }}
        required
      >
        <Select
          value={costBasisMethod}
          onChange={(e) => setCostBasisMethod(e.target.value as CostBasisMethod)}
        >
          {COST_BASIS_METHODS.map((method) => (
            <option key={method} value={method}>
              {COST_BASIS_METHOD_LABELS[method]}
            </option>
          ))}
        </Select>
      </Field>
    </EntityFormDialog>
  )
}
