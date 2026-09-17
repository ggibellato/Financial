import { useEffect, useRef, useState } from 'react'
import { Button } from '@fluentui/react-components'
import AllocationBreakdownPanel from '../components/dashboard/AllocationBreakdownPanel'
import DashboardKpiTiles from '../components/dashboard/DashboardKpiTiles'
import DataQualityWarningsPanel from '../components/dashboard/DataQualityWarningsPanel'
import type { DataQualityWarningsPanelHandle } from '../components/dashboard/DataQualityWarningsPanel'
import UpcomingIncomePanel from '../components/dashboard/UpcomingIncomePanel'
import { useAllocationBreakdown } from '../hooks/useAllocationBreakdown'
import { useDashboardSummary } from '../hooks/useDashboardSummary'
import { useDataQualityReport } from '../hooks/useDataQualityReport'
import { useUpcomingIncome } from '../hooks/useUpcomingIncome'
import './DashboardPage.css'

export default function DashboardPage() {
  const dashboard = useDashboardSummary()
  const allocation = useAllocationBreakdown()
  const dataQuality = useDataQualityReport()
  const upcomingIncome = useUpcomingIncome()
  const warningsPanelRef = useRef<DataQualityWarningsPanelHandle>(null)
  const pageHeadingRef = useRef<HTMLHeadingElement>(null)
  const [retryAllToken, setRetryAllToken] = useState(0)

  const panels = [dashboard, allocation, dataQuality, upcomingIncome]
  const anyPanelLoading = panels.some((panel) => panel.isLoading)
  const everyPanelFailed = panels.every((panel) => panel.error !== null && !panel.isLoading)

  // Retrying swaps the whole page's content back to the four panels, so the button the user just
  // pressed is gone: once every request has settled (not merely started) and at least one
  // recovered, move focus to the page heading so keyboard users don't lose their place. If every
  // panel is still failing after the retry, the same Retry button is still on screen in the same
  // spot, so focus already has somewhere sensible to stay - nothing to fix in that case.
  useEffect(() => {
    if (retryAllToken === 0 || anyPanelLoading || everyPanelFailed) return
    pageHeadingRef.current?.focus()
  }, [retryAllToken, anyPanelLoading, everyPanelFailed])

  const handleRetryAll = () => {
    panels.forEach((panel) => panel.retry())
    setRetryAllToken((token) => token + 1)
  }

  return (
    <div className="dashboard-page">
      <header className="dashboard-page__header">
        <h2 ref={pageHeadingRef} tabIndex={-1}>
          Dashboard
        </h2>
      </header>

      {everyPanelFailed ? (
        <div className="dashboard-page__error" role="alert">
          <p>Unable to load the dashboard — none of its data could be retrieved.</p>
          <Button appearance="primary" onClick={handleRetryAll}>
            Retry
          </Button>
        </div>
      ) : (
        <div className="dashboard-page__panels">
          <section className="dashboard-page__panel dashboard-page__panel--kpis" aria-labelledby="dashboard-kpis-heading">
            <h3 id="dashboard-kpis-heading">Portfolio Summary</h3>
            <DashboardKpiTiles
              summary={dashboard.summary}
              isLoading={dashboard.isLoading}
              error={dashboard.error}
              retry={dashboard.retry}
              onViewMissingPriceHoldings={() => warningsPanelRef.current?.expandCategory('missingPrice')}
            />
          </section>

          <section
            className="dashboard-page__panel dashboard-page__panel--allocation"
            aria-labelledby="dashboard-allocation-heading"
          >
            <h3 id="dashboard-allocation-heading">Allocation Breakdown</h3>
            <AllocationBreakdownPanel
              breakdown={allocation.breakdown}
              isLoading={allocation.isLoading}
              error={allocation.error}
              retry={allocation.retry}
            />
          </section>

          <section
            className="dashboard-page__panel dashboard-page__panel--warnings"
            aria-labelledby="dashboard-warnings-heading"
          >
            <h3 id="dashboard-warnings-heading">Data Quality Warnings</h3>
            <DataQualityWarningsPanel
              ref={warningsPanelRef}
              report={dataQuality.report}
              isLoading={dataQuality.isLoading}
              error={dataQuality.error}
              retry={dataQuality.retry}
            />
          </section>

          <section
            className="dashboard-page__panel dashboard-page__panel--upcoming-income"
            aria-labelledby="dashboard-upcoming-income-heading"
          >
            <h3 id="dashboard-upcoming-income-heading">Upcoming Income</h3>
            <UpcomingIncomePanel
              entries={upcomingIncome.entries}
              isLoading={upcomingIncome.isLoading}
              error={upcomingIncome.error}
              retry={upcomingIncome.retry}
            />
          </section>
        </div>
      )}
    </div>
  )
}
