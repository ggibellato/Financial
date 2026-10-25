import { Fragment } from 'react'
import type { SectionTotalItem } from '../utils/currencyFooterItems'

export default function SectionTotals({ items }: { items: SectionTotalItem[] }) {
  return (
    <p className="monthly-page__section-total">
      {items.map((item, index) => (
        <Fragment key={item.label}>
          {index > 0 && ' · '}
          {item.label}: <strong>{item.value}</strong>
        </Fragment>
      ))}
    </p>
  )
}
