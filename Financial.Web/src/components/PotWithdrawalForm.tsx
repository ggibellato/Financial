import { Button, Field, InfoLabel, Input, MessageBar, MessageBarBody, Radio, RadioGroup, Select, Text } from '@fluentui/react-components'
import type { LabelProps } from '@fluentui/react-components'
import type { BankOption } from '../api/types'
import type { LockedBank, LockedCard } from '../hooks/formSettings'
import { STATEMENTS_LOAD_ERROR } from '../hooks/useCardStatements'
import { useFieldError } from '../hooks/useFieldError'
import { useFormErrorFocus } from '../hooks/useFormErrorFocus'
import type { PaidWith, PotWithdrawalField, PotWithdrawalFormModel } from '../hooks/usePotWithdrawalForm'
import { POTS_LOAD_ERROR } from '../hooks/usePots'
import { formatCurrency, formatDayMonthYearUtc, formatStatementMonthLabel } from '../utils/formatters'
import { inactiveMarked, selectableBanks } from '../utils/selectableBanks'
import ActionErrorAlert from './ActionErrorAlert'
import ConfirmDialog from './ConfirmDialog'
import ErrorState from './ErrorState'
import { useFormPanelStyles } from './formPanelStyles'
import PotField from './PotField'
import PotFormFooter from './PotFormFooter'
import LockedValue from './LockedValue'
import ReadOnlyInput from './ReadOnlyInput'

const PAID_WITH_LABELS: Record<PaidWith, string> = { none: 'None', bank: 'Bank', card: 'Credit card' }
const NO_ACTIVE_CARD = 'No active credit card.'
const PAID_STATEMENT_LOCK = 'Its card statement is paid. Unmark the statement to change it.'
const PAID_STATEMENT_DELETE = 'Unmark the statement to delete this withdrawal.'
const BANK_INFO =
  'Choose a bank when the money passes through it. The bank records a return from the Pots account and an expense in the category you pick, so it nets to zero.'

type Validation = (field: PotWithdrawalField) => { validationState: 'error' | 'none'; validationMessage: string | undefined }

interface PotWithdrawalFormProps {
  form: PotWithdrawalFormModel
  banks: BankOption[]
  lockedBank?: LockedBank
  lockedCard?: LockedCard
  onDelete: () => void
  onCancel: () => void
}

interface PaymentFieldProps {
  form: PotWithdrawalFormModel
  validation: Validation
}

function formTitle(form: PotWithdrawalFormModel): string {
  if (form.isEditing) return 'Edit withdrawal'
  return form.isPaymentLocked ? 'Withdraw from pots' : 'New withdrawal'
}

function saveLabel(form: PotWithdrawalFormModel): string {
  if (form.isEditing) return 'Save'
  return form.isPaymentLocked ? 'Withdraw from pots' : 'Add withdrawal'
}

function investmentWithdrawalText(form: PotWithdrawalFormModel, banks: BankOption[], statementMonth: string): string | null {
  const withdrawal = form.investmentWithdrawal
  if (!withdrawal) return null
  if (!form.isStatementPaidLocked) return `Investment withdrawal: Pending — taken when the ${formatStatementMonthLabel(statementMonth)} statement is paid`
  const bank = banks.find((candidate) => candidate.id === withdrawal.bankId)?.name
  const date = withdrawal.date ? formatDayMonthYearUtc(withdrawal.date) : null
  const details = [bank, date].filter(Boolean).join(', ')
  return details ? `Investment withdrawal: Done — ${details}` : 'Investment withdrawal: Done'
}

function ReadOnlyField({ label, value }: { label: string; value: string }) {
  return (
    <Field label={label}>
      <ReadOnlyInput value={value} />
    </Field>
  )
}

