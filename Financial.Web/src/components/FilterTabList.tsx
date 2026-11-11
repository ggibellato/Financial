import { useId } from 'react'
import { Tab, TabList } from '@fluentui/react-components'
import type { SelectTabData, SelectTabEvent } from '@fluentui/react-components'
import './FilterTabList.css'

export interface FilterTabListOption<T extends string | number> {
  value: T
  label: string
}

type FilterTabListName = { label: string; 'aria-label'?: never } | { label?: never; 'aria-label': string }

type FilterTabListProps<T extends string | number> = FilterTabListName & {
  options: readonly FilterTabListOption<T>[]
  selected: T
  onSelect: (value: T) => void
}

export default function FilterTabList<T extends string | number>({
  label,
  'aria-label': ariaLabel,
  options,
  selected,
  onSelect,
}: FilterTabListProps<T>) {
  const labelId = useId()
  const handleTabSelect = (_event: SelectTabEvent, data: SelectTabData) => {
    onSelect(data.value as T)
  }

  return (
    <div className="filter-tab-list">
      {label && (
        <span id={labelId} className="filter-tab-list__label">
          {label}
        </span>
      )}
      <TabList
        selectedValue={selected}
        onTabSelect={handleTabSelect}
        size="small"
        appearance="subtle"
        aria-label={ariaLabel}
        aria-labelledby={label ? labelId : undefined}
      >
        {options.map((opt) => (
          <Tab key={opt.value} value={opt.value}>
            {opt.label}
          </Tab>
        ))}
      </TabList>
    </div>
  )
}
