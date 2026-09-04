import { Tab, TabList } from '@fluentui/react-components'
import type { SelectTabData, SelectTabEvent } from '@fluentui/react-components'
import './FilterTabList.css'

export interface FilterTabListOption<T extends string> {
  value: T
  label: string
}

interface FilterTabListProps<T extends string> {
  label?: string
  options: readonly FilterTabListOption<T>[]
  selected: T
  onSelect: (value: T) => void
}

export default function FilterTabList<T extends string>({
  label,
  options,
  selected,
  onSelect,
}: FilterTabListProps<T>) {
  const handleTabSelect = (_event: SelectTabEvent, data: SelectTabData) => {
    onSelect(data.value as T)
  }

  return (
    <div className="filter-tab-list">
      {label && <span className="filter-tab-list__label">{label}</span>}
      <TabList selectedValue={selected} onTabSelect={handleTabSelect} size="small" appearance="subtle">
        {options.map((opt) => (
          <Tab key={opt.value} value={opt.value}>
            {opt.label}
          </Tab>
        ))}
      </TabList>
    </div>
  )
}
