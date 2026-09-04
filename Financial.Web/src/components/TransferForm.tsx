import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import type { BankDto } from '../api/types'
import type { TransferFormField } from '../hooks/mapTransferErrorToField'
import { useFieldError } from '../hooks/useFieldError'
import { useFormPanelStyles } from './formPanelStyles'

interface TransferFormProps {
  isEditing: boolean
  date: string
  sourceBank: string
  destinationBank: string
  amount: string
  note: string
  banks: BankDto[]
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
  isSaving,
  saveError,
  saveErrorFields,
  onFieldChange,
  onSave,
  onCancel,
}: TransferFormProps) {
  const styles = useFormPanelStyles()
  const sameBankError =
    sourceBank !== '' && destinationBank !== '' && sourceBank === destinationBank
      ? 'Source and destination must be different banks.'
      : null

  const destinationBanks = banks.filter((b) => b.id !== sourceBank)

  const baseFieldError = useFieldError(saveErrorFields)
  const fieldError = (field: TransferFormField): string | null => {
    if (field === 'destinationBank' && sameBankError) return sameBankError
    return baseFieldError(field)
  }

  const generalError = Object.keys(saveErrorFields).length === 0 ? saveError : null

  return (
    <div className={styles.panel} data-testid="transfer-form-panel">
      <Text as="h2" weight="semibold" size={400}>
        {isEditing ? 'Edit Transfer' : 'New Transfer'}
      </Text>

      <div className={styles.grid}>
        <Field
          label="Date"
          required
          validationState={fieldError('date') ? 'error' : 'none'}
          validationMessage={fieldError('date')}
        >
          <Input type="date" value={date} onChange={(e) => onFieldChange('date', e.target.value)} />
        </Field>

        <Field
          label="From"
          required
          validationState={fieldError('sourceBank') ? 'error' : 'none'}
          validationMessage={fieldError('sourceBank')}
        >
          <Select value={sourceBank} onChange={(e) => onFieldChange('sourceBank', e.target.value)}>
            {banks.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field
          label="To"
          required
          validationState={fieldError('destinationBank') ? 'error' : 'none'}
          validationMessage={fieldError('destinationBank')}
        >
          <Select value={destinationBank} onChange={(e) => onFieldChange('destinationBank', e.target.value)}>
            <option value="">Select a bank</option>
            {destinationBanks.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field
          label="Amount"
          required
          validationState={fieldError('amount') ? 'error' : 'none'}
          validationMessage={fieldError('amount')}
        >
          <Input type="number" step="0.01" value={amount} onChange={(e) => onFieldChange('amount', e.target.value)} />
        </Field>

        <Field label="Note">
          <Input value={note} onChange={(e) => onFieldChange('note', e.target.value)} />
        </Field>
      </div>

      <div className={styles.actions}>
        <Button appearance="primary" disabled={isSaving || sameBankError !== null} onClick={onSave}>
          {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Add Transfer'}
        </Button>
        <Button appearance="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </div>

      {generalError && (
        <MessageBar intent="error">
          <MessageBarBody>{generalError}</MessageBarBody>
        </MessageBar>
      )}
    </div>
  )
}
