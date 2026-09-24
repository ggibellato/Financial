import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text, Textarea } from '@fluentui/react-components'
import type { CorporateActionFormField } from '../hooks/useCorporateActions'
import { useFieldError } from '../hooks/useFieldError'
import { useFormPanelStyles } from './formPanelStyles'
import TargetAssetPicker from './TargetAssetPicker'
import type { TargetAssetPickerValue } from './targetAssetPickerValue'
import { formatN2, parseValidatedNumber } from '../utils/formatters'
import './CorporateActionForm.css'

const CORPORATE_ACTION_TYPE_OPTIONS = [
  { value: 'Split', label: 'Split / Reverse Split' },
  { value: 'Merger', label: 'Merger' },
]

interface CorporateActionFormProps {
  editingId: string | null
  formEffectiveDate: string
  formType: string
  formRatioNumerator: string
  formRatioDenominator: string
  formNote: string
  formStep: 'fields' | 'confirm'
  formTargetAsset: TargetAssetPickerValue
  formExchangeRatio: string
  formCashInLieu: string
  sourceAssetName: string
  sourceQuantity: number
  sourceCostBasis: number
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<CorporateActionFormField, string>>
  onFieldChange: (field: CorporateActionFormField, value: string) => void
  onTargetAssetChange: (value: TargetAssetPickerValue) => void
  onAdvanceToConfirm: () => void
  onBackToFields: () => void
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
  formStep,
  formTargetAsset,
  formExchangeRatio,
  formCashInLieu,
  sourceAssetName,
  sourceQuantity,
  sourceCostBasis,
  isSaving,
  saveError,
  saveErrorFields,
  onFieldChange,
  onTargetAssetChange,
  onAdvanceToConfirm,
  onBackToFields,
  onSave,
  onCancel,
}: CorporateActionFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(saveErrorFields)
  const title = editingId ? 'Edit corporate action' : 'New corporate action'
  const isMergerFieldsStep = formType === 'Merger' && formStep === 'fields'
  const isMergerConfirmStep = formType === 'Merger' && formStep === 'confirm'
  const confirmLabel = isSaving
    ? 'Saving...'
    : isMergerFieldsStep
      ? 'Continue'
      : isMergerConfirmStep
        ? 'Confirm & Save'
        : editingId
          ? 'Save'
          : 'Add corporate action'
  const exchangeRatioNumber = parseValidatedNumber(formExchangeRatio, { min: 0 }) ?? 0
  const targetUnits = sourceQuantity * exchangeRatioNumber

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
          <Select
            value={formType}
            disabled={isMergerConfirmStep || isSaving}
            onChange={(e) => onFieldChange('formType', e.target.value)}
          >
            {CORPORATE_ACTION_TYPE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </Field>

        {formType === 'Split' && (
          <>
            <Field
              label="New units"
              required
              validationState={fieldError('formRatio') ? 'error' : 'none'}
              validationMessage={fieldError('formRatio')}
            >
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
          </>
        )}

        {isMergerFieldsStep && (
          <>
            <Field label="Source Asset">
              <Input value={sourceAssetName} disabled readOnly />
            </Field>

            {editingId ? (
              <Field label="Target Asset">
                <Input value={formTargetAsset.assetName} disabled readOnly />
              </Field>
            ) : (
              <TargetAssetPicker
                label="Target Asset"
                value={formTargetAsset}
                onChange={onTargetAssetChange}
                disabled={isSaving}
                nameError={fieldError('formTargetAsset')}
              />
            )}

            <Field
              label="Exchange Ratio"
              required
              validationState={fieldError('formExchangeRatio') ? 'error' : 'none'}
              validationMessage={fieldError('formExchangeRatio')}
            >
              <Input
                type="number"
                step="0.0001"
                min="0"
                value={formExchangeRatio}
                onChange={(e) => onFieldChange('formExchangeRatio', e.target.value)}
              />
            </Field>

            <Field
              label="Cash-in-Lieu Amount"
              validationState={fieldError('formCashInLieu') ? 'error' : 'none'}
              validationMessage={fieldError('formCashInLieu')}
            >
              <Input
                type="number"
                step="0.01"
                min="0"
                value={formCashInLieu}
                onChange={(e) => onFieldChange('formCashInLieu', e.target.value)}
              />
            </Field>
          </>
        )}

        {formStep === 'fields' && (
          <div className={styles.spanTwo}>
            <Field label="Note">
              <Textarea value={formNote} maxLength={500} onChange={(e) => onFieldChange('formNote', e.target.value)} />
            </Field>
          </div>
        )}

        {isMergerConfirmStep && (
          <div className={styles.spanTwo}>
            <MessageBar intent="warning">
              <MessageBarBody>
                Your position in {sourceAssetName} ({formatN2(sourceQuantity)} units) will close and convert into{' '}
                <strong>{formatN2(targetUnits)} units</strong> of {formTargetAsset.assetName}, carrying over{' '}
                <strong>{formatN2(sourceCostBasis)}</strong> of cost basis.
              </MessageBarBody>
            </MessageBar>
          </div>
        )}
      </div>

      <div className={styles.actions}>
        <Button appearance="primary" disabled={isSaving} onClick={isMergerFieldsStep ? onAdvanceToConfirm : onSave}>
          {confirmLabel}
        </Button>
        {isMergerConfirmStep ? (
          <Button appearance="secondary" disabled={isSaving} onClick={onBackToFields}>
            Back
          </Button>
        ) : (
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        )}
      </div>

      {Object.keys(saveErrorFields).length === 0 && saveError && (
        <MessageBar intent="error">
          <MessageBarBody>{saveError}</MessageBarBody>
        </MessageBar>
      )}
    </div>
  )
}
