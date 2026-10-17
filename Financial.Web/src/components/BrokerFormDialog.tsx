import { useState } from 'react'
import { Field, InfoLabel, Input, Select } from '@fluentui/react-components'
import type { LabelProps } from '@fluentui/react-components'
import type { BrokerDto, CostBasisMethod } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
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
  const { isSaving, error, submit } = useDialogSubmit('The broker could not be saved.')

  const trimmedName = name.trim()
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const canSubmit = validationMessage.length === 0 && !isSaving

  const handleSubmit = () => {
    if (!canSubmit) return
    void submit(() => onSubmit(trimmedName, currency, costBasisMethod))
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit Broker' : 'Create Broker'}
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

      <Field label="Currency" required>
        <Select value={currency} onChange={(e) => setCurrency(e.target.value)} disabled={isSaving}>
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
              Cost Basis Method
            </InfoLabel>
          ),
        }}
        required
      >
        <Select
          value={costBasisMethod}
          onChange={(e) => setCostBasisMethod(e.target.value as CostBasisMethod)}
          disabled={isSaving}
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
