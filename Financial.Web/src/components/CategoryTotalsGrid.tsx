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
  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered } =
    useColumnFilters(categoryTotals, FILTER_ACCESSORS)

  return (
    <TotalsGrid
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
      isFiltered={isColumnFiltered('category')}
      rowKey={(c) => c.category}
      footerItems={[{ label: 'Total', value: formatN2(categoryTotalsSum) }]}
    />
  )
}
