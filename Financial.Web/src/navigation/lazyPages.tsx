import { lazy } from 'react'

// Split into its own module (rather than declared inline in routes.tsx) so that file can export
// only route data: react-refresh/only-export-components requires a file to export components
// exclusively, and PAGE_ROUTES/PageRoute are not components.
export const ActiveInvestmentsPage = lazy(() => import('../pages/ActiveInvestmentsPage'))
export const AdminEntityPlaceholderPage = lazy(() => import('../pages/AdminEntityPlaceholderPage'))
export const AssetsPage = lazy(() => import('../pages/AssetsPage'))
export const BanksPage = lazy(() => import('../pages/BanksPage'))
export const BrokersPage = lazy(() => import('../pages/BrokersPage'))
export const CategoriesPage = lazy(() => import('../pages/CategoriesPage'))
export const CreditCardsPage = lazy(() => import('../pages/CreditCardsPage'))
export const IncomeSourcesPage = lazy(() => import('../pages/IncomeSourcesPage'))
export const PortfoliosPage = lazy(() => import('../pages/PortfoliosPage'))
export const AnnualSummaryPage = lazy(() => import('../pages/AnnualSummaryPage'))
export const ControleMaePage = lazy(() => import('../pages/ControleMaePage'))
export const CurrentValuesPage = lazy(() => import('../pages/CurrentValuesPage'))
export const DividendCheckPage = lazy(() => import('../pages/DividendCheckPage'))
export const HistoricInvestmentsPage = lazy(() => import('../pages/HistoricInvestmentsPage'))
export const InvestmentSnapshotsPage = lazy(() => import('../pages/InvestmentSnapshotsPage'))
export const MensaisPage = lazy(() => import('../pages/MensaisPage'))
export const MonthlyPage = lazy(() => import('../pages/MonthlyPage'))
export const RecurringBillsPage = lazy(() => import('../pages/RecurringBillsPage'))
export const ReservaPage = lazy(() => import('../pages/ReservaPage'))
