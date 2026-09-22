import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { useAggregatedSummary } from '../hooks/useAggregatedSummary'
import type { AggregatedSummaryDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { AggregatedSummaryView } from './AggregatedSummaryTab'
import './PortfolioSummaryTab.css'

function incompleteShareBasisMessage(summary: AggregatedSummaryDto): string | null {
  if (summary.unvaluedHoldingCount === 0) return null
  return summary.marketValue === null
    ? 'No portfolio share can be computed for the same reason.'
    : 'Portfolio shares also do not total 100% for the same reason.'
}

export default function PortfolioSummaryTab() {
  const { scope } = useSelectedNode()
  const isHistoric = scope === 'historic'
  const { summary, isLoading: isSummaryLoading, error: summaryError, retry: retrySummary } = useAggregatedSummary()

  const shareBasisMessage = !isHistoric && summary ? incompleteShareBasisMessage(summary) : null

  return (
    <div className="portfolio-summary">
      <div className="portfolio-summary__totals">
        {isSummaryLoading && <LoadingState />}
        {summaryError && <ErrorState message={summaryError} onRetry={retrySummary} />}
        {!isSummaryLoading && !summaryError && summary && <AggregatedSummaryView summary={summary} retry={retrySummary} />}
      </div>

      {shareBasisMessage && (
        <p className="portfolio-summary__share-notice" role="status">
          {shareBasisMessage}
        </p>
      )}
    </div>
  )
}
