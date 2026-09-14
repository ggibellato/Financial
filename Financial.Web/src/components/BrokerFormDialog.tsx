import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  InfoLabel,
  Input,
  MessageBar,
  MessageBarBody,
  Select,
} from '@fluentui/react-components'
import type { LabelProps } from '@fluentui/react-components'
import type { BrokerDto, CostBasisMethod } from '../api/types'
import { useFormPanelStyles } from './formPanelStyles'
import { getErrorMessage } from '../utils/formatters'
import { COST_BASIS_METHODS, COST_BASIS_METHOD_LABELS } from '../utils/costBasisMethod'

/** The Investment bounded context has no shared currency enum (CashFlow's Currency is BRL/GBP only
 * and out of reach across the bounded-context boundary); these are the values already observed in
 * this codebase's broker fixtures. */
const CURRENCIES = ['BRL', 'GBP', 'USD']

interface BrokerFormDialogProps {
  broker: BrokerDto | null
  onCancel: () => void
  onSubmit: (name: string, currency: string, costBasisMethod: CostBasisMethod) => Promise<unknown>
}

export default function BrokerFormDialog({ broker, onCancel, onSubmit }: BrokerFormDialogProps) {
  const styles = useFormPanelStyles()
  const isEditing = broker !== null
  const [name, setName] = useState(broker?.name ?? '')
  const [currency, setCurrency] = useState(broker?.currency ?? CURRENCIES[0])
  const [costBasisMethod, setCostBasisMethod] = useState<CostBasisMethod>(broker?.costBasisMethod ?? 'AverageCost')
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
      await onSubmit(trimmedName, currency, costBasisMethod)
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'The broker could not be saved.'))
      setIsSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>{isEditing ? 'Edit Broker' : 'Create Broker'}</DialogTitle>
          <DialogContent>
            <Field
              label="Name"
              required
              validationState={validationMessage ? 'error' : 'none'}
              validationMessage={validationMessage}
            >
              <Input value={name} onChange={(e) => setName(e.target.value)} disabled={isSaving} autoFocus />
            </Field>

            <Field label="Currency" required>
              <Select value={currency} onChange={(e) => setCurrency(e.target.value)} disabled={isSaving}>
                {CURRENCIES.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </Select>
            </Field>

            <Field
              label={{
                children: (_: unknown, props: LabelProps) => (
                  <InfoLabel
                    {...props}
                    info="Changing this for an existing broker recalculates every disposal record under it."
                  >
                    Cost Basis Method
                  </InfoLabel>
                ),
              }}
              required
            >
              <Select
                value={costBasisMethod}
                onChange={(e) => setCostBasisMethod(e.target.value as CostBasisMethod)}
                disabled={isSaving}
              >
                {COST_BASIS_METHODS.map((method) => (
                  <option key={method} value={method}>
                    {COST_BASIS_METHOD_LABELS[method]}
                  </option>
                ))}
              </Select>
            </Field>

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
