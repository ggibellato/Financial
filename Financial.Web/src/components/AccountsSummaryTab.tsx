import { Text } from '@fluentui/react-components'
import type { ReactNode } from 'react'
import { SUMMARY_LOAD_ERROR, useAccountsSummary } from '../hooks/useAccountsSummary'
import { formatSignedN2 } from '../utils/formatters'
import CategoryTotalsGrid from './CategoryTotalsGrid'
import ErrorState from './ErrorState'
import IncomingGrid from './IncomingGrid'
import LoadingState from './LoadingState'
import SectionTotals from './SectionTotals'

function SummarySection({ heading, children }: { heading: string; children: ReactNode }) {
  return (
    <div className="accounts-page__section monthly-page__section--grid">
      <Text as="h2" weight="semibold" size={400}>
        {heading}
      </Text>
      {children}
    </div>
  )
}

export default function AccountsSummaryTab({ year, month }: { year: number; month: number }) {
  const summary = useAccountsSummary(year, month)

  if (summary.isLoading) return <LoadingState />
  if (summary.error) return <ErrorState message={SUMMARY_LOAD_ERROR} onRetry={summary.retry} />

  return (
    <div className="monthly-page__summary-groups">
      <div className="monthly-page__grids-row">
        <SummarySection heading="Category totals">
          <CategoryTotalsGrid categoryTotals={summary.categoryTotals} categoryTotalsSum={summary.categoryTotalsSum} />
        </SummarySection>
        <SummarySection heading="Income by source">
          <IncomingGrid
            incomeTotals={summary.incomeTotals}
            totalIncoming={summary.totalIncoming}
            titheSummary={summary.titheSummary}
            carryForwardUpdating={summary.carryForwardUpdating}
            onToggleCarryForward={summary.updateCarryForwardInclusion}
            carryForwardActionError={summary.carryForwardError}
          />
        </SummarySection>
      </div>
      <SectionTotals label="Month result" items={[{ label: 'Result', value: `${formatSignedN2(summary.result.value)} ${summary.result.label}` }]} />
    </div>
  )
}
