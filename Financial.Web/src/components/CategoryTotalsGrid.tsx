import type { CategoryTotalDto } from '../api/types'
import { formatN2 } from '../utils/formatters'

interface CategoryTotalsGridProps {
  categoryTotals: CategoryTotalDto[]
  categoryTotalsSum: number
}

export default function CategoryTotalsGrid({ categoryTotals, categoryTotalsSum }: CategoryTotalsGridProps) {
  return (
    <section className="monthly-page__section monthly-page__section--grid">
      <div className="monthly-page__table-scroll">
        <table className="monthly-page__table data-table">
          <thead>
            <tr>
              <th>Category</th>
              <th className="data-table__col--numeric">Total</th>
            </tr>
          </thead>
          <tbody>
            {categoryTotals.map((c) => (
              <tr key={c.category}>
                <td>{c.category}</td>
                <td className="data-table__col--numeric">{formatN2(c.totalValue)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="monthly-page__section-total">
        Total: <strong>{formatN2(categoryTotalsSum)}</strong>
      </p>
    </section>
  )
}
