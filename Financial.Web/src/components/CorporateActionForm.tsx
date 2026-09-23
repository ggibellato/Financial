import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text, Textarea } from '@fluentui/react-components'
import type { CorporateActionFormField } from '../hooks/useCorporateActions'
import { useFieldError } from '../hooks/useFieldError'
import { useFormPanelStyles } from './formPanelStyles'
import './CorporateActionForm.css'

const CORPORATE_ACTION_TYPE_OPTIONS = [{ value: 'Split', label: 'Split / Reverse Split' }]

interface CorporateActionFormProps {
  editingId: string | null
  formEffectiveDate: string
  formType: string
  formRatioNumerator: string
  formRatioDenominator: string
  formNote: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<CorporateActionFormField, string>>
  onFieldChange: (field: CorporateActionFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function CorporateActionForm({
  editingId,
  formEffectiveDate,
  formType,
  formRatioNumerator,
  formRatioDenominator,
  formNote,
  isSaving,
  saveError,
  saveErrorFields,
  onFieldChange,
  onSave,
  onCancel,
}: CorporateActionFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(saveErrorFields)
  const title = editingId ? 'Edit corporate action' : 'New corporate action'
  const confirmLabel = isSaving ? 'Saving...' : editingId ? 'Save' : 'Add corporate action'

  return (
    <div className={styles.panel}>
      <Text as="h2" weight="semibold" size={400}>
        {title}
      </Text>

      <div className={styles.grid}>
        <Field
          label="Effective Date"
          required
          validationState={fieldError('formEffectiveDate') ? 'error' : 'none'}
          validationMessage={fieldError('formEffectiveDate')}
        >
          <Input
            type="date"
            value={formEffectiveDate}
            onChange={(e) => onFieldChange('formEffectiveDate', e.target.value)}
          />
        </Field>

        <Field label="Type">
          <Select value={formType} onChange={(e) => onFieldChange('formType', e.target.value)}>
            {CORPORATE_ACTION_TYPE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </Field>

        {formType === 'Split' && (
          <>
            <Field label="New units" required validationState={fieldError('formRatio') ? 'error' : 'none'}>
              <Input
                type="number"
                step="0.0001"
                min="0"
                value={formRatioNumerator}
                onChange={(e) => onFieldChange('formRatioNumerator', e.target.value)}
              />
            </Field>

            <Field
              label="Old units"
              required
              validationState={fieldError('formRatio') ? 'error' : 'none'}
              validationMessage={fieldError('formRatio')}
            >
              <Input
                type="number"
                step="0.0001"
                min="0"
                value={formRatioDenominator}
                onChange={(e) => onFieldChange('formRatioDenominator', e.target.value)}
              />
            </Field>

            <div className={styles.spanTwo}>
              <Text size={200} className="corporate-action-form__ratio-hint">
                e.g. 2 new for 1 old is a 2-for-1 split; 1 new for 10 old is a 1-for-10 reverse split.
              </Text>
            </div>

            <div className={styles.spanTwo}>
              <Field label="Note">
                <Textarea value={formNote} maxLength={500} onChange={(e) => onFieldChange('formNote', e.target.value)} />
              </Field>
            </div>
          </>
        )}
      </div>

      <div className={styles.actions}>
        <Button appearance="primary" disabled={isSaving} onClick={onSave}>
          {confirmLabel}
        </Button>
        <Button appearance="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </div>

      {Object.keys(saveErrorFields).length === 0 && saveError && (
        <MessageBar intent="error">
          <MessageBarBody>{saveError}</MessageBarBody>
        </MessageBar>
      )}
    </div>
  )
}
