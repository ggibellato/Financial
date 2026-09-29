import { Button, Field, InfoLabel, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import type { LabelProps } from '@fluentui/react-components'
import type { BankDto, CategoryDto, ReserveBucketDto } from '../api/types'
import type { WithdrawalFormField } from '../hooks/useReserva'
import { useFieldError } from '../hooks/useFieldError'
import { useFormPanelStyles } from './formPanelStyles'

interface WithdrawalFormProps {
  bucketId: string
  amount: string
  date: string
  description: string
  bankId: string
  expenseCategoryId: string
  buckets: ReserveBucketDto[]
  banks: BankDto[]
  categories: CategoryDto[]
  isSubmitting: boolean
  error: string | null
  errorFields: Partial<Record<WithdrawalFormField, string>>
  onFieldChange: (field: WithdrawalFormField, value: string) => void
  onSubmit: () => void
  onCancel: () => void
}

export default function WithdrawalForm({
  bucketId,
  amount,
  date,
  description,
  bankId,
  expenseCategoryId,
  buckets,
  banks,
  categories,
  isSubmitting,
  error,
  errorFields,
  onFieldChange,
  onSubmit,
  onCancel,
}: WithdrawalFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(errorFields)
  const generalError = Object.keys(errorFields).length === 0 ? error : null

  return (
    <div className={styles.panel} data-testid="withdrawal-form-panel">
      <Text as="h2" weight="semibold" size={400}>
        New Withdrawal
      </Text>

      <div className={styles.grid}>
        <Field
          label="Date"
          required
          validationState={fieldError('withdrawalDate') ? 'error' : 'none'}
          validationMessage={fieldError('withdrawalDate')}
        >
          <Input
            type="date"
            value={date}
            disabled={isSubmitting}
            onChange={(e) => onFieldChange('withdrawalDate', e.target.value)}
          />
        </Field>

        <Field
          label="Bucket"
          required
          validationState={fieldError('withdrawalBucketId') ? 'error' : 'none'}
          validationMessage={fieldError('withdrawalBucketId')}
        >
          <Select
            value={bucketId}
            disabled={isSubmitting}
            onChange={(e) => onFieldChange('withdrawalBucketId', e.target.value)}
          >
            {buckets.map((bucket) => (
              <option key={bucket.id} value={bucket.id}>
                {bucket.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field
          label={{
            children: (_: unknown, props: LabelProps) => (
              <InfoLabel
                {...props}
                info="Choose a bank when the money passes through it. The bank records a Reserva return and an expense in the category you pick."
              >
                Through bank
              </InfoLabel>
            ),
          }}
        >
          <Select
            value={bankId}
            disabled={isSubmitting}
            onChange={(e) => onFieldChange('withdrawalBankId', e.target.value)}
          >
            <option value="">No bank (direct)</option>
            {banks.map((bank) => (
              <option key={bank.id} value={bank.id}>
                {bank.name}
              </option>
            ))}
          </Select>
        </Field>

        {bankId !== '' && (
          <Field
            label="Expense category"
            required
            validationState={fieldError('withdrawalExpenseCategoryId') ? 'error' : 'none'}
            validationMessage={fieldError('withdrawalExpenseCategoryId')}
          >
            <Select
              value={expenseCategoryId}
              disabled={isSubmitting}
              onChange={(e) => onFieldChange('withdrawalExpenseCategoryId', e.target.value)}
            >
              <option value="">Select a category</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </Select>
          </Field>
        )}

        <Field
          label="Description"
          required
          validationState={fieldError('withdrawalDescription') ? 'error' : 'none'}
          validationMessage={fieldError('withdrawalDescription')}
        >
          <Input
            value={description}
            disabled={isSubmitting}
            onChange={(e) => onFieldChange('withdrawalDescription', e.target.value)}
          />
        </Field>

        <Field
          label="Amount"
          required
          validationState={fieldError('withdrawalAmount') ? 'error' : 'none'}
          validationMessage={fieldError('withdrawalAmount')}
        >
          <Input
            type="number"
            step="0.01"
            min="0"
            value={amount}
            disabled={isSubmitting}
            onChange={(e) => onFieldChange('withdrawalAmount', e.target.value)}
          />
        </Field>
      </div>

      <div className={styles.actions}>
        <Button appearance="primary" disabled={isSubmitting} onClick={onSubmit}>
          {isSubmitting ? 'Saving...' : 'Add Withdrawal'}
        </Button>
        <Button appearance="secondary" disabled={isSubmitting} onClick={onCancel}>
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
