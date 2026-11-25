import { useState, type KeyboardEvent } from 'react'
import { makeStyles, type ComboboxProps } from '@fluentui/react-components'

const useStyles = makeStyles({
  combobox: {
    minWidth: '10rem',
  },
})

// Fluent's Combobox prevents the default of every Enter, so without this the form never submits from the field.
function submitOnEnter(event: KeyboardEvent<HTMLInputElement>) {
  const input = event.currentTarget
  const activeOptionId = input.getAttribute('aria-activedescendant')
  const picksAnOption = input.getAttribute('aria-expanded') === 'true' && !!activeOptionId && !!document.getElementById(activeOptionId)
  if (event.key === 'Enter' && !picksAnOption) input.form?.requestSubmit()
}

export function useFreeformCombobox(hasOptions: boolean) {
  const styles = useStyles()
  const [isOpen, setIsOpen] = useState(false)
  const onOpenChange: NonNullable<ComboboxProps['onOpenChange']> = (event, data) => {
    const opensOnSubmit = data.open && 'key' in event && event.key === 'Enter'
    if (!opensOnSubmit) setIsOpen(data.open)
  }
  return { className: styles.combobox, open: isOpen && hasOptions, onOpenChange, onKeyDown: submitOnEnter }
}
