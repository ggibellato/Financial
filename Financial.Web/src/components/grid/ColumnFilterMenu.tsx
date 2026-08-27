import { useState } from 'react'
import { Button, Checkbox, Popover, PopoverSurface, PopoverTrigger, SearchBox } from '@fluentui/react-components'
import { FilterFilled, FilterRegular } from '@fluentui/react-icons'
import './ColumnFilterMenu.css'

const SEARCH_BOX_THRESHOLD = 10

interface ColumnFilterMenuProps {
  columnKey: string
  label: string
  availableValues: string[]
  selectedValues: Set<string> | undefined
  onToggleValue: (columnKey: string, value: string) => void
  onToggleAll: (columnKey: string) => void
  isFiltered: boolean
}

export default function ColumnFilterMenu({
  columnKey,
  label,
  availableValues,
  selectedValues,
  onToggleValue,
  onToggleAll,
  isFiltered,
}: ColumnFilterMenuProps) {
  const [searchText, setSearchText] = useState('')

  const isChecked = (value: string) => selectedValues === undefined || selectedValues.has(value)
  const allChecked = availableValues.every((value) => isChecked(value))
  const visibleValues = availableValues.filter((value) => value.toLowerCase().includes(searchText.toLowerCase()))

  return (
    <Popover positioning="below-end">
      <PopoverTrigger disableButtonEnhancement>
        <Button
          appearance="subtle"
          size="small"
          className={`column-filter-menu__trigger${isFiltered ? ' column-filter-menu__trigger--active' : ''}`}
          icon={isFiltered ? <FilterFilled /> : <FilterRegular />}
          aria-label={`Filter by ${label}`}
        />
      </PopoverTrigger>
      <PopoverSurface className="column-filter-menu__surface">
        {availableValues.length > SEARCH_BOX_THRESHOLD && (
          <SearchBox
            className="column-filter-menu__search"
            placeholder={`Search ${label}`}
            value={searchText}
            onChange={(_, data) => setSearchText(data.value)}
          />
        )}
        <div className="column-filter-menu__list">
          <Checkbox label="(All)" checked={allChecked} onChange={() => onToggleAll(columnKey)} />
          {visibleValues.map((value) => (
            <Checkbox key={value} label={value} checked={isChecked(value)} onChange={() => onToggleValue(columnKey, value)} />
          ))}
        </div>
      </PopoverSurface>
    </Popover>
  )
}
