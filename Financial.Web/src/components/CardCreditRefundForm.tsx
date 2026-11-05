import { useRef } from 'react'
import { Button, Field, Input, MessageBar, MessageBarBody, Select, Spinner, Text } from '@fluentui/react-components'
import type { LockedCard } from '../hooks/formSettings'
import type { CardCreditRefundField, CardCreditRefundForm as CardCreditRefundFormState } from '../hooks/useCardCreditRefundForm'
import { todayIsoDate } from '../utils/formatters'
import ErrorState from './ErrorState'
import { useFormPanelStyles } from './formPanelStyles'

interface CardCreditRefundFormProps {
  refundForm: CardCreditRefundFormState
  lockedCard: LockedCard
  onCancel: () => void
}

export default function CardCreditRefundForm({ refundForm, lockedCard, onCancel }: CardCreditRefundFormProps) {
  const styles = useFormPanelStyles()
  const bankRef = useRef<HTMLSelectElement | null>(null)
  const amountRef = useRef<HTMLInputElement | null>(null)
  const dateRef = useRef<HTMLInputElement | null>(null)
  const { banks, currency, values, isLoading, loadError, retry, hasNoBanks, isSaving, saveError, fieldErrors, setField, submit } = refundForm
  const fieldRefs: Record<CardCreditRefundField, { current: HTMLElement | null }> = { destinationAccountId: bankRef, amount: amountRef, date: dateRef }
  const validation = (field: CardCreditRefundField) => ({
    validationState: fieldErrors[field] ? ('error' as const) : ('none' as const),
    validationMessage: fieldErrors[field],
  })

  return (
    <form
      className={styles.panel}
      noValidate
      aria-labelledby="card-credit-refund-title"
      onSubmit={(event) => {
        event.preventDefault()
        const firstInvalid = submit()
        if (firstInvalid) fieldRefs[firstInvalid].current?.focus()
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset}>
        <Text as="h2" id="card-credit-refund-title" weight="semibold" size={400}>
          Refund credit to bank
        </Text>
        {saveError && (
          <MessageBar intent="error">
            <MessageBarBody>{saveError}</MessageBarBody>
          </MessageBar>
        )}
        {loadError && <ErrorState message={loadError} onRetry={retry} />}
        <div className={styles.grid}>
          <Field label="From">
            <Text weight="semibold">{lockedCard.name}</Text>
          </Field>
          <Field
            label="To bank"
            required
            hint={hasNoBanks ? `No active bank in ${currency} can receive this refund.` : undefined}
            {...validation('destinationAccountId')}
          >
            <Select
              ref={bankRef}
              value={values.destinationAccountId}
              aria-busy={isLoading}
              onChange={(event) => setField('destinationAccountId', event.target.value)}
            >
              <option value="">{isLoading ? 'Loading banks…' : 'Select a bank'}</option>
              {banks.map((bank) => (
                <option key={bank.id} value={bank.id}>
                  {bank.name}
                </option>
              ))}
            </Select>
          </Field>
          <Field label={`Amount (${currency})`} required {...validation('amount')}>
            <Input ref={amountRef} type="number" step="0.01" value={values.amount} onChange={(event) => setField('amount', event.target.value)} />
          </Field>
          <Field label="Date" required {...validation('date')}>
            <Input ref={dateRef} type="date" max={todayIsoDate()} value={values.date} onChange={(event) => setField('date', event.target.value)} />
          </Field>
        </div>
        <div className={styles.actions}>
          <Button
            type="submit"
            appearance="primary"
            disabled={isSaving || isLoading || loadError !== null || hasNoBanks}
            icon={isSaving ? <Spinner size="tiny" /> : undefined}
          >
            {isSaving ? 'Saving...' : 'Refund credit to bank'}
          </Button>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}
