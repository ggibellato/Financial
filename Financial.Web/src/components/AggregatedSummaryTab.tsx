import BrokerBreakdownCharts from './BrokerBreakdownCharts'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { useAggregatedSummary } from '../hooks/useAggregatedSummary'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { formatN2, signClass } from '../utils/formatters'
import './AggregatedSummaryTab.css'

export default function AggregatedSummaryTab() {
  const { summary, isLoading, error, retry } = useAggregatedSummary()
  const { selectedNode } = useSelectedNode()
  const isBroker = selectedNode?.nodeType === 'Broker'

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  if (!summary) {
    return null
  }

  return (
    <div className="aggregated-summary">
      <div className="aggregated-summary__grid">
        <div className="aggregated-summary__field">
          <span className="aggregated-summary__label">Total Bought</span>
          <span className="aggregated-summary__value aggregated-summary__value--green">
            {formatN2(summary.totalBought)}
          </span>
        </div>
        <div className="aggregated-summary__field">
          <span className="aggregated-summary__label">Total Sold</span>
          <span className="aggregated-summary__value aggregated-summary__value--red">
            {formatN2(summary.totalSold)}
          </span>
        </div>
        <div className="aggregated-summary__field">
          <span className="aggregated-summary__label">Total Credits</span>
          <span className="aggregated-summary__value aggregated-summary__value--blue">
            {formatN2(summary.totalCredits)}
          </span>
        </div>
        <div className="aggregated-summary__field">
          <span className="aggregated-summary__label">Total Invested</span>
          <span className={`aggregated-summary__value ${signClass(summary.totalInvested, 'aggregated-summary__value')}`}>
            {formatN2(summary.totalInvested)}
          </span>
        </div>
      </div>
      {isBroker && <BrokerBreakdownCharts />}
    </div>
  )
}
