import { useMemo, type Ref } from 'react'
import { Combobox, Field, Option, OptionGroup } from '@fluentui/react-components'
import { useFreeformCombobox } from '../hooks/useFreeformCombobox'

export interface TickerGroup {
  label: string
  tickers: string[]
}

interface TickerComboboxProps {
  groups: TickerGroup[]
  value: string
  onChange: (ticker: string) => void
  placeholder?: string
  hint?: string
  validationMessage?: string
  ref?: Ref<HTMLInputElement>
}

function matchingGroups(groups: TickerGroup[], value: string): TickerGroup[] {
  const query = value.trim().toUpperCase()
  const isChosenTicker = groups.some((group) => group.tickers.includes(query))
  if (!query || isChosenTicker) return groups
  return groups
    .map((group) => ({ ...group, tickers: group.tickers.filter((ticker) => ticker.startsWith(query)) }))
    .filter((group) => group.tickers.length > 0)
}

export default function TickerCombobox({
  groups,
  value,
  onChange,
  placeholder,
  hint,
  validationMessage,
  ref,
}: TickerComboboxProps) {
  const shownGroups = useMemo(() => matchingGroups(groups, value), [groups, value])
  const freeform = useFreeformCombobox(shownGroups.length > 0)
  return (
    <Field
      label="Ticker"
      required
      hint={hint}
      validationMessage={validationMessage}
    >
      <Combobox
        ref={ref}
        freeform
        value={value}
        selectedOptions={value ? [value] : []}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        {...freeform}
        onOptionSelect={(_event, data) => {
          if (data.optionValue !== undefined) onChange(data.optionValue)
        }}
      >
        {shownGroups.map((group) => (
          <OptionGroup key={group.label} label={group.label}>
            {group.tickers.map((ticker) => (
              <Option key={ticker} value={ticker}>
                {ticker}
              </Option>
            ))}
          </OptionGroup>
        ))}
      </Combobox>
    </Field>
  )
}
