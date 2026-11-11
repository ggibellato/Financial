import { Button, Field, InfoLabel, Input, Select, Text } from '@fluentui/react-components'
import type { LabelProps } from '@fluentui/react-components'
import type { BankOption } from '../api/types'
import type { BalanceAdjustmentFormField } from '../hooks/mapBalanceAdjustmentErrorToField'
import { useFieldError } from '../hooks/useFieldError'
import { useFormErrorFocus } from '../hooks/useFormErrorFocus'
import { formatCurrency } from '../utils/formatters'
import { selectableBanks } from '../utils/selectableBanks'
import ActionErrorAlert from './ActionErrorAlert'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'
import LockedValue from './LockedValue'
import { generalErrorOf } from '../utils/fieldErrors'

interface BalanceAdjustmentFormProps {
  isEditing: boolean
  bankName: string
  bankDisplayName: string
  banks: BankOption[]
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
  const generalError = generalErrorOf(saveError, saveErrorFields)
  const formRef = useFormErrorFocus(saveErrorFields)

  if (savedDelta !== null) {
    return (
      <div className={styles.panel} data-testid="balance-adjustment-form-panel">
        <Text as="h2" weight="semibold" size={400}>
          Balance adjusted
        </Text>
        <Text as="p">
          Adjustment of <Text weight="semibold">{formatCurrency(savedDelta, currency)}</Text> recorded.
        </Text>
        <div className={styles.actions}>
          <Button appearance="primary" onClick={onCancel}>
            Close
          </Button>
        </div>
      </div>
    )
  }

  const bankChosen = bankName !== ''

  return (
    <form
      ref={formRef}
      className={styles.panel}
      data-testid="balance-adjustment-form-panel"
      noValidate
      onSubmit={(event) => {
        event.preventDefault()
        onSave()
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset} role="presentation">
        <Text as="h2" weight="semibold" size={400}>
          {isEditing ? 'Edit balance adjustment' : 'Adjust balance'}
        </Text>
        <ActionErrorAlert message={generalError} />

        {isBankLocked ? (
          <div className={styles.grid}>
            <LockedValue label="Bank" value={bankDisplayName} />
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
                      Target balance
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
      </fieldset>
      <FormSaveActions
        saveLabel={isEditing ? 'Save' : 'Adjust balance'}
        isSaving={isSaving}
        isSaveDisabled={!isEditing && !bankChosen}
        onCancel={onCancel}
      />
    </form>
  )
}
