import { Text } from '@fluentui/react-components'
import { type ReactNode, useId } from 'react'

export interface CalculationLine {
  label: string
  value: ReactNode
}

export interface CalculationResult extends CalculationLine {
  tone?: 'positive' | 'negative'
}

interface CalculationCardProps {
  heading: string
  lines: CalculationLine[]
  result: CalculationResult
  className?: string
  children?: ReactNode
}

export default function CalculationCard({ heading, lines, result, className, children }: CalculationCardProps) {
  const headingId = useId()

  return (
    <div className={className ? `calculation-card ${className}` : 'calculation-card'}>
      <Text as="h2" id={headingId} weight="semibold" size={400}>
        {heading}
      </Text>
      <table className="calculation-card__sum" aria-labelledby={headingId}>
        <tbody>
          {lines.map((line) => (
            <tr key={line.label}>
              <th scope="row">{line.label}</th>
              <td>{line.value}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <th scope="row">{result.label}</th>
            <td className={result.tone ? `calculation-card__value--${result.tone}` : undefined}>
              <strong>{result.value}</strong>
            </td>
          </tr>
        </tfoot>
      </table>
      {children}
    </div>
  )
}
