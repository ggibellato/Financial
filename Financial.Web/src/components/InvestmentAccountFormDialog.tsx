import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Select,
  Switch,
  Text,
} from '@fluentui/react-components'
import type { CreditCardDto, InvestmentAccountDto } from '../api/types'
import { useFormPanelStyles } from './formPanelStyles'
import { getErrorMessage } from '../utils/formatters'

export type InvestmentAccountSourceOption = 'None' | 'CreditCard' | 'ReserveBucketsSum'

interface InvestmentAccountFormDialogProps {
  investmentAccount: InvestmentAccountDto | null
  creditCards: CreditCardDto[]
  onCancel: () => void
  onSubmit: (
    name: string,
    isActive: boolean,
    isLiability: boolean,
    source: InvestmentAccountSourceOption,
    creditCardId: string | null,
  ) => Promise<unknown>
}

export default function InvestmentAccountFormDialog({ investmentAccount, creditCards, onCancel, onSubmit }: InvestmentAccountFormDialogProps) {
  const styles = useFormPanelStyles()
  const isEditing = investmentAccount !== null
  const [name, setName] = useState(investmentAccount?.name ?? '')
  const [isActive, setIsActive] = useState(investmentAccount?.isActive ?? true)
  const [isLiability, setIsLiability] = useState(investmentAccount?.isLiability ?? false)
  const [source, setSource] = useState<InvestmentAccountSourceOption>(
    (investmentAccount?.source as InvestmentAccountSourceOption | undefined) ?? 'None',
  )
  const [creditCardId, setCreditCardId] = useState(investmentAccount?.creditCardId ?? '')
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const trimmedName = name.trim()
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const creditCardValidationMessage = source === 'CreditCard' && creditCardId === '' ? 'Select a credit card' : ''
  const canSubmit = validationMessage.length === 0 && creditCardValidationMessage.length === 0 && !isSaving

  // The picker always offers every active card, plus the account's currently-linked card even if
  // it has since become inactive - otherwise reopening Edit would silently show no selection for a
  // link that is, in fact, still configured.
  const cardOptions = creditCards.filter(
    (c) => c.isActive || (investmentAccount?.source === 'CreditCard' && c.id === investmentAccount.creditCardId),
  )

  const handleSubmit = async () => {
    if (!canSubmit) return

    setIsSaving(true)
    setError(null)
    try {
      await onSubmit(trimmedName, isActive, isLiability, source, source === 'CreditCard' ? creditCardId : null)
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'The investment account could not be saved.'))
      setIsSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>{isEditing ? 'Edit Investment Account' : 'Create Investment Account'}</DialogTitle>
          <DialogContent>
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
                <option value="ReserveBucketsSum">Sum of Reserve Buckets</option>
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

            {source === 'ReserveBucketsSum' && (
              <Text as="p" size={200}>
                Uses the total balance across all reserve buckets
              </Text>
            )}

            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}
          </DialogContent>
          <div className={styles.actions}>
            <Button appearance="primary" onClick={() => void handleSubmit()} disabled={!canSubmit}>
              Save
            </Button>
            <Button appearance="secondary" onClick={onCancel} disabled={isSaving}>
              Cancel
            </Button>
          </div>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
