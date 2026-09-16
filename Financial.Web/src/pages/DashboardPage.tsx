import DashboardKpiTiles from '../components/dashboard/DashboardKpiTiles'
import { useDashboardSummary } from '../hooks/useDashboardSummary'
import './DashboardPage.css'

export default function DashboardPage() {
  const dashboard = useDashboardSummary()

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
      </div>
    </div>
  )
}
