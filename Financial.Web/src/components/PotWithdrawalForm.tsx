import { useEffect, useRef } from 'react'
import { Button, Field, InfoLabel, Input, MessageBar, MessageBarBody, Select, Spinner, Text } from '@fluentui/react-components'
import type { LabelProps } from '@fluentui/react-components'
import { DeleteRegular } from '@fluentui/react-icons'
import type { BankDto } from '../api/types'
import type { LockedBank } from '../hooks/formSettings'
import { useFieldError } from '../hooks/useFieldError'
import { useFormErrorFocus } from '../hooks/useFormErrorFocus'
import type { PotWithdrawalFormModel } from '../hooks/usePotWithdrawalForm'
import { POTS_LOAD_ERROR } from '../hooks/usePots'
import { selectableBanks } from '../utils/selectableBanks'
import ConfirmDialog from './ConfirmDialog'
import ErrorState from './ErrorState'
import { useFormPanelStyles } from './formPanelStyles'

interface PotWithdrawalFormProps {
  form: PotWithdrawalFormModel
  banks: BankDto[]
  lockedBank?: LockedBank
  onDelete: () => void
  onCancel: () => void
}

function formTitle(form: PotWithdrawalFormModel): string {
  if (form.isEditing) return 'Edit withdrawal'
  return form.isBankLocked ? 'Withdraw from pots' : 'New withdrawal'
}

export default function PotWithdrawalForm({ form, banks, lockedBank, onDelete, onCancel }: PotWithdrawalFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(form.errorFields)
  const generalError = Object.keys(form.errorFields).length === 0 ? form.error : null
  const { formRef, saveRef } = useFormErrorFocus(form.errorFields, generalError)
  const wasOverdraftOpen = useRef(false)

  useEffect(() => {
    if (wasOverdraftOpen.current && form.overdraftMessage === null) saveRef.current?.focus()
    wasOverdraftOpen.current = form.overdraftMessage !== null
  }, [form.overdraftMessage, saveRef])

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

            <Field
              label="Pot"
              required
              hint={form.pots === null && !form.hasPotsError ? 'Loading pots…' : undefined}
              validationState={fieldError('potId') ? 'error' : 'none'}
              validationMessage={fieldError('potId')}
            >
              <Select value={values.potId} onChange={(event) => form.setField('potId', event.target.value)}>
                {values.potId === '' && <option value="">{form.pots === null ? 'Loading pots…' : 'Select a pot'}</option>}
                {(form.pots ?? []).map((pot) => (
                  <option key={pot.id} value={pot.id}>
                    {pot.isActive ? pot.name : `${pot.name} (inactive)`}
                  </option>
                ))}
              </Select>
            </Field>

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
              <Field label="Through bank">
                <Text weight="semibold">{lockedBank.name}</Text>
              </Field>
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
                      {bank.isActive === false ? `${bank.name} (inactive)` : bank.name}
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

          <div className={styles.actions}>
            <Button
              ref={saveRef}
              type="submit"
              appearance="primary"
              disabled={form.isSaving || form.pots === null}
              icon={form.isSaving ? <Spinner size="tiny" /> : undefined}
            >
              {form.isSaving ? 'Saving...' : form.isEditing ? 'Save' : 'Add withdrawal'}
            </Button>
            <Button appearance="secondary" onClick={onCancel}>
              Cancel
            </Button>
            {form.isEditing && (
              <Button className={styles.deleteAction} appearance="primary" icon={<DeleteRegular />} onClick={onDelete}>
                Delete
              </Button>
            )}
          </div>

          {generalError && (
            <MessageBar role="alert" intent="error">
              <MessageBarBody>{generalError}</MessageBarBody>
            </MessageBar>
          )}
        </fieldset>
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
