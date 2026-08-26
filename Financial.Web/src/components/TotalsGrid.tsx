import { Fragment, type ReactNode } from 'react'

interface TotalsGridColumn<T> {
  key: string
  header: string
  numeric?: boolean
  render: (row: T) => ReactNode
}

interface TotalsGridFooterItem {
  label: string
  value: ReactNode
}

interface TotalsGridProps<T> {
  columns: TotalsGridColumn<T>[]
  rows: T[]
  rowKey: (row: T) => string
  footerItems: TotalsGridFooterItem[]
}

export default function TotalsGrid<T>({ columns, rows, rowKey, footerItems }: TotalsGridProps<T>) {
  return (
    <section className="monthly-page__section monthly-page__section--grid">
      <div className="monthly-page__table-scroll">
        <table className="monthly-page__table data-table">
          <thead>
            <tr>
              {columns.map((col) => (
                <th key={col.key} className={col.numeric ? 'data-table__col--numeric' : undefined}>
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={rowKey(row)}>
                {columns.map((col) => (
                  <td key={col.key} className={col.numeric ? 'data-table__col--numeric' : undefined}>
                    {col.render(row)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="monthly-page__section-total">
        {footerItems.map((item, index) => (
          <Fragment key={item.label}>
            {index > 0 && ' · '}
            {item.label}: <strong>{item.value}</strong>
          </Fragment>
        ))}
      </p>
    </section>
  )
}
