import { Button, Field, InfoLabel, Input, MessageBar, MessageBarBody, Select, Spinner, Text } from '@fluentui/react-components'
import type { LabelProps } from '@fluentui/react-components'
import type { BankDto } from '../api/types'
import type { BalanceAdjustmentFormField } from '../hooks/mapBalanceAdjustmentErrorToField'
import { useFieldError } from '../hooks/useFieldError'
import { formatCurrency } from '../utils/formatters'
import { selectableBanks } from '../utils/selectableBanks'
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
  isBankLocked?: boolean
  currency?: string
  balanceLabel?: string
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
  isBankLocked = false,
  currency = 'GBP',
  balanceLabel,
  onFieldChange,
  onSave,
  onCancel,
}: BalanceAdjustmentFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(saveErrorFields)

  if (savedDelta !== null) {
    return (
      <div className={styles.panel} data-testid="balance-adjustment-form-panel">
        <Text as="h2" weight="semibold" size={400}>
          Balance adjusted
        </Text>
        <Text as="p">
          Adjustment of <Text weight="semibold">{formatCurrency(savedDelta, currency)}</Text> recorded
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
    <form
      className={styles.panel}
      data-testid="balance-adjustment-form-panel"
      noValidate
      onSubmit={(event) => {
        event.preventDefault()
        onSave()
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset}>
        <Text as="h2" weight="semibold" size={400}>
          {isEditing ? 'Edit balance adjustment' : 'Adjust balance'}
        </Text>
        {generalError && (
          <MessageBar intent="error">
            <MessageBarBody>{generalError}</MessageBarBody>
          </MessageBar>
        )}

        {isBankLocked ? (
          <div className={styles.grid}>
            <Field label="Bank">
              <Text weight="semibold">{bankDisplayName}</Text>
            </Field>
          </div>
        ) : !isEditing && (
          <div className={styles.grid}>
            <Field
              label="Bank"
              required
              validationState={fieldError('bankName') ? 'error' : 'none'}
              validationMessage={fieldError('bankName')}
            >
              <Select value={bankName} onChange={(e) => onFieldChange('bankName', e.target.value)}>
                <option value="">Select a bank</option>
                {selectableBanks(banks, bankName).map((b) => (
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
              {balanceLabel ?? `Current calculated balance for ${bankDisplayName}`}:{' '}
              <Text size={200} weight="semibold">
                {formatCurrency(currentBalance, currency)}
              </Text>
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

              <Field label="Note" className={styles.spanAll}>
                <Input value={note} onChange={(e) => onFieldChange('note', e.target.value)} />
              </Field>
            </div>
          </>
        )}

        <div className={styles.actions}>
          <Button type="submit" appearance="primary" disabled={saveDisabled} icon={isSaving ? <Spinner size="tiny" /> : undefined}>
            {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Adjust balance'}
          </Button>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}
