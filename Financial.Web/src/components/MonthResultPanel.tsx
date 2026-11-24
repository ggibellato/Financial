import { formatN2, formatSignedN2 } from '../utils/formatters'
import type { MonthResult } from '../utils/monthResult'
import CalculationCard, { type CalculationResult } from './CalculationCard'

interface MonthResultPanelProps {
  totalIncoming: number
  categoryTotalsSum: number
  result: MonthResult
}

function resultTone(value: number): CalculationResult['tone'] {
  if (value > 0) return 'positive'
  if (value < 0) return 'negative'
  return undefined
}

export default function MonthResultPanel({ totalIncoming, categoryTotalsSum, result }: MonthResultPanelProps) {
  return (
    <CalculationCard
      heading="Month result"
      className="month-result"
      lines={[
        { label: 'Total incoming', value: formatN2(totalIncoming) },
        { label: 'Category total', value: formatSignedN2(-categoryTotalsSum) },
      ]}
      result={{ label: result.label, value: formatSignedN2(result.value), tone: resultTone(result.value) }}
    />
  )
}
