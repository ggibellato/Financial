import { CHART_TEXT_STYLE } from '../styles/chartText'

const LABEL_GAP = 4
const DIGIT_WIDTH_EM = 0.6

interface ChartValueLabelProps {
  x?: number | string
  y?: number | string
  width?: number | string
  height?: number | string
  value?: unknown
  formatValue: (value: number) => string
}

function estimatedLabelWidth(text: string): number {
  return text.length * CHART_TEXT_STYLE.fontSize * DIGIT_WIDTH_EM
}

export default function ChartValueLabel({ x, y, width, height, value, formatValue }: ChartValueLabelProps) {
  if (typeof value !== 'number' || typeof x !== 'number' || typeof y !== 'number' || typeof width !== 'number') return null
  const text = formatValue(value)
  if (!text || width < estimatedLabelWidth(text)) return null

  const isBelowAxis = typeof height === 'number' && height < 0
  return (
    <text
      x={x + width / 2}
      y={isBelowAxis ? y + LABEL_GAP : y - LABEL_GAP}
      textAnchor="middle"
      dominantBaseline={isBelowAxis ? 'hanging' : undefined}
      style={CHART_TEXT_STYLE}
    >
      {text}
    </text>
  )
}
