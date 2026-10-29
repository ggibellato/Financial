import { useState } from 'react'
import { Field, Input, Select, Switch } from '@fluentui/react-components'
import type { AccountDto, IncomeSourceDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import type { AccountsStatus } from '../hooks/useAccounts'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { accountsWith } from '../utils/accountCapabilities'

const GROUP_OPTIONS = ['Salary', 'DividendoJuros', 'NonReportable']

interface IncomeSourceFormDialogProps {
  incomeSource: IncomeSourceDto | null
  accounts: AccountDto[]
  accountsStatus: AccountsStatus
  onCancel: () => void
  onSubmit: (name: string, group: string, isActive: boolean, potsAccountId: string | null) => Promise<unknown>
}

function potsAccountHint(accountsStatus: AccountsStatus, hasChoices: boolean, potsAccountId: string): string {
  if (accountsStatus === 'loading') return 'Loading accounts…'
  if (accountsStatus === 'failed') return "Accounts couldn't be loaded, so the Pots account can't be changed. Close this and use Retry on the page."
  if (!hasChoices) return 'Add a Pots account in Admin → CashFlow → Accounts to split income.'
  if (potsAccountId === '') return "None means incomes from this source aren't split."
  return "Incomes from this source split into the account's active pots."
}

export default function IncomeSourceFormDialog({
  incomeSource,
  accounts,
  accountsStatus,
  onCancel,
  onSubmit,
}: IncomeSourceFormDialogProps) {
  const isEditing = incomeSource !== null
  const linkedPotsAccountId = incomeSource?.potsAccountId ?? ''
  const [name, setName] = useState(incomeSource?.name ?? '')
  const [group, setGroup] = useState(incomeSource?.group ?? GROUP_OPTIONS[0])
  const [isActive, setIsActive] = useState(incomeSource?.isActive ?? true)
  const [potsAccountId, setPotsAccountId] = useState(linkedPotsAccountId)
  const { isSaving, error, submit } = useDialogSubmit('The income source could not be saved.')

  const trimmedName = name.trim()
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const canSubmit = validationMessage.length === 0 && !isSaving
  const potsAccountOptions = accountsWith(accounts, 'ReceivesIncomeSplit', linkedPotsAccountId || undefined)
  const isLinkedAccountUnlisted =
    linkedPotsAccountId !== '' && !potsAccountOptions.some((account) => account.id === linkedPotsAccountId)
  const hasPotsAccountChoices = potsAccountOptions.length > 0
  const canChoosePotsAccount = accountsStatus === 'ready' && hasPotsAccountChoices && !isLinkedAccountUnlisted

  const handleSubmit = () => {
    if (!canSubmit) return
    void submit(() => onSubmit(trimmedName, group, isActive, potsAccountId === '' ? null : potsAccountId))
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit Income Source' : 'Create Income Source'}
      error={error}
      isSaving={isSaving}
      canSave={canSubmit}
      onSave={handleSubmit}
      onCancel={onCancel}
    >
      <Field
        label="Name"
        required
        validationState={validationMessage ? 'error' : 'none'}
        validationMessage={validationMessage}
      >
        <Input value={name} onChange={(e) => setName(e.target.value)} disabled={isSaving} autoFocus />
      </Field>

      <Field label="Group">
        <Select value={group} onChange={(e) => setGroup(e.target.value)} disabled={isSaving}>
          {GROUP_OPTIONS.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </Select>
      </Field>

      <Field label="Active">
        <Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} disabled={isSaving} />
      </Field>

      <Field label="Pots account" hint={potsAccountHint(accountsStatus, hasPotsAccountChoices, potsAccountId)}>
        <Select
          value={potsAccountId}
          onChange={(e) => setPotsAccountId(e.target.value)}
          disabled={isSaving || !canChoosePotsAccount}
        >
          <option value="">None</option>
          {isLinkedAccountUnlisted && <option value={linkedPotsAccountId}>Current Pots account</option>}
          {potsAccountOptions.map((account) => (
            <option key={account.id} value={account.id}>
              {account.isActive ? account.name : `${account.name} (inactive)`}
            </option>
          ))}
        </Select>
      </Field>
    </EntityFormDialog>
  )
}
