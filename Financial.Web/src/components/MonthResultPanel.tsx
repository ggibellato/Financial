import { formatN2, formatSignedN2 } from '../utils/formatters'
import type { MonthResult } from '../utils/monthResult'
import CalculationCard from './CalculationCard'

interface MonthResultPanelProps {
  totalIncoming: number
  categoryTotalsSum: number
  result: MonthResult
}

const RESULT_TONES = { Surplus: 'positive', Deficit: 'negative', Balanced: undefined } as const

export default function MonthResultPanel({ totalIncoming, categoryTotalsSum, result }: MonthResultPanelProps) {
  return (
    <CalculationCard
      heading="Month result"
      className="month-result"
      lines={[
        { label: 'Total incoming', value: formatN2(totalIncoming) },
        { label: 'Category total', value: formatSignedN2(-categoryTotalsSum) },
      ]}
      result={{ label: result.label, value: formatSignedN2(result.value), tone: RESULT_TONES[result.label] }}
    />
  )
}
