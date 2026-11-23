import { useState } from 'react'
import { Field, Input, Select, Switch } from '@fluentui/react-components'
import type { AccountDto, InvestmentAccountDto, InvestmentAccountUpdateDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { useEntityForm } from '../hooks/useEntityForm'
import { useDuplicateNameField } from '../hooks/useDuplicateNameField'
import { accountsWith } from '../utils/accountCapabilities'
import { isLinkedAccountSource, LINKED_ACCOUNT_SOURCES, type LinkedAccountSource } from '../utils/linkedAccountSources'
import { inactiveMarked } from '../utils/selectableBanks'

type InvestmentAccountSourceOption = 'None' | 'CreditCard' | LinkedAccountSource

interface InvestmentAccountFormDialogProps {
  investmentAccount: InvestmentAccountDto | null
  creditCards: AccountDto[]
  accounts: AccountDto[]
  onCancel: () => void
  onSubmit: (values: InvestmentAccountUpdateDto) => Promise<unknown>
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
  const initialLinkedAccountId =
    investmentAccount && isLinkedAccountSource(investmentAccount.source)
      ? investmentAccount[LINKED_ACCOUNT_SOURCES[investmentAccount.source].idField] || undefined
      : undefined
  const [linkedAccountId, setLinkedAccountId] = useState(initialLinkedAccountId ?? '')
  const { isSaving, error, failure, submit } = useDialogSubmit('The investment account could not be saved.')
  const form = useEntityForm({ name, isActive, isLiability, source, creditCardId, linkedAccountId })

  const trimmedName = name.trim()
  const { nameRef, nameError, summaryError } = useDuplicateNameField(failure, error, trimmedName)
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const nameMessage = form.shown(validationMessage) || nameError
  const creditCardValidationMessage = source === 'CreditCard' && creditCardId === '' ? 'Select a credit card.' : ''
  const linked = isLinkedAccountSource(source) ? LINKED_ACCOUNT_SOURCES[source] : null
  const linkedAccountOptions = linked ? accountsWith(accounts, linked.capability, initialLinkedAccountId) : []
  const hasLinkedAccountChoices = linkedAccountOptions.length > 0
  const shownLinkedAccountId = linkedAccountOptions.some((account) => account.id === linkedAccountId) ? linkedAccountId : ''
  const isLinkedAccountMissing = linked !== null && shownLinkedAccountId === ''
  const linkedAccountValidationMessage =
    isLinkedAccountMissing && hasLinkedAccountChoices ? `Select a ${linked.label}.` : ''
  const sourceValidationMessage =
    isLinkedAccountMissing && !hasLinkedAccountChoices
      ? `No active ${linked.label}s. Add one in Admin → CashFlow → Accounts, or choose another source.`
      : ''
  const canSubmit =
    validationMessage.length === 0 &&
    creditCardValidationMessage.length === 0 &&
    !isLinkedAccountMissing

  // The picker always offers every active card, plus the account's currently-linked card even if
  // it has since become inactive - otherwise reopening Edit would silently show no selection for a
  // link that is, in fact, still configured.
  const cardOptions = creditCards.filter(
    (c) => c.isActive || (investmentAccount?.source === 'CreditCard' && c.id === investmentAccount.creditCardId),
  )

  const handleSubmit = () => {
    form.revealValidation()
    if (!canSubmit) return
    void submit(() =>
      onSubmit({
        name: trimmedName,
        isActive,
        isLiability,
        source,
        creditCardId: source === 'CreditCard' ? creditCardId : null,
        potsAccountId: null,
        savingsAccountId: null,
        ...(linked && { [linked.idField]: shownLinkedAccountId }),
      }),
    )
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit investment account' : 'New investment account'}
      saveLabel={isEditing ? 'Save' : 'Add investment account'}
      error={summaryError}
      isSaving={isSaving}
      isDirty={form.isDirty}
      onSave={handleSubmit}
      onCancel={onCancel}
    >
      <Field
        label="Name"
        required
        validationState={nameMessage ? 'error' : 'none'}
        validationMessage={nameMessage}
      >
        <Input ref={nameRef} value={name} onChange={(e) => setName(e.target.value)} />
      </Field>

      <Field label="Active">
        <Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
      </Field>

      <Field label="Liability">
        <Switch checked={isLiability} onChange={(e) => setIsLiability(e.target.checked)} />
      </Field>

      <Field label="Source" {...form.fieldValidation(sourceValidationMessage)}>
        <Select
          value={source}
          onChange={(e) => setSource(e.target.value as InvestmentAccountSourceOption)}
        >
          <option value="None">None</option>
          <option value="CreditCard">Credit card</option>
          <option value="PotsAccountTotal">Pots account total</option>
          <option value="SavingsAccountBalance">Savings account balance</option>
        </Select>
      </Field>

      {source === 'CreditCard' && (
        <Field
          label="Credit card"
          required
          {...form.fieldValidation(creditCardValidationMessage)}
        >
          <Select value={creditCardId} onChange={(e) => setCreditCardId(e.target.value)}>
            <option value="">Select card…</option>
            {cardOptions.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </Select>
        </Field>
      )}

      {linked && (
        <Field
          label={linked.label}
          required
          hint={
            hasLinkedAccountChoices
              ? `Investment snapshots will suggest the ${linked.label}'s ${linked.snapshotValue} at the end of the previous month.`
              : `Add a ${linked.label} in Admin → CashFlow → Accounts first.`
          }
          {...form.fieldValidation(linkedAccountValidationMessage)}
        >
          <Select
            value={shownLinkedAccountId}
            onChange={(e) => setLinkedAccountId(e.target.value)}
            disabled={!hasLinkedAccountChoices}
          >
            <option value="">{hasLinkedAccountChoices ? `Select ${linked.label}…` : `No active ${linked.label}s`}</option>
            {linkedAccountOptions.map((account) => (
              <option key={account.id} value={account.id}>
                {inactiveMarked(account)}
              </option>
            ))}
          </Select>
        </Field>
      )}
    </EntityFormDialog>
  )
}