function PaidWithField({ form }: { form: PotWithdrawalFormModel }) {
  const styles = useFormPanelStyles()
  if (form.isStatementPaidLocked) return <ReadOnlyField label="Paid with" value={PAID_WITH_LABELS[form.paidWith]} />
  if (form.isPaymentLocked) return null
  const hasCards = form.cards.length > 0
  return (
    <Field className={styles.spanTwo} label="Paid with" hint={hasCards ? undefined : NO_ACTIVE_CARD}>
      <RadioGroup className={styles.radioRow} layout="horizontal" value={form.paidWith} onChange={(_, data) => form.setField('paidWith', data.value)}>
        <Radio value="none" label={PAID_WITH_LABELS.none} />
        <Radio value="bank" label={PAID_WITH_LABELS.bank} />
        <Radio value="card" label={PAID_WITH_LABELS.card} disabled={!hasCards && form.paidWith !== 'card'} />
      </RadioGroup>
    </Field>
  )
}

function BankField({ form, validation, banks, lockedBank, bankId }: PaymentFieldProps & { banks: BankOption[]; lockedBank?: LockedBank; bankId: string }) {
  if (form.isPaymentLocked && lockedBank) return <LockedValue label="Bank" value={lockedBank.name} />
  return (
    <Field
      label={{
        children: (_: unknown, props: LabelProps) => (
          <InfoLabel {...props} info={BANK_INFO}>
            Bank
          </InfoLabel>
        ),
      }}
      required
      {...validation('bankId')}
    >
      <Select value={bankId} onChange={(event) => form.setField('bankId', event.target.value)}>
        <option value="">Select a bank</option>
        {selectableBanks(banks, bankId, form.openedBankId).map((bank) => (
          <option key={bank.id} value={bank.id}>
            {inactiveMarked(bank)}
          </option>
        ))}
      </Select>
    </Field>
  )
}

