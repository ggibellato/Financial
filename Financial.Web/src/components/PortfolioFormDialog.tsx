import { useState } from 'react'
import { Field, Input, Select } from '@fluentui/react-components'
import type { BrokerDto, PortfolioDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'

interface PortfolioFormDialogProps {
  portfolio: PortfolioDto | null
  activeBrokers: BrokerDto[]
  onCancel: () => void
  onSubmit: (brokerName: string, name: string) => Promise<unknown>
}

export default function PortfolioFormDialog({ portfolio, activeBrokers, onCancel, onSubmit }: PortfolioFormDialogProps) {
  const isEditing = portfolio !== null
  const [brokerName, setBrokerName] = useState(portfolio?.brokerName ?? activeBrokers[0]?.name ?? '')
  const [name, setName] = useState(portfolio?.name ?? '')
  const { isSaving, error, submit } = useDialogSubmit('The portfolio could not be saved.')

  const trimmedName = name.trim()
  const nameValidationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const brokerValidationMessage = !isEditing && brokerName.length === 0 ? 'A broker is required.' : ''
  const canSubmit = nameValidationMessage.length === 0 && brokerValidationMessage.length === 0 && !isSaving

  const handleSubmit = () => {
    if (!canSubmit) return
    void submit(() => onSubmit(brokerName, trimmedName))
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit Portfolio' : 'Create Portfolio'}
      error={error}
      isSaving={isSaving}
      canSave={canSubmit}
      onSave={handleSubmit}
      onCancel={onCancel}
    >
      {isEditing ? (
        <Field label="Broker">
          <Input value={portfolio.brokerName} disabled readOnly />
        </Field>
      ) : (
        <Field
          label="Broker"
          required
          validationState={brokerValidationMessage ? 'error' : 'none'}
          validationMessage={brokerValidationMessage}
        >
          <Select value={brokerName} onChange={(e) => setBrokerName(e.target.value)} disabled={isSaving}>
            {activeBrokers.length === 0 && <option value="">No active brokers available</option>}
            {activeBrokers.map((b) => (
              <option key={b.name} value={b.name}>
                {b.name}
              </option>
            ))}
          </Select>
        </Field>
      )}

      <Field
        label="Name"
        required
        validationState={nameValidationMessage ? 'error' : 'none'}
        validationMessage={nameValidationMessage}
      >
        <Input value={name} onChange={(e) => setName(e.target.value)} disabled={isSaving} autoFocus />
      </Field>
    </EntityFormDialog>
  )
}
