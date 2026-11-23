import { useId, useRef } from 'react'
import { Button, Field, Input, Select, Switch, Text } from '@fluentui/react-components'
import { AddRegular, DeleteRegular } from '@fluentui/react-icons'
import {
  MAX_PAYMENT_DAY,
  MIN_PAYMENT_DAY,
  newRateRow,
  type InterestFrequency,
  type RateRow,
  type SavingsErrors,
  type SavingsFormValues,
} from './savingsTermsForm'
import './SavingsTermsFields.css'

interface SavingsTermsFieldsProps {
  values: SavingsFormValues
  errors: SavingsErrors | null
  onChange: (values: SavingsFormValues) => void
}

export default function SavingsTermsFields({ values, errors, onChange }: SavingsTermsFieldsProps) {
  const ratesLabelId = useId()
  const addRateButton = useRef<HTMLButtonElement | null>(null)
  const canRemoveRate = values.rates.length > 1

  const change = (next: Partial<SavingsFormValues>) => onChange({ ...values, ...next })

  const changeFrequency = (interestFrequency: InterestFrequency) =>
    change({ interestFrequency, paymentDay: interestFrequency === 'Daily' ? '' : values.paymentDay })

  const changeRate = (key: string, next: Partial<RateRow>) =>
    change({ rates: values.rates.map((rate) => (rate.key === key ? { ...rate, ...next } : rate)) })

  const removeRate = (key: string) => {
    change({ rates: values.rates.filter((rate) => rate.key !== key) })
    addRateButton.current?.focus()
  }

  const validation = (message: string | undefined) => ({
    validationState: message ? ('error' as const) : ('none' as const),
    validationMessage: message,
  })

  return (
    <>
      <Field label="Interest paid" required>
        <Select
          value={values.interestFrequency}
          onChange={(_, data) => changeFrequency(data.value as InterestFrequency)}
        >
          <option value="Monthly">Monthly</option>
          <option value="Daily">Daily</option>
        </Select>
      </Field>

      {values.interestFrequency === 'Monthly' && (
        <Field
          label="Payment day"
          required
          hint="In shorter months, interest is paid on the month's last day."
          {...validation(errors?.paymentDay)}
        >
          <Input
            className="savings-terms__day"
            type="number"
            min={MIN_PAYMENT_DAY}
            max={MAX_PAYMENT_DAY}
            step={1}
            inputMode="numeric"
            value={values.paymentDay}
            onChange={(_, data) => change({ paymentDay: data.value })}
          />
        </Field>
      )}

      <Field label="Tax-free">
        <Switch checked={values.isTaxFree} onChange={(_, data) => change({ isTaxFree: data.checked })} />
      </Field>

      <div className="savings-terms__rates" role="group" aria-labelledby={ratesLabelId}>
        <Text id={ratesLabelId} weight="semibold">
          Rates
        </Text>
        <Text size={200}>The rate in force on a date is the latest one that took effect on or before it.</Text>
        {values.rates.map((rate, index) => {
          const rateErrors = errors?.rates[index]
          const rateName = `Rate ${index + 1}`
          return (
            <div key={rate.key} className="savings-terms__rate" role="group" aria-label={rateName}>
              <Field label="Annual rate (%)" required {...validation(rateErrors?.annualPercent)}>
                <Input
                  className="savings-terms__percent"
                  type="number"
                  min={0}
                  max={100}
                  step="0.01"
                  inputMode="decimal"
                  value={rate.annualPercent}
                  onChange={(_, data) => changeRate(rate.key, { annualPercent: data.value })}
                />
              </Field>
              <Field label="Effective from" required {...validation(rateErrors?.effectiveFrom)}>
                <Input
                  type="date"
                  value={rate.effectiveFrom}
                  onChange={(_, data) => changeRate(rate.key, { effectiveFrom: data.value })}
                />
              </Field>
              <Button
                className="savings-terms__remove"
                appearance="subtle"
                icon={<DeleteRegular />}
                aria-label={`Remove ${rateName.toLowerCase()}`}
                disabled={!canRemoveRate}
                onClick={() => removeRate(rate.key)}
              />
            </div>
          )
        })}
        <div>
          <Button
            ref={addRateButton}
            appearance="primary"
            icon={<AddRegular />}
            onClick={() => change({ rates: [...values.rates, newRateRow()] })}
          >
            Add rate
          </Button>
        </div>
      </div>
    </>
  )
}
