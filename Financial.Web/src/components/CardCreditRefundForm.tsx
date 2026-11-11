import { useRef } from 'react'
import { Field, Input, Select, Text } from '@fluentui/react-components'
import type { CardCreditRefundField, CardCreditRefundForm as CardCreditRefundFormState } from '../hooks/useCardCreditRefundForm'
import { todayIsoDate } from '../utils/formatters'
import ActionErrorAlert from './ActionErrorAlert'
import ErrorState from './ErrorState'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'
import LockedValue from './LockedValue'

interface CardCreditRefundFormProps {
  refundForm: CardCreditRefundFormState
  onCancel: () => void
}

const MODE_TEXT = {
  card: {
    title: 'Refund credit to bank',
    loading: 'Loading banks…',
    placeholder: 'Select a bank',
    empty: (currency: string) => `No active bank in ${currency} can receive this refund.`,
  },
  bank: {
    title: 'Receive card credit',
    loading: 'Loading cards…',
    placeholder: 'Select a card',
    empty: (currency: string) => `No card in ${currency} has credit to refund.`,
  },
} as const

export default function CardCreditRefundForm({ refundForm, onCancel }: CardCreditRefundFormProps) {
  const styles = useFormPanelStyles()
  const counterpartRef = useRef<HTMLSelectElement | null>(null)
  const amountRef = useRef<HTMLInputElement | null>(null)
  const dateRef = useRef<HTMLInputElement | null>(null)
  const { mode, accountName, counterparts, currency, values, isLoading, loadError, retry, hasNoCounterparts, isSaving, saveError, fieldErrors, setField, submit } =
    refundForm
  const text = MODE_TEXT[mode]
  const fieldRefs: Record<CardCreditRefundField, { current: HTMLElement | null }> = { counterpartId: counterpartRef, amount: amountRef, date: dateRef }
  const validation = (field: CardCreditRefundField) => ({
    validationState: fieldErrors[field] ? ('error' as const) : ('none' as const),
    validationMessage: fieldErrors[field],
  })

  const lockedField = (label: string) => (
    <LockedValue label={label} value={accountName} />
  )
  const pickerField = (label: string) => (
    <Field label={label} required hint={hasNoCounterparts ? text.empty(currency) : undefined} {...validation('counterpartId')}>
      <Select
        ref={counterpartRef}
        value={values.counterpartId}
        aria-busy={isLoading}
        onChange={(event) => setField('counterpartId', event.target.value)}
      >
        <option value="">{isLoading ? text.loading : text.placeholder}</option>
        {counterparts.map((counterpart) => (
          <option key={counterpart.id} value={counterpart.id}>
            {counterpart.label}
          </option>
        ))}
      </Select>
    </Field>
  )
  const [fromField, toField] = mode === 'card' ? [lockedField('From'), pickerField('To bank')] : [pickerField('From'), lockedField('To bank')]

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
          {text.title}
        </Text>
        <ActionErrorAlert message={saveError} />
        {loadError && (
          <ErrorState
            message={loadError}
            onRetry={() => {
              retry()
              counterpartRef.current?.focus()
            }}
          />
        )}
        <div className={styles.grid}>
          {fromField}
          {toField}
          <Field label={`Amount (${currency})`} required {...validation('amount')}>
            <Input
              ref={amountRef}
              type="number"
              step="0.01"
              placeholder={mode === 'bank' && !values.counterpartId ? '—' : undefined}
              value={values.amount}
              onChange={(event) => setField('amount', event.target.value)}
            />
          </Field>
          <Field label="Date" required {...validation('date')}>
            <Input ref={dateRef} type="date" max={todayIsoDate()} value={values.date} onChange={(event) => setField('date', event.target.value)} />
          </Field>
        </div>
      </fieldset>
      <FormSaveActions
        saveLabel={text.title}
        isSaving={isSaving}
        isSaveDisabled={isLoading || loadError !== null || hasNoCounterparts}
        onCancel={onCancel}
      />
    </form>
  )
}
