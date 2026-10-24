import { Field, Input, Select, Text } from '@fluentui/react-components'
import type { BankDto, CategoryDto } from '../api/types'
import type { LockedBank } from '../hooks/formSettings'
import type { BillExpenseField, BillExpenseValues } from './billExpenseValues'
import { useFormPanelStyles } from './formPanelStyles'

interface BillExpenseFieldsProps {
  values: BillExpenseValues
  banks: BankDto[]
  categories: CategoryDto[]
  disabled?: boolean
  lockedBank?: LockedBank
  onFieldChange: (field: BillExpenseField, value: string) => void
}

export default function BillExpenseFields({
  values,
  banks,
  categories,
  disabled,
  lockedBank,
  onFieldChange,
}: BillExpenseFieldsProps) {
  const styles = useFormPanelStyles()

  return (
    <>
      <Field label="Date" required>
        <Input type="date" value={values.date} onChange={(e) => onFieldChange('date', e.target.value)} disabled={disabled} />
      </Field>

      <Field label="Description" required className={styles.spanTwo}>
        <Input value={values.description} onChange={(e) => onFieldChange('description', e.target.value)} disabled={disabled} />
      </Field>

      <Field label="Value" required>
        <Input
          type="number"
          step="0.01"
          value={values.value}
          onChange={(e) => onFieldChange('value', e.target.value)}
          disabled={disabled}
        />
      </Field>

      {lockedBank ? (
        <Field label="Bank">
          <Text weight="semibold">{lockedBank.name}</Text>
        </Field>
      ) : (
        <Field label="Bank" required>
          <Select value={values.bankId} onChange={(e) => onFieldChange('bankId', e.target.value)} disabled={disabled}>
            <option value="">Select bank…</option>
            {banks.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name}
              </option>
            ))}
          </Select>
        </Field>
      )}

      <Field label="Category" required>
        <Select value={values.categoryId} onChange={(e) => onFieldChange('categoryId', e.target.value)} disabled={disabled}>
          <option value="">Select category…</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </Select>
      </Field>
    </>
  )
}
