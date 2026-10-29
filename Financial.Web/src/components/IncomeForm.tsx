import { useState } from 'react'
import { Button, Checkbox, Field, Input, MessageBar, MessageBarBody, Select, Spinner, Text } from '@fluentui/react-components'
import type { BankDto, IncomeSourceDto } from '../api/types'
import type { LockedBank } from '../hooks/formSettings'
import { INCOME_SOURCES_WITH_GROSS_VALUE, selectActiveIncomeSources } from '../hooks/useIncomeForm'
import { useFieldError } from '../hooks/useFieldError'
import { selectableBanks } from '../utils/selectableBanks'
import { useFormPanelStyles } from './formPanelStyles'

export type IncomeFormField = 'date' | 'incomeSource' | 'grossValue' | 'netValue' | 'bank' | 'description' | 'splitToReserve'

interface IncomeFormProps {
  isEditing: boolean
  date: string
  incomeSource: string
  grossValue: string
  netValue: string
  bank: string
  description: string
  splitToReserve: boolean
  banks: BankDto[]
  incomeSources: IncomeSourceDto[]
  isSaving: boolean
  lockedBank?: LockedBank
  saveError: string | null
  saveErrorFields: Partial<Record<IncomeFormField, string>>
  onFieldChange: (field: IncomeFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function IncomeForm({
  isEditing,
  date,
  incomeSource,
  grossValue,
  netValue,
  bank,
  description,
  splitToReserve,
  banks,
  incomeSources,
  isSaving,
  lockedBank,
  saveError,
  saveErrorFields,
  onFieldChange,
  onSave,
  onCancel,
}: IncomeFormProps) {
  const styles = useFormPanelStyles()
  const [openedBank] = useState(bank)
  const fieldError = useFieldError(saveErrorFields)
  const activeIncomeSources = selectActiveIncomeSources(incomeSources)
  const showGrossValueField = INCOME_SOURCES_WITH_GROSS_VALUE.includes(
    incomeSources.find((s) => s.id === incomeSource)?.name ?? '',
  )
  const showSplitField = incomeSources.find((s) => s.id === incomeSource)?.potsAccountId != null

  return (
    <form
      className={styles.panel}
      noValidate
      onSubmit={(event) => {
        event.preventDefault()
        onSave()
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset}>
        <Text as="h2" weight="semibold" size={400}>
          {isEditing ? 'Edit Income' : 'New Income'}
        </Text>
        {Object.keys(saveErrorFields).length === 0 && saveError && (
          <MessageBar intent="error">
            <MessageBarBody>{saveError}</MessageBarBody>
          </MessageBar>
        )}

        <div className={styles.grid}>
          <Field
            label="Date"
            required
            validationState={fieldError('date') ? 'error' : 'none'}
            validationMessage={fieldError('date')}
          >
            <Input type="date" value={date} onChange={(e) => onFieldChange('date', e.target.value)} />
          </Field>

          <Field
            label="Source"
            required
            validationState={fieldError('incomeSource') ? 'error' : 'none'}
            validationMessage={fieldError('incomeSource')}
          >
            <Select value={incomeSource} onChange={(e) => onFieldChange('incomeSource', e.target.value)}>
              {activeIncomeSources.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </Select>
          </Field>

          <Field label="Bank">
            {lockedBank ? (
              <Text weight="semibold">{lockedBank.name}</Text>
            ) : (
              <Select value={bank} onChange={(e) => onFieldChange('bank', e.target.value)}>
                <option value="">— No bank —</option>
                {selectableBanks(banks, bank, openedBank).map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))}
              </Select>
            )}
          </Field>

          <Field label="Description">
            <Input name="description" value={description} onChange={(e) => onFieldChange('description', e.target.value)} />
          </Field>

          {showGrossValueField && (
            <Field
              label="Gross Value"
              validationState={fieldError('grossValue') ? 'error' : 'none'}
              validationMessage={fieldError('grossValue')}
            >
              <Input
                type="number"
                step="0.01"
                value={grossValue}
                onChange={(e) => onFieldChange('grossValue', e.target.value)}
              />
            </Field>
          )}

          <Field
            label="Net Value"
            required
            validationState={fieldError('netValue') ? 'error' : 'none'}
            validationMessage={fieldError('netValue')}
          >
            <Input
              type="number"
              step="0.01"
              value={netValue}
              onChange={(e) => onFieldChange('netValue', e.target.value)}
            />
          </Field>

          {showSplitField && (
            <div>
              <Checkbox
                label="Split to reserve"
                checked={splitToReserve}
                onChange={(_, data) => onFieldChange('splitToReserve', data.checked ? 'true' : 'false')}
              />
            </div>
          )}
        </div>

        <div className={styles.actions}>
          <Button type="submit" appearance="primary" disabled={isSaving} icon={isSaving ? <Spinner size="tiny" /> : undefined}>
            {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Add Income'}
          </Button>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}
