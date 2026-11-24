import { Text } from '@fluentui/react-components'
import { useId } from 'react'
import { formatN2, formatSignedN2 } from '../utils/formatters'
import type { MonthResult } from '../utils/monthResult'

interface MonthResultPanelProps {
  totalIncoming: number
  categoryTotalsSum: number
  result: MonthResult
}

export default function MonthResultPanel({ totalIncoming, categoryTotalsSum, result }: MonthResultPanelProps) {
  const headingId = useId()

  return (
    <div className="month-result">
      <Text as="h2" id={headingId} weight="semibold" size={400}>
        Month result
      </Text>
      <table className="month-result__sum" aria-labelledby={headingId}>
        <tbody>
          <tr>
            <th scope="row">Total incoming</th>
            <td>{formatN2(totalIncoming)}</td>
          </tr>
          <tr>
            <th scope="row">Category total</th>
            <td>{formatSignedN2(-categoryTotalsSum)}</td>
          </tr>
        </tbody>
        <tfoot>
          <tr>
            <th scope="row">{result.label}</th>
            <td className={`month-result__value month-result__value--${result.label.toLowerCase()}`}>
              <strong>{formatSignedN2(result.value)}</strong>
            </td>
          </tr>
        </tfoot>
      </table>
    </div>
  )
}
