import { Button, Field, InfoLabel, Input, Select, Text } from '@fluentui/react-components'
import type { LabelProps } from '@fluentui/react-components'
import type { BankOption } from '../api/types'
import type { LockedBank } from '../hooks/formSettings'
import { useFieldError } from '../hooks/useFieldError'
import { useFormErrorFocus } from '../hooks/useFormErrorFocus'
import type { PotWithdrawalFormModel } from '../hooks/usePotWithdrawalForm'
import { POTS_LOAD_ERROR } from '../hooks/usePots'
import { inactiveMarked, selectableBanks } from '../utils/selectableBanks'
import ActionErrorAlert from './ActionErrorAlert'
import ConfirmDialog from './ConfirmDialog'
import ErrorState from './ErrorState'
import { useFormPanelStyles } from './formPanelStyles'
import PotField from './PotField'
import PotFormFooter from './PotFormFooter'
import LockedValue from './LockedValue'

interface PotWithdrawalFormProps {
  form: PotWithdrawalFormModel
  banks: BankOption[]
  lockedBank?: LockedBank
  onDelete: () => void
  onCancel: () => void
}

function formTitle(form: PotWithdrawalFormModel): string {
  if (form.isEditing) return 'Edit withdrawal'
  return form.isBankLocked ? 'Withdraw from pots' : 'New withdrawal'
}

function saveLabel(form: PotWithdrawalFormModel): string {
  if (form.isEditing) return 'Save'
  return form.isBankLocked ? 'Withdraw from pots' : 'Add withdrawal'
}

export default function PotWithdrawalForm({ form, banks, lockedBank, onDelete, onCancel }: PotWithdrawalFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(form.errorFields)
  const formRef = useFormErrorFocus(form.errorFields)

  const { values } = form
  if (!values) return null
  const title = formTitle(form)

  if (form.isBankLocked && form.potsAccounts.length === 0) {
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

  const isCategoryUnlisted = values.categoryId !== '' && !form.categories.some((category) => category.id === values.categoryId)

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
        <fieldset disabled={form.isSaving} className={styles.savingFieldset}>
          <Text as="h2" id="pot-withdrawal-title" weight="semibold" size={400}>
            {title}
          </Text>
          <ActionErrorAlert message={form.error} />

          {form.hasPotsError && <ErrorState message={POTS_LOAD_ERROR} onRetry={form.retryPots} />}

          <div className={styles.grid}>
            {form.isBankLocked && (
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

            <PotField
              value={values.potId}
              pots={form.pots}
              hasLoadError={form.hasPotsError}
              error={fieldError('potId')}
              onChange={(potId) => form.setField('potId', potId)}
            />

            <Field label="Date" required validationState={fieldError('date') ? 'error' : 'none'} validationMessage={fieldError('date')}>
              <Input type="date" value={values.date} onChange={(event) => form.setField('date', event.target.value)} />
            </Field>

            <Field
              className={styles.spanTwo}
              label="Description"
              required
              validationState={fieldError('description') ? 'error' : 'none'}
              validationMessage={fieldError('description')}
            >
              <Input name="description" value={values.description} onChange={(event) => form.setField('description', event.target.value)} />
            </Field>

            <Field
              label={form.currency ? `Amount (${form.currency})` : 'Amount'}
              required
              validationState={fieldError('amount') ? 'error' : 'none'}
              validationMessage={fieldError('amount')}
            >
              <Input type="number" step="0.01" min="0" value={values.amount} onChange={(event) => form.setField('amount', event.target.value)} />
            </Field>

            {form.isBankLocked && lockedBank ? (
              <LockedValue label="Through bank" value={lockedBank.name} />
            ) : (
              <Field
                label={{
                  children: (_: unknown, props: LabelProps) => (
                    <InfoLabel
                      {...props}
                      info="Choose a bank when the money passes through it. The bank records a return from the Pots account and an expense in the category you pick, so it nets to zero."
                    >
                      Through bank
                    </InfoLabel>
                  ),
                }}
              >
                <Select value={values.bankId} onChange={(event) => form.setField('bankId', event.target.value)}>
                  <option value="">No bank (direct)</option>
                  {selectableBanks(banks, values.bankId, form.openedBankId).map((bank) => (
                    <option key={bank.id} value={bank.id}>
                      {inactiveMarked(bank)}
                    </option>
                  ))}
                </Select>
              </Field>
            )}

            {values.bankId !== '' && (
              <Field
                label="Category"
                required
                validationState={fieldError('categoryId') ? 'error' : 'none'}
                validationMessage={fieldError('categoryId')}
              >
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
          </div>
        </fieldset>

        <PotFormFooter
          saveLabel={saveLabel(form)}
          deleteLabel={form.isEditing ? 'Delete withdrawal' : null}
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
