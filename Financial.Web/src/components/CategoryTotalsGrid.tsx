import type { CategoryTotalDto } from '../api/types'
import { formatN2 } from '../utils/formatters'
import TotalsGrid from './TotalsGrid'
import ColumnFilterMenu from './grid/ColumnFilterMenu'
import { useColumnFilters } from '../hooks/useColumnFilters'

interface CategoryTotalsGridProps {
  categoryTotals: CategoryTotalDto[]
  categoryTotalsSum: number
}

const FILTER_ACCESSORS = {
  category: (c: CategoryTotalDto) => c.category,
}

export default function CategoryTotalsGrid({ categoryTotals, categoryTotalsSum }: CategoryTotalsGridProps) {
  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered, filterSummary } =
    useColumnFilters(categoryTotals, FILTER_ACCESSORS)

  return (
    <TotalsGrid
      label="Category totals"
      columns={[
        {
          key: 'category',
          header: 'Category',
          render: (c: CategoryTotalDto) => c.category,
          sortAccessor: (c: CategoryTotalDto) => c.category,
          filterSlot: (
            <ColumnFilterMenu
              columnKey="category"
              label="Category"
              availableValues={availableValues.category}
              selectedValues={selectedValues.category}
              onToggleValue={toggleValue}
              onToggleAll={toggleAll}
              isFiltered={isColumnFiltered('category')}
            />
          ),
        },
        {
          key: 'total',
          header: 'Total',
          numeric: true,
          render: (c: CategoryTotalDto) => formatN2(c.totalValue),
          sortAccessor: (c: CategoryTotalDto) => c.totalValue,
        },
      ]}
      rows={filteredRows}
      filter={filterSummary}
      rowKey={(c) => c.category}
      emptyMessage="No expenses recorded for this month"
      footerItems={[{ label: 'Total', value: formatN2(categoryTotalsSum) }]}
    />
  )
}
