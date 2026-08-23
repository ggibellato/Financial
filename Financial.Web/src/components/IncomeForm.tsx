import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text, makeStyles, tokens } from '@fluentui/react-components'
import type { BankDto, IncomeSourceDto } from '../api/types'
import { INCOME_SOURCES_WITH_GROSS_VALUE, selectActiveIncomeSources } from '../hooks/useIncomeForm'

export type IncomeFormField = 'date' | 'incomeSource' | 'grossValue' | 'netValue' | 'bank' | 'description'

interface IncomeFormProps {
  isEditing: boolean
  date: string
  incomeSource: string
  grossValue: string
  netValue: string
  bank: string
  description: string
  banks: BankDto[]
  incomeSources: IncomeSourceDto[]
  isSaving: boolean
  saveError: string | null
  onFieldChange: (field: IncomeFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

const useStyles = makeStyles({
  panel: {
    flexShrink: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    border: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(4, minmax(0, 1fr))',
    gap: tokens.spacingVerticalM,
    '@media (max-width: 1023px)': {
      gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
    },
    '@media (max-width: 639px)': {
      gridTemplateColumns: '1fr',
    },
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
})

export default function IncomeForm({
  isEditing,
  date,
  incomeSource,
  grossValue,
  netValue,
  bank,
  description,
  banks,
  incomeSources,
  isSaving,
  saveError,
  onFieldChange,
  onSave,
  onCancel,
}: IncomeFormProps) {
  const styles = useStyles()
  const activeIncomeSources = selectActiveIncomeSources(incomeSources)
  const showGrossValueField = INCOME_SOURCES_WITH_GROSS_VALUE.includes(
    incomeSources.find((s) => s.id === incomeSource)?.name ?? '',
  )

  return (
    <div className={styles.panel}>
      <Text as="h2" weight="semibold" size={400}>
        {isEditing ? 'Edit Income' : 'New Income'}
      </Text>

      <div className={styles.grid}>
        <Field label="Date">
          <Input type="date" value={date} onChange={(e) => onFieldChange('date', e.target.value)} />
        </Field>

        <Field label="Source">
          <Select value={incomeSource} onChange={(e) => onFieldChange('incomeSource', e.target.value)}>
            {activeIncomeSources.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </Select>
        </Field>

        {showGrossValueField && (
          <Field label="Gross Value">
            <Input
              type="number"
              step="0.01"
              value={grossValue}
              onChange={(e) => onFieldChange('grossValue', e.target.value)}
            />
          </Field>
        )}

        <Field label="Net Value">
          <Input
            type="number"
            step="0.01"
            value={netValue}
            onChange={(e) => onFieldChange('netValue', e.target.value)}
          />
        </Field>

        <Field label="Bank">
          <Select value={bank} onChange={(e) => onFieldChange('bank', e.target.value)}>
            <option value="">— No bank —</option>
            {banks.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field label="Description">
          <Input value={description} onChange={(e) => onFieldChange('description', e.target.value)} />
        </Field>
      </div>

      <div className={styles.actions}>
        <Button appearance="primary" disabled={isSaving} onClick={onSave}>
          {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Add Income'}
        </Button>
        <Button appearance="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </div>

      {saveError && (
        <MessageBar intent="error">
          <MessageBarBody>{saveError}</MessageBarBody>
        </MessageBar>
      )}
    </div>
  )
}