function CardFields({ form, validation, lockedCard, creditCardId, statementMonth }: PaymentFieldProps & { lockedCard?: LockedCard; creditCardId: string; statementMonth: string }) {
  const styles = useFormPanelStyles()
  const cardName = lockedCard?.name ?? form.cards.find((card) => card.id === creditCardId)?.name ?? ''
  if (form.isStatementPaidLocked) {
    return (
      <>
        <ReadOnlyField label="Card" value={cardName} />
        <ReadOnlyField label="Statement" value={formatStatementMonthLabel(statementMonth)} />
      </>
    )
  }
  return (
    <>
      {form.isPaymentLocked ? (
        <LockedValue label="Card" value={cardName} />
      ) : (
        <Field label="Card" required {...validation('creditCardId')}>
          <Select value={creditCardId} onChange={(event) => form.setField('creditCardId', event.target.value)}>
            <option value="">Select a card</option>
            {form.cards.map((card) => (
              <option key={card.id} value={card.id}>
                {inactiveMarked(card)}
              </option>
            ))}
          </Select>
        </Field>
      )}
      <Field label="Statement" required {...validation('statementMonth')}>
        <Select value={statementMonth} aria-busy={form.statementMonths.isLoading} onChange={(event) => form.setField('statementMonth', event.target.value)}>
          <option value="">{form.statementMonths.isLoading ? 'Loading statements…' : 'Select a statement'}</option>
          {form.statementMonths.options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>
      </Field>
      {form.statementMonths.error && (
        <div className={styles.spanTwo}>
          <ErrorState message={STATEMENTS_LOAD_ERROR} onRetry={form.statementMonths.retry} />
        </div>
      )}
    </>
  )
}

export default function PotWithdrawalForm({ form, banks, lockedBank, lockedCard, onDelete, onCancel }: PotWithdrawalFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(form.errorFields)
  const formRef = useFormErrorFocus(form.errorFields)

  const { values } = form
  if (!values) return null
  const title = formTitle(form)

  if (form.isPaymentLocked && form.potsAccounts.length === 0) {
    return (
      <div className={styles.panel}>
        <Text as="h2" weight="semibold" size={400}>
          {title}
        </Text>
        <p role="status">Create a Pots account first.</p>
        <div className={styles.actions}>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </div>
    )
  }

  const isLocked = form.isStatementPaidLocked
  const isCategoryUnlisted = values.categoryId !== '' && !form.categories.some((category) => category.id === values.categoryId)
  const status = investmentWithdrawalText(form, banks, values.statementMonth)
  const validation: Validation = (field) => ({
    validationState: fieldError(field) ? 'error' : 'none',
    validationMessage: fieldError(field) ?? undefined,
  })
  const amountText = form.currency ? formatCurrency(Number(values.amount), form.currency) : values.amount

  return (
    <>
      <form
        ref={formRef}
        className={styles.panel}
        noValidate
        aria-labelledby="pot-withdrawal-title"
        onSubmit={(event) => {
          event.preventDefault()
          form.submit()
        }}
      >
        <fieldset disabled={form.isSaving} className={styles.savingFieldset} role="presentation">
          <Text as="h2" id="pot-withdrawal-title" weight="semibold" size={400}>
            {title}
          </Text>
          <ActionErrorAlert message={form.error} />
          {isLocked && (
            <MessageBar intent="info">
              <MessageBarBody>{PAID_STATEMENT_LOCK}</MessageBarBody>
            </MessageBar>
          )}

          {form.hasPotsError && <ErrorState message={POTS_LOAD_ERROR} onRetry={form.retryPots} />}

          <div className={styles.grid}>
            {form.isPaymentLocked && (
              <Field label="Pots account" required>
                <Select value={values.potsAccountId} onChange={(event) => form.setField('potsAccountId', event.target.value)}>
                  {form.potsAccounts.map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.name}
                    </option>
                  ))}
                </Select>
              </Field>
            )}

            {isLocked ? (
              <>
                <ReadOnlyField label="Date" value={formatDayMonthYearUtc(values.date)} />
                <ReadOnlyField label="Pot" value={form.potName} />
              </>
            ) : (
              <>
                <Field label="Date" required {...validation('date')}>
                  <Input type="date" value={values.date} onChange={(event) => form.setField('date', event.target.value)} />
                </Field>
                <PotField
                  value={values.potId}
                  pots={form.pots}
                  hasLoadError={form.hasPotsError}
                  error={fieldError('potId')}
                  onChange={(potId) => form.setField('potId', potId)}
                />
              </>
            )}

            <PaidWithField form={form} />
            {form.paidWith === 'bank' && <BankField form={form} validation={validation} banks={banks} lockedBank={lockedBank} bankId={values.bankId} />}
            {form.paidWith === 'card' && (
              <CardFields
                form={form}
                validation={validation}
                lockedCard={lockedCard}
                creditCardId={values.creditCardId}
                statementMonth={values.statementMonth}
              />
            )}

            {form.paidWith !== 'none' && (
              <Field label="Category" required {...validation('categoryId')}>
                <Select value={values.categoryId} onChange={(event) => form.setField('categoryId', event.target.value)}>
                  <option value="">Select a category</option>
                  {isCategoryUnlisted && <option value={values.categoryId}>Saved category (no longer available)</option>}
                  {form.categories.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
                </Select>
              </Field>
            )}

            <Field className={styles.spanTwo} label="Description" required {...validation('description')}>
              <Input name="description" value={values.description} onChange={(event) => form.setField('description', event.target.value)} />
            </Field>

            {isLocked ? (
              <ReadOnlyField label="Amount" value={amountText} />
            ) : (
              <Field label={form.currency ? `Amount (${form.currency})` : 'Amount'} required {...validation('amount')}>
                <Input type="number" step="0.01" min="0" value={values.amount} onChange={(event) => form.setField('amount', event.target.value)} />
              </Field>
            )}
          </div>

          {status && <Text role="status">{status}</Text>}
        </fieldset>

        <PotFormFooter
          saveLabel={saveLabel(form)}
          deleteLabel={form.isEditing ? 'Delete withdrawal' : null}
          deleteBlockedReason={isLocked ? PAID_STATEMENT_DELETE : undefined}
          isSaving={form.isSaving}
          isSaveDisabled={form.pots === null}
          onDelete={onDelete}
          onCancel={onCancel}
        />
      </form>

      <ConfirmDialog
        open={form.overdraftMessage !== null}
        title="Pot goes below zero"
        message={form.overdraftMessage ?? ''}
        confirmLabel="Continue"
        cancelLabel="Cancel"
        isBusy={form.isSaving}
        onConfirm={form.confirmOverdraft}
        onCancel={form.cancelOverdraft}
      />
    </>
  )
}
