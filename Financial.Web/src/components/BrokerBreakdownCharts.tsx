import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import AllocationPieChart, { type AllocationChartEntry } from './dashboard/AllocationPieChart'
import { useBrokerBreakdown } from '../hooks/useBrokerBreakdown'
import { totalsBySegment } from '../utils/assetSegments'
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
  return <AllocationPieChart title={title} entries={entries} headingLevel="h3" valueLabel="Total invested" />
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

  const assets = breakdown.flatMap((portfolio) => portfolio.assets)
  const investedByAsset = new Map<string, number>()
  assets.forEach((asset) => investedByAsset.set(asset.assetName, (investedByAsset.get(asset.assetName) ?? 0) + asset.totalInvested))
  const assetEntries = buildChartEntries([...investedByAsset].map(([name, value]) => ({ name, value })))

  const segmentEntries = assets.some((asset) => asset.segment)
    ? buildChartEntries(totalsBySegment(assets.map((asset) => ({ name: asset.assetName, segment: asset.segment, value: asset.totalInvested }))))
    : null

  return (
    <div className="broker-breakdown">
      <BreakdownPie title="All assets" entries={assetEntries} />
      {segmentEntries && <BreakdownPie title="By segment" entries={segmentEntries} />}
      <BreakdownPie title="Portfolio breakdown" entries={portfolioEntries} />
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
