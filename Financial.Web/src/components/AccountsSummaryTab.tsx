import { SUMMARY_LOAD_ERROR, useAccountsSummary } from '../hooks/useAccountsSummary'
import { formatSignedN2 } from '../utils/formatters'
import CategoryTotalsGrid from './CategoryTotalsGrid'
import ErrorState from './ErrorState'
import IncomingGrid from './IncomingGrid'
import LoadingState from './LoadingState'
import SectionTotals from './SectionTotals'

export default function AccountsSummaryTab({ year, month }: { year: number; month: number }) {
  const summary = useAccountsSummary(year, month)

  if (summary.isLoading) return <LoadingState />
  if (summary.error) return <ErrorState message={SUMMARY_LOAD_ERROR} onRetry={summary.retry} />

  return (
    <div className="monthly-page__summary-groups">
      <div className="monthly-page__grids-row">
        <CategoryTotalsGrid categoryTotals={summary.categoryTotals} categoryTotalsSum={summary.categoryTotalsSum} />
        <IncomingGrid
          incomeTotals={summary.incomeTotals}
          totalIncoming={summary.totalIncoming}
          titheSummary={summary.titheSummary}
          carryForwardUpdating={summary.carryForwardUpdating}
          onToggleCarryForward={summary.updateCarryForwardInclusion}
          carryForwardActionError={summary.carryForwardError}
        />
      </div>
      <SectionTotals label="Month result" items={[{ label: 'Result', value: `${formatSignedN2(summary.result.value)} ${summary.result.label}` }]} />
    </div>
  )
}
