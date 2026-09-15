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
  Textarea,
} from '@fluentui/react-components'
import type { TaxRuleDto } from '../api/types'
import { useFormPanelStyles } from './formPanelStyles'
import { getErrorMessage } from '../utils/formatters'

const JURISDICTIONS = ['BR', 'UK']
const EVENT_CATEGORIES = ['CapitalGain', 'Dividend', 'Interest', 'SecuritiesLendingIncome']

interface TaxRuleFormDialogProps {
  taxRule: TaxRuleDto | null
  onCancel: () => void
  onSubmit: (
    jurisdiction: string,
    eventCategory: string,
    label: string,
    description: string,
    effectiveFrom: string,
    effectiveTo: string | null,
  ) => Promise<unknown>
}

export default function TaxRuleFormDialog({ taxRule, onCancel, onSubmit }: TaxRuleFormDialogProps) {
  const styles = useFormPanelStyles()
  const isEditing = taxRule !== null
  const [jurisdiction, setJurisdiction] = useState(taxRule?.jurisdiction ?? JURISDICTIONS[0])
  const [eventCategory, setEventCategory] = useState(taxRule?.eventCategory ?? EVENT_CATEGORIES[0])
  const [label, setLabel] = useState(taxRule?.label ?? '')
  const [description, setDescription] = useState(taxRule?.description ?? '')
  const [effectiveFrom, setEffectiveFrom] = useState(taxRule?.effectiveFrom ?? '')
  const [effectiveTo, setEffectiveTo] = useState(taxRule?.effectiveTo ?? '')
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const trimmedLabel = label.trim()
  const validationMessage = trimmedLabel.length === 0
    ? 'Label is required.'
    : effectiveFrom.length === 0
      ? 'Effective From is required.'
      : effectiveTo.length > 0 && effectiveFrom >= effectiveTo
        ? 'Effective From must be before Effective To.'
        : ''
  const canSubmit = validationMessage.length === 0 && !isSaving

  const handleSubmit = async () => {
    if (!canSubmit) return

    setIsSaving(true)
    setError(null)
    try {
      await onSubmit(jurisdiction, eventCategory, trimmedLabel, description, effectiveFrom, effectiveTo === '' ? null : effectiveTo)
      onCancel()
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'The tax rule could not be saved.'))
      setIsSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>{isEditing ? 'Edit Tax Rule' : 'Create Tax Rule'}</DialogTitle>
          <DialogContent>
            <div className={styles.grid}>
              <Field label="Jurisdiction">
                <Select
                  value={jurisdiction}
                  onChange={(e) => setJurisdiction(e.target.value)}
                  disabled={isSaving || isEditing}
                >
                  {JURISDICTIONS.map((j) => (
                    <option key={j} value={j}>
                      {j}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label="Event Category">
                <Select
                  value={eventCategory}
                  onChange={(e) => setEventCategory(e.target.value)}
                  disabled={isSaving || isEditing}
                >
                  {EVENT_CATEGORIES.map((c) => (
                    <option key={c} value={c}>
                      {c}
                    </option>
                  ))}
                </Select>
              </Field>

              <div className={styles.spanTwo}>
                <Field
                  label="Label"
                  required
                  validationState={trimmedLabel.length === 0 ? 'error' : 'none'}
                  validationMessage={trimmedLabel.length === 0 ? 'Label is required.' : ''}
                >
                  <Input value={label} onChange={(e) => setLabel(e.target.value)} disabled={isSaving} autoFocus />
                </Field>
              </div>

              <div className={styles.spanTwo}>
                <Field label="Description">
                  <Textarea value={description} onChange={(e) => setDescription(e.target.value)} disabled={isSaving} />
                </Field>
              </div>

              <Field label="Effective From" required>
                <Input
                  type="date"
                  value={effectiveFrom}
                  onChange={(e) => setEffectiveFrom(e.target.value)}
                  disabled={isSaving}
                />
              </Field>

              <Field label="Effective To">
                <Input
                  type="date"
                  value={effectiveTo}
                  onChange={(e) => setEffectiveTo(e.target.value)}
                  disabled={isSaving}
                />
              </Field>
            </div>

            {validationMessage && trimmedLabel.length > 0 && (
              <MessageBar intent="error">
                <MessageBarBody>{validationMessage}</MessageBarBody>
              </MessageBar>
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
