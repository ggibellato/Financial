import { Field, Select } from '@fluentui/react-components'
import type { PotBalanceDto } from '../api/types'
import { inactiveMarked } from '../utils/selectableBanks'

interface PotFieldProps {
  value: string
  pots: readonly PotBalanceDto[] | null
  hasLoadError: boolean
  error: string | null
  onChange: (potId: string) => void
}

function potHint(pots: readonly PotBalanceDto[] | null, hasLoadError: boolean): string | undefined {
  if (pots === null) return hasLoadError ? undefined : 'Loading pots…'
  return pots.length === 0 ? 'This account has no pots. Add one with Manage pots.' : undefined
}

export default function PotField({ value, pots, hasLoadError, error, onChange }: PotFieldProps) {
  return (
    <Field label="Pot" required hint={potHint(pots, hasLoadError)} validationState={error ? 'error' : 'none'} validationMessage={error}>
      <Select value={value} onChange={(event) => onChange(event.target.value)}>
        {value === '' && <option value="">{pots === null && !hasLoadError ? 'Loading pots…' : 'Select a pot'}</option>}
        {(pots ?? []).map((pot) => (
          <option key={pot.id} value={pot.id}>
            {inactiveMarked(pot)}
          </option>
        ))}
      </Select>
    </Field>
  )
}
