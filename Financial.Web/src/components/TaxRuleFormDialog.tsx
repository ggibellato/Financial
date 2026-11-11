import { useState } from 'react'
import { Field, Input, Select, Textarea } from '@fluentui/react-components'
import type { TaxRuleDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import ReadOnlyInput from './ReadOnlyInput'
import { useFormPanelStyles } from './formPanelStyles'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { useEntityForm } from '../hooks/useEntityForm'

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
  const { isSaving, error, submit } = useDialogSubmit('The tax rule could not be saved.')
  const form = useEntityForm({ jurisdiction, eventCategory, label, description, effectiveFrom, effectiveTo })

  const trimmedLabel = label.trim()
  const labelError = trimmedLabel.length === 0 ? 'Label is required.' : ''
  const effectiveFromError =
    effectiveFrom.length === 0
      ? 'Effective from is required.'
      : effectiveTo.length > 0 && effectiveFrom >= effectiveTo
        ? 'Effective from must be before effective to.'
        : ''
  const canSubmit = !labelError && !effectiveFromError

  const handleSubmit = () => {
    form.revealValidation()
    if (!canSubmit) return
    void submit(async () => {
      await onSubmit(jurisdiction, eventCategory, trimmedLabel, description, effectiveFrom, effectiveTo === '' ? null : effectiveTo)
      onCancel()
    })
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit tax rule' : 'New tax rule'}
      saveLabel={isEditing ? 'Save' : 'Add tax rule'}
      error={error}
      isSaving={isSaving}
      isDirty={form.isDirty}
      onSave={handleSubmit}
      onCancel={onCancel}
    >
      <div className={styles.grid}>
        <Field label="Jurisdiction">
          {isEditing ? (
            <ReadOnlyInput value={jurisdiction} />
          ) : (
            <Select value={jurisdiction} onChange={(e) => setJurisdiction(e.target.value)}>
              {JURISDICTIONS.map((j) => (
                <option key={j} value={j}>
                  {j}
                </option>
              ))}
            </Select>
          )}
        </Field>

        <Field label="Event category">
          {isEditing ? (
            <ReadOnlyInput value={eventCategory} />
          ) : (
            <Select value={eventCategory} onChange={(e) => setEventCategory(e.target.value)}>
              {EVENT_CATEGORIES.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </Select>
          )}
        </Field>

        <div className={styles.spanTwo}>
          <Field
            label="Label"
            required
            {...form.fieldValidation(labelError)}
          >
            <Input value={label} onChange={(e) => setLabel(e.target.value)} />
          </Field>
        </div>

        <div className={styles.spanTwo}>
          <Field label="Description">
            <Textarea value={description} onChange={(e) => setDescription(e.target.value)} />
          </Field>
        </div>

        <Field
          label="Effective from"
          required
          {...form.fieldValidation(effectiveFromError)}
        >
          <Input
            type="date"
            value={effectiveFrom}
            onChange={(e) => setEffectiveFrom(e.target.value)}
          />
        </Field>

        <Field label="Effective to">
          <Input
            type="date"
            value={effectiveTo}
            onChange={(e) => setEffectiveTo(e.target.value)}
          />
        </Field>
      </div>
    </EntityFormDialog>
  )
}
