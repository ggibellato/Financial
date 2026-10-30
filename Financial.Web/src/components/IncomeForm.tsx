import { useState } from 'react'
import {
  Button,
  Checkbox,
  Field,
  Input,
  MessageBar,
  MessageBarActions,
  MessageBarBody,
  Select,
  Spinner,
  Text,
} from '@fluentui/react-components'
import type { AccountDto, BankDto, IncomeSourceDto } from '../api/types'
import type { LockedBank } from '../hooks/formSettings'
import type { AccountsStatus } from '../hooks/useAccounts'
import { INCOME_SOURCES_WITH_GROSS_VALUE, selectActiveIncomeSources } from '../hooks/useIncomeForm'
import { useFieldError } from '../hooks/useFieldError'
import { selectableBanks } from '../utils/selectableBanks'
import { useFormPanelStyles } from './formPanelStyles'

export type IncomeFormField =
  | 'date'
  | 'incomeSource'
  | 'grossValue'
  | 'netValue'
  | 'bank'
  | 'description'
  | 'splitToReserve'
  | 'potsAccountId'

interface IncomeFormProps {
  isEditing: boolean
  date: string
  incomeSource: string
  grossValue: string
  netValue: string
  bank: string
  description: string
  splitToReserve: boolean
  potsAccountId: string
  potsAccounts: AccountDto[]
  potsAccountsStatus: AccountsStatus
  onRetryPotsAccounts: () => void
  banks: BankDto[]
  incomeSources: IncomeSourceDto[]
  isSaving: boolean
  lockedBank?: LockedBank
  saveError: string | null
  saveErrorFields: Partial<Record<IncomeFormField, string>>
  onFieldChange: (field: IncomeFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

function potsAccountHint(status: AccountsStatus, hasChoices: boolean): string | undefined {
  if (status === 'loading') return 'Loading Pots accounts…'
  if (status === 'failed') return undefined
  if (!hasChoices) return 'No active Pots account. Add one in Admin → CashFlow → Accounts, or untick the split.'
  return "The split goes into this account's active pots."
}

export default function IncomeForm({
  isEditing,
  date,
  incomeSource,
  grossValue,
  netValue,
  bank,
  description,
  splitToReserve,
  potsAccountId,
  potsAccounts,
  potsAccountsStatus,
  onRetryPotsAccounts,
  banks,
  incomeSources,
  isSaving,
  lockedBank,
  saveError,
  saveErrorFields,
  onFieldChange,
  onSave,
  onCancel,
}: IncomeFormProps) {
  const styles = useFormPanelStyles()
  const [openedBank] = useState(bank)
  const fieldError = useFieldError(saveErrorFields)
  const activeIncomeSources = selectActiveIncomeSources(incomeSources)
  const showGrossValueField = INCOME_SOURCES_WITH_GROSS_VALUE.includes(
    incomeSources.find((s) => s.id === incomeSource)?.name ?? '',
  )
  const showSplitField = incomeSources.find((s) => s.id === incomeSource)?.potsAccountId != null
  const showPotsAccountField = showSplitField && splitToReserve
  const isPotsAccountUnlisted = potsAccountId !== '' && !potsAccounts.some((account) => account.id === potsAccountId)
  const canChoosePotsAccount = potsAccountsStatus === 'ready' && potsAccounts.length > 0

  return (
    <form
      className={styles.panel}
      noValidate
      onSubmit={(event) => {
        event.preventDefault()
        onSave()
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset}>
        <Text as="h2" weight="semibold" size={400}>
          {isEditing ? 'Edit Income' : 'New Income'}
        </Text>
        {Object.keys(saveErrorFields).length === 0 && saveError && (
          <MessageBar intent="error">
            <MessageBarBody>{saveError}</MessageBarBody>
          </MessageBar>
        )}

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
            label="Source"
            required
            validationState={fieldError('incomeSource') ? 'error' : 'none'}
            validationMessage={fieldError('incomeSource')}
          >
            <Select value={incomeSource} onChange={(e) => onFieldChange('incomeSource', e.target.value)}>
              {activeIncomeSources.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </Select>
          </Field>

          <Field label="Bank">
            {lockedBank ? (
              <Text weight="semibold">{lockedBank.name}</Text>
            ) : (
              <Select value={bank} onChange={(e) => onFieldChange('bank', e.target.value)}>
                <option value="">— No bank —</option>
                {selectableBanks(banks, bank, openedBank).map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))}
              </Select>
            )}
          </Field>

          <Field label="Description">
            <Input name="description" value={description} onChange={(e) => onFieldChange('description', e.target.value)} />
          </Field>

          {showGrossValueField && (
            <Field
              label="Gross Value"
              validationState={fieldError('grossValue') ? 'error' : 'none'}
              validationMessage={fieldError('grossValue')}
            >
              <Input
                type="number"
                step="0.01"
                value={grossValue}
                onChange={(e) => onFieldChange('grossValue', e.target.value)}
              />
            </Field>
          )}

          <Field
            label="Net Value"
            required
            validationState={fieldError('netValue') ? 'error' : 'none'}
            validationMessage={fieldError('netValue')}
          >
            <Input
              type="number"
              step="0.01"
              value={netValue}
              onChange={(e) => onFieldChange('netValue', e.target.value)}
            />
          </Field>

          {showSplitField && (
            <div>
              <Checkbox
                label="Split to pots"
                checked={splitToReserve}
                onChange={(_, data) => onFieldChange('splitToReserve', data.checked ? 'true' : 'false')}
              />
            </div>
          )}

          {showPotsAccountField && (
            <Field
              label="Pots account"
              required
              hint={potsAccountHint(potsAccountsStatus, potsAccounts.length > 0)}
              validationState={fieldError('potsAccountId') ? 'error' : 'none'}
              validationMessage={fieldError('potsAccountId')}
            >
              <Select
                value={potsAccountId}
                onChange={(e) => onFieldChange('potsAccountId', e.target.value)}
                disabled={!canChoosePotsAccount}
              >
                <option value="">Select Pots account…</option>
                {isPotsAccountUnlisted && <option value={potsAccountId}>Selected Pots account</option>}
                {potsAccounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.isActive ? account.name : `${account.name} (inactive)`}
                  </option>
                ))}
              </Select>
            </Field>
          )}
        </div>

        {showPotsAccountField && potsAccountsStatus === 'failed' && (
          <MessageBar intent="warning">
            <MessageBarBody>Pots accounts couldn&rsquo;t be loaded, so the Pots account can&rsquo;t be changed.</MessageBarBody>
            <MessageBarActions>
              <Button appearance="primary" aria-label="Retry loading Pots accounts" onClick={onRetryPotsAccounts}>
                Retry
              </Button>
            </MessageBarActions>
          </MessageBar>
        )}

        <div className={styles.actions}>
          <Button type="submit" appearance="primary" disabled={isSaving} icon={isSaving ? <Spinner size="tiny" /> : undefined}>
            {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Add Income'}
          </Button>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}
