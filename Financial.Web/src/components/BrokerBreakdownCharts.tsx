import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import AllocationPieChart, { type AllocationChartEntry } from './dashboard/AllocationPieChart'
import { useBrokerBreakdown } from '../hooks/useBrokerBreakdown'
import './BrokerBreakdownCharts.css'

function buildChartEntries(items: { name: string; value: number }[]): AllocationChartEntry[] {
  const total = items.reduce((sum, item) => sum + item.value, 0)
  return items.map((item) => ({
    label: item.name,
    marketValue: item.value,
    percentage: total > 0 ? (item.value / total) * 100 : 0,
  }))
}

interface BreakdownPieProps {
  title: string
  entries: AllocationChartEntry[]
}

function BreakdownPie({ title, entries }: BreakdownPieProps) {
  return <AllocationPieChart title={title} entries={entries} headingLevel="h3" valueLabel="Total Invested" />
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

  const portfolioEntries = buildChartEntries(
    breakdown.map((portfolio) => ({ name: portfolio.portfolioName, value: portfolio.totalInvested })),
  )

  return (
    <div className="broker-breakdown">
      <BreakdownPie title="Portfolio Breakdown" entries={portfolioEntries} />
      <div className="broker-breakdown__grid">
        {breakdown.map((portfolio) => (
          <BreakdownPie
            key={portfolio.portfolioName}
            title={portfolio.portfolioName}
            entries={buildChartEntries(
              portfolio.assets.map((asset) => ({ name: asset.assetName, value: asset.totalInvested })),
            )}
          />
        ))}
      </div>
    </div>
  )
}
