import BrokerBreakdownCharts from './BrokerBreakdownCharts'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { useAggregatedSummary } from '../hooks/useAggregatedSummary'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { formatN2, formatPercentFraction, signClass } from '../utils/formatters'
import type { AggregatedSummaryDto } from '../api/types'
import './AggregatedSummaryTab.css'

function incompleteValuationMessage(summary: AggregatedSummaryDto): string | null {
  if (summary.unvaluedHoldingCount === 0) return null
  return summary.marketValue === null
    ? `None of the ${summary.holdingCount} holdings could be valued; returns are withheld.`
    : `${summary.unvaluedHoldingCount} of ${summary.holdingCount} holdings could not be valued; the total is incomplete and returns are withheld.`
}

function formatConvertedAmount(value: number | null): string {
  return value === null ? '—' : formatN2(value)
}

function formatConvertedPercent(value: number | null): string {
  return value === null ? '—' : formatPercentFraction(value)
}

export function AggregatedSummaryView({ summary, retry }: { summary: AggregatedSummaryDto; retry: () => void }) {
  const { selectedNode } = useSelectedNode()
  const isBroker = selectedNode?.nodeType === 'Broker'
  const incompleteMessage = incompleteValuationMessage(summary)

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
        <div className="aggregated-summary__field">
          <span className="aggregated-summary__label">Market Value</span>
          <span className="aggregated-summary__value">
            {summary.marketValue === null ? '—' : formatN2(summary.marketValue)}
          </span>
        </div>
        <div className="aggregated-summary__field">
          <span className="aggregated-summary__label">Price-Only Return</span>
          <span
            className={`aggregated-summary__value ${summary.priceOnlyReturn === null ? '' : signClass(summary.priceOnlyReturn, 'aggregated-summary__value')}`}
          >
            {summary.priceOnlyReturn === null ? '—' : formatPercentFraction(summary.priceOnlyReturn)}
          </span>
        </div>
        <div className="aggregated-summary__field">
          <span className="aggregated-summary__label">Total Return (Gross)</span>
          <span
            className={`aggregated-summary__value ${summary.totalReturn === null ? '' : signClass(summary.totalReturn, 'aggregated-summary__value')}`}
          >
            {summary.totalReturn === null ? '—' : formatPercentFraction(summary.totalReturn)}
          </span>
        </div>
        <div className="aggregated-summary__field">
          <span className="aggregated-summary__label">Total Return (Net of Tax)</span>
          <span
            className={`aggregated-summary__value ${summary.totalReturnNetOfTax === null ? '' : signClass(summary.totalReturnNetOfTax, 'aggregated-summary__value')}`}
          >
            {summary.totalReturnNetOfTax === null ? '—' : formatPercentFraction(summary.totalReturnNetOfTax)}
          </span>
        </div>
      </div>
      {incompleteMessage && (
        <p className="aggregated-summary__incomplete-notice" role="status">
          {incompleteMessage}
        </p>
      )}
      {summary.isReportingCurrencyUnavailable ? (
        <ErrorState
          message={`Converted totals unavailable — showing native-currency figures only.`}
          onRetry={retry}
        />
      ) : (
        <div className="aggregated-summary__converted">
          <h3 className="aggregated-summary__converted-heading">Converted to {summary.reportingCurrency}</h3>
          {summary.isPartial && (
            <p className="aggregated-summary__partial-notice" role="status">
              Some figures could not be converted to {summary.reportingCurrency} — showing a partial total.
            </p>
          )}
          <div className="aggregated-summary__grid">
            <div className="aggregated-summary__field">
              <span className="aggregated-summary__label">Market Value (converted to {summary.reportingCurrency})</span>
              <span className="aggregated-summary__value">{formatConvertedAmount(summary.convertedMarketValue)}</span>
            </div>
            <div className="aggregated-summary__field">
              <span className="aggregated-summary__label">Invested (converted to {summary.reportingCurrency})</span>
              <span
                className={`aggregated-summary__value ${summary.convertedInvested === null ? '' : signClass(summary.convertedInvested, 'aggregated-summary__value')}`}
              >
                {formatConvertedAmount(summary.convertedInvested)}
              </span>
            </div>
            <div className="aggregated-summary__field">
              <span className="aggregated-summary__label">Unrealised Gain/Loss (converted to {summary.reportingCurrency})</span>
              <span
                className={`aggregated-summary__value ${summary.convertedUnrealisedGainLoss === null ? '' : signClass(summary.convertedUnrealisedGainLoss, 'aggregated-summary__value')}`}
              >
                {formatConvertedAmount(summary.convertedUnrealisedGainLoss)}
              </span>
            </div>
            <div className="aggregated-summary__field">
              <span className="aggregated-summary__label">Total Return (converted to {summary.reportingCurrency})</span>
              <span
                className={`aggregated-summary__value ${summary.convertedTotalReturn === null ? '' : signClass(summary.convertedTotalReturn, 'aggregated-summary__value')}`}
              >
                {formatConvertedPercent(summary.convertedTotalReturn)}
              </span>
            </div>
            <div className="aggregated-summary__field">
              <span className="aggregated-summary__label">Total Return Net of Tax (converted to {summary.reportingCurrency})</span>
              <span
                className={`aggregated-summary__value ${summary.convertedTotalReturnNetOfTax === null ? '' : signClass(summary.convertedTotalReturnNetOfTax, 'aggregated-summary__value')}`}
              >
                {formatConvertedPercent(summary.convertedTotalReturnNetOfTax)}
              </span>
            </div>
          </div>
        </div>
      )}
      {isBroker && <BrokerBreakdownCharts />}
    </div>
  )
}

export default function AggregatedSummaryTab() {
  const { summary, isLoading, error, retry } = useAggregatedSummary()

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  if (!summary) {
    return null
  }

  return <AggregatedSummaryView summary={summary} retry={retry} />
}
