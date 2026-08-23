import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text, makeStyles, tokens } from '@fluentui/react-components'
import type { BankDto } from '../api/types'
import type { TransferFormField } from '../hooks/mapTransferErrorToField'

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
  saveErrorField: TransferFormField | null
  onFieldChange: (field: TransferFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

const useStyles = makeStyles({
  panel: {
    flexShrink: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    border: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(4, minmax(0, 1fr))',
    gap: tokens.spacingVerticalM,
    '@media (max-width: 1023px)': {
      gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
    },
    '@media (max-width: 639px)': {
      gridTemplateColumns: '1fr',
    },
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
})

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
  saveErrorField,
  onFieldChange,
  onSave,
  onCancel,
}: TransferFormProps) {
  const styles = useStyles()
  const sameBankError =
    sourceBank !== '' && destinationBank !== '' && sourceBank === destinationBank
      ? 'Source and destination must be different banks.'
      : null

  const destinationBanks = banks.filter((b) => b.id !== sourceBank)

  const fieldError = (field: TransferFormField): string | null => {
    if (field === 'destinationBank' && sameBankError) return sameBankError
    return saveErrorField === field ? saveError : null
  }

  const generalError = saveErrorField === null ? saveError : null

  return (
    <div className={styles.panel} data-testid="transfer-form-panel">
      <Text as="h2" weight="semibold" size={400}>
        {isEditing ? 'Edit Transfer' : 'Move Money'}
      </Text>

      <div className={styles.grid}>
        <Field label="Date" validationState={fieldError('date') ? 'error' : 'none'} validationMessage={fieldError('date')}>
          <Input type="date" value={date} onChange={(e) => onFieldChange('date', e.target.value)} />
        </Field>

        <Field
          label="From"
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
          {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Move Money'}
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
