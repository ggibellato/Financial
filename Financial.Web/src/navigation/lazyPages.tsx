import { lazy } from 'react'
import { Navigate, useSearchParams } from 'react-router-dom'

// Split into its own module (rather than declared inline in routes.tsx) so that file can export
// only route data: react-refresh/only-export-components requires a file to export components
// exclusively, and PAGE_ROUTES/PageRoute are not components.
export const AccountScreenPage = lazy(() => import('../pages/AccountScreenPage'))
export const AccountsPage = lazy(() => import('../pages/AccountsPage'))
export const ActiveInvestmentsPage = lazy(() => import('../pages/ActiveInvestmentsPage'))
export const AdminAccountsPage = lazy(() => import('../pages/AdminAccountsPage'))
export const AdminEntityPlaceholderPage = lazy(() => import('../pages/AdminEntityPlaceholderPage'))
export const AppearancePage = lazy(() => import('../pages/AppearancePage'))
export const AssetsPage = lazy(() => import('../pages/AssetsPage'))
export const BrokersPage = lazy(() => import('../pages/BrokersPage'))
export const CategoriesPage = lazy(() => import('../pages/CategoriesPage'))
export const IncomeSourcesPage = lazy(() => import('../pages/IncomeSourcesPage'))
export const IntegrationsPage = lazy(() => import('../pages/IntegrationsPage'))
export const InvestmentAccountsPage = lazy(() => import('../pages/InvestmentAccountsPage'))
export const PortfoliosPage = lazy(() => import('../pages/PortfoliosPage'))
export const AnnualSummaryPage = lazy(() => import('../pages/AnnualSummaryPage'))
export const DashboardPage = lazy(() => import('../pages/DashboardPage'))
export const CurrentValuesPage = lazy(() => import('../pages/CurrentValuesPage'))
export const DividendCheckPage = lazy(() => import('../pages/DividendCheckPage'))
export const HistoricInvestmentsPage = lazy(() => import('../pages/HistoricInvestmentsPage'))
export const InvestmentSnapshotsPage = lazy(() => import('../pages/InvestmentSnapshotsPage'))
export const MensaisPage = lazy(() => import('../pages/MensaisPage'))
export const RecurringBillsPage = lazy(() => import('../pages/RecurringBillsPage'))
export const ReportingCurrencyPage = lazy(() => import('../pages/ReportingCurrencyPage'))
export const MovedPageRedirect = lazy(() => import('../pages/MovedPageRedirect'))
export const TaxPage = lazy(() => import('../pages/TaxPage'))
export const TaxRulesPage = lazy(() => import('../pages/TaxRulesPage'))

export function MonthlyRedirect() {
  const [searchParams] = useSearchParams()
  const month = searchParams.get('month')
  const target = new URLSearchParams({ tab: 'summary', ...(month ? { month } : {}) })
  return <Navigate replace to={`/cashflow/accounts?${target}`} />
}
