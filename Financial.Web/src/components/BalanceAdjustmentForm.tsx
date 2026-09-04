import { Button, Field, InfoLabel, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import type { LabelProps } from '@fluentui/react-components'
import type { BankDto } from '../api/types'
import type { BalanceAdjustmentFormField } from '../hooks/mapBalanceAdjustmentErrorToField'
import { useFieldError } from '../hooks/useFieldError'
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
  saveErrorFields: Partial<Record<BalanceAdjustmentFormField, string>>
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
  saveErrorFields,
  savedDelta,
  onFieldChange,
  onSave,
  onCancel,
}: BalanceAdjustmentFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(saveErrorFields)

  if (savedDelta !== null) {
    const sign = savedDelta < 0 ? '-' : ''
    return (
      <div className={styles.panel} data-testid="balance-adjustment-form-panel">
        <Text as="h2" weight="semibold" size={400}>
          Balance Corrected
        </Text>
        <Text as="p">
          Adjustment of <Text weight="semibold">{sign}£{formatN2(Math.abs(savedDelta))}</Text> recorded
        </Text>
        <div className={styles.actions}>
          <Button appearance="primary" onClick={onCancel}>
            Close
          </Button>
        </div>
      </div>
    )
  }

  const generalError = Object.keys(saveErrorFields).length === 0 ? saveError : null

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
            required
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
            Current calculated balance for {bankDisplayName}:{' '}
            <Text size={200} weight="semibold">
              £{formatN2(currentBalance)}
            </Text>
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
              label={{
                children: (_: unknown, props: LabelProps) => (
                  <InfoLabel
                    {...props}
                    info="Enter the balance you want this bank to show after the adjustment — the app calculates and records the difference."
                  >
                    Target Balance
                  </InfoLabel>
                ),
              }}
              required
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
