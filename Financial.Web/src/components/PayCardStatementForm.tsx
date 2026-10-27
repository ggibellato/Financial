import { useRef } from 'react'
import { Button, Field, Input, MessageBar, MessageBarBody, Select, Spinner, Text } from '@fluentui/react-components'
import type { LockedBank } from '../hooks/formSettings'
import type { PayCardStatementField, PayCardStatementForm as PayCardStatementFormState } from '../hooks/usePayCardStatementForm'
import { formatCurrency, formatLongMonthYear, todayIsoDate } from '../utils/formatters'
import ErrorState from './ErrorState'
import { useFormPanelStyles } from './formPanelStyles'

const NOTHING_TO_PAY_MESSAGE = 'No card statement is waiting to be paid.'

interface PayCardStatementFormProps {
  payForm: PayCardStatementFormState
  lockedBank: LockedBank
  onCancel: () => void
}

export default function PayCardStatementForm({ payForm, lockedBank, onCancel }: PayCardStatementFormProps) {
  const styles = useFormPanelStyles()
  const cardRef = useRef<HTMLSelectElement | null>(null)
  const statementRef = useRef<HTMLSelectElement | null>(null)
  const dateRef = useRef<HTMLInputElement | null>(null)
  const { cards, statements, statement, values, isLoading, loadError, retry, hasNothingToPay, isSaving, saveError, fieldErrors, setField, submit } =
    payForm
  const fieldRefs: Record<PayCardStatementField, { current: HTMLElement | null }> = { cardId: cardRef, statementId: statementRef, date: dateRef }
  const validation = (field: PayCardStatementField) => ({
    validationState: fieldErrors[field] ? ('error' as const) : ('none' as const),
    validationMessage: fieldErrors[field],
  })

  return (
    <form
      className={styles.panel}
      noValidate
      aria-labelledby="pay-card-statement-title"
      onSubmit={(event) => {
        event.preventDefault()
        const firstInvalid = submit()
        if (firstInvalid) fieldRefs[firstInvalid].current?.focus()
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset}>
        <Text as="h2" id="pay-card-statement-title" weight="semibold" size={400}>
          Pay card statement
        </Text>
        {saveError && (
          <MessageBar intent="error">
            <MessageBarBody>{saveError}</MessageBarBody>
          </MessageBar>
        )}
        {loadError && <ErrorState message={loadError} onRetry={retry} />}
        <div className={styles.grid}>
          <Field label="From">
            <Text weight="semibold">{lockedBank.name}</Text>
          </Field>
          <Field label="Card" required hint={hasNothingToPay ? NOTHING_TO_PAY_MESSAGE : undefined} {...validation('cardId')}>
            <Select ref={cardRef} value={values.cardId} aria-busy={isLoading} onChange={(event) => setField('cardId', event.target.value)}>
              <option value="">{isLoading ? 'Loading statements…' : 'Select a card'}</option>
              {cards.map((card) => (
                <option key={card.id} value={card.id}>
                  {card.name}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Statement" required {...validation('statementId')}>
            <Select
              ref={statementRef}
              value={values.statementId}
              aria-busy={isLoading}
              onChange={(event) => setField('statementId', event.target.value)}
            >
              <option value="">{values.cardId ? 'Select a statement' : 'Select a card first'}</option>
              {statements.map((candidate) => (
                <option key={candidate.id} value={candidate.id}>
                  {`${formatLongMonthYear(candidate.year, candidate.month)} · ${formatCurrency(candidate.total, candidate.currency)}`}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Date" required {...validation('date')}>
            <Input
              ref={dateRef}
              type="date"
              max={todayIsoDate()}
              value={values.date}
              onChange={(event) => setField('date', event.target.value)}
            />
          </Field>
          <Field label="Amount">
            <Input readOnly value={statement ? formatCurrency(statement.total, statement.currency) : '—'} />
          </Field>
        </div>
        <div className={styles.actions}>
          <Button
            type="submit"
            appearance="primary"
            disabled={isSaving || hasNothingToPay}
            icon={isSaving ? <Spinner size="tiny" /> : undefined}
          >
            {isSaving ? 'Saving...' : 'Pay statement'}
          </Button>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}
