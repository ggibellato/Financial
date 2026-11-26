import { useState } from 'react'
import { Field, InfoLabel, Input, Select, type LabelProps } from '@fluentui/react-components'
import type { BrokerDto, PortfolioDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import ReadOnlyInput from './ReadOnlyInput'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { useEntityForm } from '../hooks/useEntityForm'
import { useDuplicateNameField } from '../hooks/useDuplicateNameField'
import { ARCA_BUCKET_LABELS, ARCA_BUCKET_ORDER, NOT_IN_ARCA, type ArcaBucket } from '../utils/arcaBucket'
import { optionalChoiceOptions } from '../utils/selectOptions'

interface PortfolioFormDialogProps {
  portfolio: PortfolioDto | null
  activeBrokers: BrokerDto[]
  onCancel: () => void
  onSubmit: (brokerName: string, name: string, arcaBucket: ArcaBucket | null) => Promise<unknown>
}

const ARCA_BUCKET_OPTIONS = optionalChoiceOptions(ARCA_BUCKET_ORDER, ARCA_BUCKET_LABELS, NOT_IN_ARCA)
const ARCA_BUCKET_INFO =
  "Places this portfolio in the broker's ARCA allocation on the Holdings and Summary tabs. A Reserve fund is shown apart and is not part of the ARCA percentages."

export default function PortfolioFormDialog({ portfolio, activeBrokers, onCancel, onSubmit }: PortfolioFormDialogProps) {
  const isEditing = portfolio !== null
  const [brokerName, setBrokerName] = useState(portfolio?.brokerName ?? activeBrokers[0]?.name ?? '')
  const [name, setName] = useState(portfolio?.name ?? '')
  const [arcaBucket, setArcaBucket] = useState<ArcaBucket | ''>(portfolio?.arcaBucket ?? '')
  const { isSaving, error, failure, submit } = useDialogSubmit('The portfolio could not be saved.')
  const form = useEntityForm({ brokerName, name, arcaBucket })

  const trimmedName = name.trim()
  const { nameRef, nameError, summaryError } = useDuplicateNameField(failure, error, JSON.stringify([brokerName, trimmedName]))
  const nameValidationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const nameMessage = form.shown(nameValidationMessage) || nameError
  const brokerValidationMessage = !isEditing && brokerName.length === 0 ? 'A broker is required.' : ''
  const canSubmit = nameValidationMessage.length === 0 && brokerValidationMessage.length === 0

  const handleSubmit = () => {
    form.revealValidation()
    if (!canSubmit) return
    void submit(() => onSubmit(brokerName, trimmedName, arcaBucket || null))
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit portfolio' : 'New portfolio'}
      saveLabel={isEditing ? 'Save' : 'Add portfolio'}
      error={summaryError}
      isSaving={isSaving}
      isDirty={form.isDirty}
      onSave={handleSubmit}
      onCancel={onCancel}
    >
      {isEditing ? (
        <Field label="Broker">
          <ReadOnlyInput value={portfolio.brokerName} />
        </Field>
      ) : (
        <Field
          label="Broker"
          required
          {...form.fieldValidation(brokerValidationMessage)}
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

      <Field
        label={{
          children: (_: unknown, props: LabelProps) => (
            <InfoLabel {...props} info={ARCA_BUCKET_INFO}>
              ARCA bucket
            </InfoLabel>
          ),
        }}
      >
        <Select value={arcaBucket} onChange={(e) => setArcaBucket(e.target.value as ArcaBucket | '')}>
          {ARCA_BUCKET_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>
      </Field>
    </EntityFormDialog>
  )
}
