import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { useBrokerBreakdown } from '../hooks/useBrokerBreakdown'
import { formatN2 } from '../utils/formatters'
import './BrokerBreakdownCharts.css'

// Validated categorical palette (fixed hue order, CVD-safe adjacency).
const CATEGORICAL_PALETTE = [
  '#2a78d6', // blue
  '#1baf7a', // aqua
  '#eda100', // yellow
  '#008300', // green
  '#4a3aa7', // violet
  '#e34948', // red
  '#e87ba4', // magenta
  '#eb6834', // orange
]

function formatPercent(value: number): string {
  return `${value.toFixed(1)}%`
}

interface PieSliceDatum {
  name: string
  value: number
  percent: number
}

function buildPieSliceData(items: { name: string; value: number }[]): PieSliceDatum[] {
  const total = items.reduce((sum, item) => sum + item.value, 0)
  return items.map((item) => ({
    ...item,
    percent: total > 0 ? item.value / total : 0,
  }))
}

interface PieTooltipContentProps {
  name: string
  value: number
  percent: number
}

function PieTooltipContent({ name, value, percent }: PieTooltipContentProps) {
  return (
    <div className="broker-breakdown__tooltip">
      <div className="broker-breakdown__tooltip-name">{name}</div>
      <div className="broker-breakdown__tooltip-value">{formatN2(value)}</div>
      <div className="broker-breakdown__tooltip-percent">{formatPercent(percent * 100)}</div>
    </div>
  )
}

// recharts' Pie tooltip payload entry doesn't carry `percent` itself (that's
// only attached to the internal sector geometry, not the tooltip payload) —
// but it does carry the original data object under `.payload`, which is where
// our own pre-computed `percent` (see buildPieSliceData) actually surfaces.
function readPieTooltipEntry(entry: unknown): PieTooltipContentProps {
  const record = entry as { payload?: { name?: unknown; value?: unknown; percent?: unknown } }
  const source = record.payload ?? {}
  return {
    name: typeof source.name === 'string' ? source.name : '',
    value: typeof source.value === 'number' ? source.value : 0,
    percent: typeof source.percent === 'number' ? source.percent : 0,
  }
}

interface BreakdownPieProps {
  title: string
  data: PieSliceDatum[]
}

function BreakdownPie({ title, data }: BreakdownPieProps) {
  return (
    <div className="broker-breakdown__chart">
      <h3 className="broker-breakdown__chart-title">{title}</h3>
      <ResponsiveContainer width="100%" height={280}>
        <PieChart>
          <Pie data={data} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={90}>
            {data.map((entry, index) => (
              <Cell key={entry.name} fill={CATEGORICAL_PALETTE[index % CATEGORICAL_PALETTE.length]} />
            ))}
          </Pie>
          <Tooltip
            content={({ active, payload }) => {
              if (!active || !payload || payload.length === 0) {
                return null
              }
              return <PieTooltipContent {...readPieTooltipEntry(payload[0])} />
            }}
          />
          <Legend />
        </PieChart>
      </ResponsiveContainer>
    </div>
  )
}

export default function BrokerBreakdownCharts() {
  const { breakdown, isLoading, error, retry } = useBrokerBreakdown()

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  if (!breakdown) {
    return null
  }

  if (breakdown.length === 0) {
    return <p className="broker-breakdown__empty">No active portfolios to display</p>
  }

  const portfolioData = buildPieSliceData(
    breakdown.map((portfolio) => ({ name: portfolio.portfolioName, value: portfolio.totalInvested })),
  )

  return (
    <div className="broker-breakdown">
      <BreakdownPie title="Portfolio Breakdown" data={portfolioData} />
      <div className="broker-breakdown__grid">
        {breakdown.map((portfolio) => (
          <BreakdownPie
            key={portfolio.portfolioName}
            title={portfolio.portfolioName}
            data={buildPieSliceData(
              portfolio.assets.map((asset) => ({ name: asset.assetName, value: asset.totalInvested })),
            )}
          />
        ))}
      </div>
    </div>
  )
}
