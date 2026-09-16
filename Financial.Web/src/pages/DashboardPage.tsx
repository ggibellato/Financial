import { useRef } from 'react'
import AllocationBreakdownPanel from '../components/dashboard/AllocationBreakdownPanel'
import DashboardKpiTiles from '../components/dashboard/DashboardKpiTiles'
import DataQualityWarningsPanel from '../components/dashboard/DataQualityWarningsPanel'
import type { DataQualityWarningsPanelHandle } from '../components/dashboard/DataQualityWarningsPanel'
import { useAllocationBreakdown } from '../hooks/useAllocationBreakdown'
import { useDashboardSummary } from '../hooks/useDashboardSummary'
import { useDataQualityReport } from '../hooks/useDataQualityReport'
import './DashboardPage.css'

export default function DashboardPage() {
  const dashboard = useDashboardSummary()
  const allocation = useAllocationBreakdown()
  const dataQuality = useDataQualityReport()
  const warningsPanelRef = useRef<DataQualityWarningsPanelHandle>(null)

  return (
    <div className="dashboard-page">
      <header className="dashboard-page__header">
        <h2>Dashboard</h2>
      </header>

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
      </div>
    </div>
  )
}
