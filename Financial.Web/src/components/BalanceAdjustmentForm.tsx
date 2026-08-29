import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import type { BankDto } from '../api/types'
import type { BalanceAdjustmentFormField } from '../hooks/mapBalanceAdjustmentErrorToField'
import { formatN2 } from '../utils/formatters'
import { useFormPanelStyles } from './formPanelStyles'

interface BalanceAdjustmentFormProps {
  isEditing: boolean
  bankName: string
  bankDisplayName: string
  banks: BankDto[]
  currentBalance: number
  date: string
  targetBalance: string
  note: string
  isSaving: boolean
  saveError: string | null
  saveErrorField: BalanceAdjustmentFormField | null
  savedDelta: number | null
  onFieldChange: (field: BalanceAdjustmentFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function BalanceAdjustmentForm({
  isEditing,
  bankName,
  bankDisplayName,
  banks,
  currentBalance,
  date,
  targetBalance,
  note,
  isSaving,
  saveError,
  saveErrorField,
  savedDelta,
  onFieldChange,
  onSave,
  onCancel,
}: BalanceAdjustmentFormProps) {
  const styles = useFormPanelStyles()

  if (savedDelta !== null) {
    const sign = savedDelta < 0 ? '-' : ''
    return (
      <div className={styles.panel} data-testid="balance-adjustment-form-panel">
        <Text as="h2" weight="semibold" size={400}>
          Balance Corrected
        </Text>
        <Text as="p">
          Adjustment of {sign}£{formatN2(Math.abs(savedDelta))} recorded
        </Text>
        <div className={styles.actions}>
          <Button appearance="primary" onClick={onCancel}>
            Close
          </Button>
        </div>
      </div>
    )
  }

  const fieldError = (field: BalanceAdjustmentFormField): string | null =>
    saveErrorField === field ? saveError : null

  const generalError = saveErrorField === null ? saveError : null

  const bankChosen = bankName !== ''
  const saveDisabled = isSaving || (!isEditing && !bankChosen)

  return (
    <div className={styles.panel} data-testid="balance-adjustment-form-panel">
      <Text as="h2" weight="semibold" size={400}>
        {isEditing ? 'Edit Balance Correction' : 'New Balance Correction'}
      </Text>

      {!isEditing && (
        <div className={styles.grid}>
          <Field
            label="Bank"
            validationState={fieldError('bankName') ? 'error' : 'none'}
            validationMessage={fieldError('bankName')}
          >
            <Select value={bankName} onChange={(e) => onFieldChange('bankName', e.target.value)}>
              <option value="">Select a bank</option>
              {banks.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.name}
                </option>
              ))}
            </Select>
          </Field>
        </div>
      )}

      {bankChosen && (
        <>
          <Text as="p" size={200}>
            Current calculated balance for {bankDisplayName}: £{formatN2(currentBalance)}
          </Text>
          <div className={styles.grid}>
            <Field
              label="Date"
              validationState={fieldError('date') ? 'error' : 'none'}
              validationMessage={fieldError('date')}
            >
              <Input type="date" value={date} onChange={(e) => onFieldChange('date', e.target.value)} />
            </Field>

            <Field
              label="Target Balance"
              validationState={fieldError('targetBalance') ? 'error' : 'none'}
              validationMessage={fieldError('targetBalance')}
            >
              <Input
                type="number"
                step="0.01"
                value={targetBalance}
                onChange={(e) => onFieldChange('targetBalance', e.target.value)}
              />
            </Field>

            <Field label="Note">
              <Input value={note} onChange={(e) => onFieldChange('note', e.target.value)} />
            </Field>
          </div>
        </>
      )}

      <div className={styles.actions}>
        <Button appearance="primary" disabled={saveDisabled} onClick={onSave}>
          {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Add Balance Correction'}
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
