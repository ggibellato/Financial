import AllocationBreakdownPanel from '../components/dashboard/AllocationBreakdownPanel'
import DashboardKpiTiles from '../components/dashboard/DashboardKpiTiles'
import { useAllocationBreakdown } from '../hooks/useAllocationBreakdown'
import { useDashboardSummary } from '../hooks/useDashboardSummary'
import './DashboardPage.css'

export default function DashboardPage() {
  const dashboard = useDashboardSummary()
  const allocation = useAllocationBreakdown()

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
      </div>
    </div>
  )
}
