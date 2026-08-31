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
  Switch,
} from '@fluentui/react-components'
import type { InvestmentAccountDto } from '../api/types'
import { useFormPanelStyles } from './formPanelStyles'
import { getErrorMessage } from '../utils/formatters'
import AliasesInput from './AliasesInput'

interface InvestmentAccountFormDialogProps {
  investmentAccount: InvestmentAccountDto | null
  onCancel: () => void
  onSubmit: (name: string, isActive: boolean, isLiability: boolean, aliases: string[]) => Promise<unknown>
}

export default function InvestmentAccountFormDialog({ investmentAccount, onCancel, onSubmit }: InvestmentAccountFormDialogProps) {
  const styles = useFormPanelStyles()
  const isEditing = investmentAccount !== null
  const [name, setName] = useState(investmentAccount?.name ?? '')
  const [isActive, setIsActive] = useState(investmentAccount?.isActive ?? true)
  const [isLiability, setIsLiability] = useState(investmentAccount?.isLiability ?? false)
  const [aliases, setAliases] = useState<string[]>(investmentAccount?.aliases ? [...investmentAccount.aliases] : [])
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const trimmedName = name.trim()
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const canSubmit = validationMessage.length === 0 && !isSaving

  const handleSubmit = async () => {
    if (!canSubmit) return

    setIsSaving(true)
    setError(null)
    try {
      await onSubmit(trimmedName, isActive, isLiability, aliases)
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

            <AliasesInput aliases={aliases} onChange={setAliases} disabled={isSaving} />

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
