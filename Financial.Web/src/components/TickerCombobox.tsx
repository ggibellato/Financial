import { useMemo, useState, type KeyboardEvent, type Ref } from 'react'
import { Combobox, Field, Option, OptionGroup, makeStyles, type ComboboxProps } from '@fluentui/react-components'

const useStyles = makeStyles({
  combobox: {
    minWidth: '10rem',
  },
})

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

// Fluent's Combobox prevents the default of every Enter, so without this the form never submits from the field.
function submitOnEnter(event: KeyboardEvent<HTMLInputElement>) {
  const input = event.currentTarget
  const activeOptionId = input.getAttribute('aria-activedescendant')
  const picksAnOption = input.getAttribute('aria-expanded') === 'true' && !!activeOptionId && !!document.getElementById(activeOptionId)
  if (event.key === 'Enter' && !picksAnOption) input.form?.requestSubmit()
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
  const styles = useStyles()
  const [isOpen, setIsOpen] = useState(false)
  const changeOpen: NonNullable<ComboboxProps['onOpenChange']> = (event, data) => {
    const opensOnSubmit = data.open && 'key' in event && event.key === 'Enter'
    if (!opensOnSubmit) setIsOpen(data.open)
  }
  const shownGroups = useMemo(() => matchingGroups(groups, value), [groups, value])
  return (
    <Field
      label="Ticker"
      required
      hint={hint}
      validationMessage={validationMessage}
    >
      <Combobox
        ref={ref}
        className={styles.combobox}
        freeform
        value={value}
        selectedOptions={value ? [value] : []}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        open={isOpen && shownGroups.length > 0}
        onOpenChange={changeOpen}
        onKeyDown={submitOnEnter}
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
