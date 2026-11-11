import { useState } from 'react'
import { Field, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import type { LockedBank } from '../hooks/formSettings'
import type { TransferFormField } from '../hooks/transferFormField'
import { useFieldError } from '../hooks/useFieldError'
import { useFormErrorFocus } from '../hooks/useFormErrorFocus'
import { selectableBanks, type SelectableBank } from '../utils/selectableBanks'
import { ACTION_LABELS } from './accountActions'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'

const DIFFERENT_BANKS_ERROR = 'Source and destination must be different banks.'

export interface LockedTransferSide {
  side: 'source' | 'destination'
  name: string
}

interface TransferFormProps {
  isEditing: boolean
  date: string
  sourceBank: string
  destinationBank: string
  amount: string
  note: string
  banks: (LockedBank & SelectableBank)[]
  lockedSide?: LockedTransferSide
  destinationAmount?: string
  sourceCurrency?: string | null
  destinationCurrency?: string | null
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<TransferFormField, string>>
  onFieldChange: (field: TransferFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function TransferForm({
  isEditing,
  date,
  sourceBank,
  destinationBank,
  amount,
  note,
  banks,
  lockedSide,
  destinationAmount = '',
  sourceCurrency = null,
  destinationCurrency = null,
  isSaving,
  saveError,
  saveErrorFields,
  onFieldChange,
  onSave,
  onCancel,
}: TransferFormProps) {
  const styles = useFormPanelStyles()
  const title = isEditing ? 'Edit transfer' : ACTION_LABELS[lockedSide?.side === 'destination' ? 'TransferFrom' : 'TransferTo']
  const [openedSourceBank] = useState(sourceBank)
  const [openedDestinationBank] = useState(destinationBank)
  const sameBankError =
    sourceBank !== '' && destinationBank !== '' && sourceBank === destinationBank
      ? DIFFERENT_BANKS_ERROR
      : null

  const sourceBanks = selectableBanks(banks, sourceBank, openedSourceBank)
  const destinationBanks = selectableBanks(banks, destinationBank, openedDestinationBank).filter((b) => b.id !== sourceBank)
  const placeholder = lockedSide ? 'Select an account' : 'Select a bank'

  const baseFieldError = useFieldError(saveErrorFields)
  const fieldError = (field: TransferFormField): string | null => {
    if (field === 'destinationBank' && sameBankError) return sameBankError
    return baseFieldError(field)
  }

  const generalError = Object.keys(saveErrorFields).length === 0 ? saveError : null
  const { formRef, errorRef } = useFormErrorFocus(saveErrorFields, generalError)

  const accountField = (side: LockedTransferSide['side'], label: string) => {
    const field: TransferFormField = side === 'source' ? 'sourceBank' : 'destinationBank'
    if (lockedSide?.side === side) {
      return (
        <Field label={label}>
          <Text weight="semibold">{lockedSide.name}</Text>
        </Field>
      )
    }
    const options = side === 'source' ? sourceBanks : destinationBanks
    const value = side === 'source' ? sourceBank : destinationBank
    return (
      <Field
        label={label}
        required
        validationState={fieldError(field) ? 'error' : 'none'}
        validationMessage={fieldError(field)}
      >
        <Select value={value} onChange={(e) => onFieldChange(field, e.target.value)}>
          {(side === 'destination' || value === '') && <option value="">{placeholder}</option>}
          {options.map((b) => (
            <option key={b.id} value={b.id}>
              {b.name}
            </option>
          ))}
        </Select>
      </Field>
    )
  }

  return (
    <form
      ref={formRef}
      className={styles.panel}
      data-testid="transfer-form-panel"
      noValidate
      onSubmit={(event) => {
        event.preventDefault()
        onSave()
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset}>
        <Text as="h2" weight="semibold" size={400}>
          {title}
        </Text>
        {generalError && (
          <MessageBar ref={errorRef} role="alert" intent="error" tabIndex={-1}>
            <MessageBarBody>{generalError}</MessageBarBody>
          </MessageBar>
        )}

        <div className={styles.grid}>
          <Field
            label="Date"
            required
            validationState={fieldError('date') ? 'error' : 'none'}
            validationMessage={fieldError('date')}
          >
            <Input type="date" value={date} onChange={(e) => onFieldChange('date', e.target.value)} />
          </Field>

          {accountField('source', 'From')}
          {accountField('destination', 'To')}

          <Field
            label={destinationCurrency && sourceCurrency ? `Amount sent (${sourceCurrency})` : 'Amount'}
            required
            validationState={fieldError('amount') ? 'error' : 'none'}
            validationMessage={fieldError('amount')}
          >
            <Input type="number" step="0.01" value={amount} onChange={(e) => onFieldChange('amount', e.target.value)} />
          </Field>

          {destinationCurrency && (
            <Field
              label={`Amount received (${destinationCurrency})`}
              required
              validationState={fieldError('destinationAmount') ? 'error' : 'none'}
              validationMessage={fieldError('destinationAmount')}
            >
              <Input
                type="number"
                step="0.01"
                value={destinationAmount}
                onChange={(e) => onFieldChange('destinationAmount', e.target.value)}
              />
            </Field>
          )}

          <Field
            label="Note"
            className={styles.spanAll}
            validationState={fieldError('note') ? 'error' : 'none'}
            validationMessage={fieldError('note')}
          >
            <Input value={note} onChange={(e) => onFieldChange('note', e.target.value)} />
          </Field>
        </div>
      </fieldset>
      <FormSaveActions
        saveLabel={isEditing ? 'Save' : 'Transfer'}
        isSaving={isSaving}
        isSaveDisabled={sameBankError !== null}
        onCancel={onCancel}
      />
    </form>
  )
}
