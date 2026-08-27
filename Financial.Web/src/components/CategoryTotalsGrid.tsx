import type { CategoryTotalDto } from '../api/types'
import { formatN2 } from '../utils/formatters'
import TotalsGrid from './TotalsGrid'

interface CategoryTotalsGridProps {
  categoryTotals: CategoryTotalDto[]
  categoryTotalsSum: number
}

export default function CategoryTotalsGrid({ categoryTotals, categoryTotalsSum }: CategoryTotalsGridProps) {
  return (
    <TotalsGrid
      columns={[
        {
          key: 'category',
          header: 'Category',
          render: (c: CategoryTotalDto) => c.category,
          sortAccessor: (c: CategoryTotalDto) => c.category,
        },
        {
          key: 'total',
          header: 'Total',
          numeric: true,
          render: (c: CategoryTotalDto) => formatN2(c.totalValue),
          sortAccessor: (c: CategoryTotalDto) => c.totalValue,
        },
      ]}
      rows={categoryTotals}
      rowKey={(c) => c.category}
      footerItems={[{ label: 'Total', value: formatN2(categoryTotalsSum) }]}
    />
  )
}
