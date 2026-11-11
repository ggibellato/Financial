import { useState } from 'react'
import { Field, Input, Select } from '@fluentui/react-components'
import type { BrokerDto, PortfolioDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { useDuplicateNameField } from '../hooks/useDuplicateNameField'

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
  const { isSaving, error, failure, submit } = useDialogSubmit('The portfolio could not be saved.')

  const trimmedName = name.trim()
  const { nameRef, nameError, summaryError } = useDuplicateNameField(failure, error, JSON.stringify([brokerName, trimmedName]))
  const nameValidationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const nameMessage = nameValidationMessage || nameError
  const brokerValidationMessage = !isEditing && brokerName.length === 0 ? 'A broker is required.' : ''
  const canSubmit = nameValidationMessage.length === 0 && brokerValidationMessage.length === 0 && !isSaving

  const handleSubmit = () => {
    if (!canSubmit) return
    void submit(() => onSubmit(brokerName, trimmedName))
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit portfolio' : 'New portfolio'}
      saveLabel={isEditing ? 'Save' : 'Add portfolio'}
      error={summaryError}
      isSaving={isSaving}
      canSave={canSubmit}
      onSave={handleSubmit}
      onCancel={onCancel}
    >
      {isEditing ? (
        <Field label="Broker">
          <Input value={portfolio.brokerName} readOnly appearance="filled-darker" />
        </Field>
      ) : (
        <Field
          label="Broker"
          required
          validationState={brokerValidationMessage ? 'error' : 'none'}
          validationMessage={brokerValidationMessage}
        >
          <Select value={brokerName} onChange={(e) => setBrokerName(e.target.value)}>
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
        validationState={nameMessage ? 'error' : 'none'}
        validationMessage={nameMessage}
      >
        <Input ref={nameRef} value={name} onChange={(e) => setName(e.target.value)} />
      </Field>
    </EntityFormDialog>
  )
}
