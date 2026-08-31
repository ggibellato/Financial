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
} from '@fluentui/react-components'
import type { BrokerDto, PortfolioDto } from '../api/types'
import { useFormPanelStyles } from './formPanelStyles'
import { getErrorMessage } from '../utils/formatters'

interface PortfolioFormDialogProps {
  portfolio: PortfolioDto | null
  activeBrokers: BrokerDto[]
  onCancel: () => void
  onSubmit: (brokerName: string, name: string) => Promise<unknown>
}

export default function PortfolioFormDialog({ portfolio, activeBrokers, onCancel, onSubmit }: PortfolioFormDialogProps) {
  const styles = useFormPanelStyles()
  const isEditing = portfolio !== null
  const [brokerName, setBrokerName] = useState(portfolio?.brokerName ?? activeBrokers[0]?.name ?? '')
  const [name, setName] = useState(portfolio?.name ?? '')
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const trimmedName = name.trim()
  const nameValidationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const brokerValidationMessage = !isEditing && brokerName.length === 0 ? 'A broker is required.' : ''
  const canSubmit = nameValidationMessage.length === 0 && brokerValidationMessage.length === 0 && !isSaving

  const handleSubmit = async () => {
    if (!canSubmit) return

    setIsSaving(true)
    setError(null)
    try {
      await onSubmit(brokerName, trimmedName)
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'The portfolio could not be saved.'))
      setIsSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>{isEditing ? 'Edit Portfolio' : 'Create Portfolio'}</DialogTitle>
          <DialogContent>
            {isEditing ? (
              <Field label="Broker">
                <Input value={portfolio.brokerName} disabled readOnly />
              </Field>
            ) : (
              <Field
                label="Broker"
                required
                validationState={brokerValidationMessage ? 'error' : 'none'}
                validationMessage={brokerValidationMessage}
              >
                <Select value={brokerName} onChange={(e) => setBrokerName(e.target.value)} disabled={isSaving}>
                  {activeBrokers.length === 0 && <option value="">No active brokers available</option>}
                  {activeBrokers.map((b) => (
                    <option key={b.name} value={b.name}>
                      {b.name}
                    </option>
                  ))}
                </Select>
              </Field>
            )}

            <Field
              label="Name"
              required
              validationState={nameValidationMessage ? 'error' : 'none'}
              validationMessage={nameValidationMessage}
            >
              <Input value={name} onChange={(e) => setName(e.target.value)} disabled={isSaving} autoFocus />
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
