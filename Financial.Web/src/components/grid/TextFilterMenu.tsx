import { Field, SearchBox } from '@fluentui/react-components'
import ColumnFilterPopover from './ColumnFilterPopover'

interface TextFilterMenuProps {
  columnKey: string
  label: string
  text: string
  onTextChange: (columnKey: string, text: string) => void
  isFiltered: boolean
}

export default function TextFilterMenu({ columnKey, label, text, onTextChange, isFiltered }: TextFilterMenuProps) {
  return (
    <ColumnFilterPopover label={label} isFiltered={isFiltered}>
      <Field label={`${label} contains`}>
        <SearchBox className="column-filter-menu__text" autoFocus value={text} onChange={(_, data) => onTextChange(columnKey, data.value)} />
      </Field>
    </ColumnFilterPopover>
  )
}
