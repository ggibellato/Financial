import type { MonthStartColumnDto, MonthStartValueDto } from '../api/types'
import { useVisuallyHiddenStyles } from '../styles/visuallyHidden'
import { formatAmount, formatDayMonthUtc, formatN2 } from '../utils/formatters'
import MissingValue from './MissingValue'

interface MonthStartValueProps {
  value: MonthStartValueDto
  column: MonthStartColumnDto
}

export default function MonthStartValue({ value, column }: MonthStartValueProps) {
  const hidden = useVisuallyHiddenStyles()

  if (value.value == null || value.unitPrice == null || value.priceDate == null) {
    const detail = `No saved price between ${formatDayMonthUtc(column.earliestPriceDate)} and ${formatDayMonthUtc(column.date)}`
    return (
      <span title={detail}>
        <MissingValue label={detail} />
      </span>
    )
  }

  const detail = `${formatAmount(value.unitPrice, value.currency)} saved ${formatDayMonthUtc(value.priceDate)}`
  return (
    <span title={detail}>
      {formatN2(value.value)}
      <span className={hidden.visuallyHidden}>, {detail}</span>
    </span>
  )
}
