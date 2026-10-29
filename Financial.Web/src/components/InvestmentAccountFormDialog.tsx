import { useState } from 'react'
import { Field, Input, Select, Switch } from '@fluentui/react-components'
import type { AccountDto, CreditCardDto, InvestmentAccountDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { accountsWith } from '../utils/accountCapabilities'

export type InvestmentAccountSourceOption = 'None' | 'CreditCard' | 'PotsAccountTotal'

interface InvestmentAccountFormDialogProps {
  investmentAccount: InvestmentAccountDto | null
  creditCards: CreditCardDto[]
  accounts: AccountDto[]
  onCancel: () => void
  onSubmit: (
    name: string,
    isActive: boolean,
    isLiability: boolean,
    source: InvestmentAccountSourceOption,
    creditCardId: string | null,
    potsAccountId: string | null,
  ) => Promise<unknown>
}

export default function InvestmentAccountFormDialog({
  investmentAccount,
  creditCards,
  accounts,
  onCancel,
  onSubmit,
}: InvestmentAccountFormDialogProps) {
  const isEditing = investmentAccount !== null
  const [name, setName] = useState(investmentAccount?.name ?? '')
  const [isActive, setIsActive] = useState(investmentAccount?.isActive ?? true)
  const [isLiability, setIsLiability] = useState(investmentAccount?.isLiability ?? false)
  const [source, setSource] = useState<InvestmentAccountSourceOption>(
    (investmentAccount?.source as InvestmentAccountSourceOption | undefined) ?? 'None',
  )
  const [creditCardId, setCreditCardId] = useState(investmentAccount?.creditCardId ?? '')
  const [potsAccountId, setPotsAccountId] = useState(investmentAccount?.potsAccountId ?? '')
  const { isSaving, error, submit } = useDialogSubmit('The investment account could not be saved.')

  const trimmedName = name.trim()
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const creditCardValidationMessage = source === 'CreditCard' && creditCardId === '' ? 'Select a credit card' : ''
  const potsAccountOptions = accountsWith(accounts, 'HasPots', investmentAccount?.potsAccountId ?? undefined)
  const hasPotsAccountChoices = potsAccountOptions.length > 0
  const shownPotsAccountId = potsAccountOptions.some((account) => account.id === potsAccountId) ? potsAccountId : ''
  const isPotsAccountMissing = source === 'PotsAccountTotal' && shownPotsAccountId === ''
  const potsAccountValidationMessage = isPotsAccountMissing && hasPotsAccountChoices ? 'Select a Pots account' : ''
  const canSubmit =
    validationMessage.length === 0 &&
    creditCardValidationMessage.length === 0 &&
    !isPotsAccountMissing &&
    !isSaving

  // The picker always offers every active card, plus the account's currently-linked card even if
  // it has since become inactive - otherwise reopening Edit would silently show no selection for a
  // link that is, in fact, still configured.
  const cardOptions = creditCards.filter(
    (c) => c.isActive || (investmentAccount?.source === 'CreditCard' && c.id === investmentAccount.creditCardId),
  )

  const handleSubmit = () => {
    if (!canSubmit) return
    void submit(() =>
      onSubmit(
        trimmedName,
        isActive,
        isLiability,
        source,
        source === 'CreditCard' ? creditCardId : null,
        source === 'PotsAccountTotal' ? shownPotsAccountId : null,
      ),
    )
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit Investment Account' : 'Create Investment Account'}
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

      <Field label="Active">
        <Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} disabled={isSaving} />
      </Field>

      <Field label="Liability">
        <Switch checked={isLiability} onChange={(e) => setIsLiability(e.target.checked)} disabled={isSaving} />
      </Field>

      <Field label="Source">
        <Select
          value={source}
          onChange={(e) => setSource(e.target.value as InvestmentAccountSourceOption)}
          disabled={isSaving}
        >
          <option value="None">None</option>
          <option value="CreditCard">Credit Card</option>
          <option value="PotsAccountTotal">Pots account total</option>
        </Select>
      </Field>

      {source === 'CreditCard' && (
        <Field
          label="Credit Card"
          required
          validationState={creditCardValidationMessage ? 'error' : 'none'}
          validationMessage={creditCardValidationMessage}
        >
          <Select value={creditCardId} onChange={(e) => setCreditCardId(e.target.value)} disabled={isSaving}>
            <option value="">Select card…</option>
            {cardOptions.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </Select>
        </Field>
      )}

      {source === 'PotsAccountTotal' && (
        <Field
          label="Pots account"
          required
          hint={
            hasPotsAccountChoices
              ? "Investment Snapshots will suggest the Pots account's total at the end of the previous month."
              : 'Add a Pots account in Admin → Accounts first.'
          }
          validationState={potsAccountValidationMessage ? 'error' : 'none'}
          validationMessage={potsAccountValidationMessage}
        >
          <Select
            value={shownPotsAccountId}
            onChange={(e) => setPotsAccountId(e.target.value)}
            disabled={isSaving || !hasPotsAccountChoices}
          >
            <option value="">{hasPotsAccountChoices ? 'Select Pots account…' : 'No active Pots accounts'}</option>
            {potsAccountOptions.map((account) => (
              <option key={account.id} value={account.id}>
                {account.isActive ? account.name : `${account.name} (inactive)`}
              </option>
            ))}
          </Select>
        </Field>
      )}
    </EntityFormDialog>
  )
}
