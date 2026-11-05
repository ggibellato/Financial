import { useEffect, useRef } from 'react'
import { Button, Field, Input, MessageBar, MessageBarBody, Select, Spinner, Text } from '@fluentui/react-components'
import type { CardCreditRefundField, CardCreditRefundForm as CardCreditRefundFormState } from '../hooks/useCardCreditRefundForm'
import { todayIsoDate } from '../utils/formatters'
import ErrorState from './ErrorState'
import { useFormPanelStyles } from './formPanelStyles'

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
  const submitRef = useRef<HTMLButtonElement | null>(null)
  const { mode, accountName, counterparts, currency, values, isLoading, loadError, retry, hasNoCounterparts, isSaving, saveError, fieldErrors, setField, submit } =
    refundForm
  const text = MODE_TEXT[mode]
  const fieldRefs: Record<CardCreditRefundField, { current: HTMLElement | null }> = { counterpartId: counterpartRef, amount: amountRef, date: dateRef }
  const validation = (field: CardCreditRefundField) => ({
    validationState: fieldErrors[field] ? ('error' as const) : ('none' as const),
    validationMessage: fieldErrors[field],
  })

  useEffect(() => {
    if (saveError && !isSaving) submitRef.current?.focus()
  }, [saveError, isSaving])

  const lockedAccount = <Text weight="semibold">{accountName}</Text>
  const picker = (
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
  )
  const pickerField = (label: string) => (
    <Field label={label} required hint={hasNoCounterparts ? text.empty(currency) : undefined} {...validation('counterpartId')}>
      {picker}
    </Field>
  )

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
        {saveError && (
          <MessageBar role="alert" intent="error">
            <MessageBarBody>{saveError}</MessageBarBody>
          </MessageBar>
        )}
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
          {mode === 'card' ? <Field label="From">{lockedAccount}</Field> : pickerField('From')}
          {mode === 'card' ? pickerField('To bank') : <Field label="To bank">{lockedAccount}</Field>}
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
        <div className={styles.actions}>
          <Button
            ref={submitRef}
            type="submit"
            appearance="primary"
            disabled={isSaving || isLoading || loadError !== null || hasNoCounterparts}
            icon={isSaving ? <Spinner size="tiny" /> : undefined}
          >
            {isSaving ? 'Saving...' : text.title}
          </Button>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}
