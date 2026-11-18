import type { UseColumnFiltersResult } from '../../hooks/useColumnFilters'
import ColumnFilterMenu from './ColumnFilterMenu'
import TextFilterMenu from './TextFilterMenu'

export type ColumnFilterKind = 'values' | 'text'

interface ColumnHeaderFilterProps<T> {
  columnKey: string
  label: string
  kind: ColumnFilterKind | undefined
  filters: UseColumnFiltersResult<T>
}

export default function ColumnHeaderFilter<T>({ columnKey, label, kind, filters }: ColumnHeaderFilterProps<T>) {
  if (kind === 'text') {
    return (
      <TextFilterMenu
        columnKey={columnKey}
        label={label}
        text={filters.searchText(columnKey)}
        onTextChange={filters.setSearchText}
        isFiltered={filters.isColumnFiltered(columnKey)}
      />
    )
  }
  if (kind === 'values') {
    return (
      <ColumnFilterMenu
        columnKey={columnKey}
        label={label}
        availableValues={filters.availableValues[columnKey]}
        selectedValues={filters.selectedValues[columnKey]}
        onToggleValue={filters.toggleValue}
        onToggleAll={filters.toggleAll}
        isFiltered={filters.isColumnFiltered(columnKey)}
      />
    )
  }
  return null
}
