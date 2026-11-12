import type { ReactElement } from 'react'
import { Navigate } from 'react-router-dom'
import {
  AccountScreenPage,
  AccountsPage,
  ActiveInvestmentsPage,
  AdminAccountsPage,
  AnnualSummaryPage,
  AppearancePage,
  AssetsPage,
  BrokersPage,
  CategoriesPage,
  CurrentValuesPage,
  DashboardPage,
  DividendCheckPage,
  HistoricInvestmentsPage,
  IncomeSourcesPage,
  IntegrationsPage,
  InvestmentAccountsPage,
  InvestmentSnapshotsPage,
  MensaisPage,
  MonthlyRedirect,
  MovedPageRedirect,
  PortfoliosPage,
  RecurringBillsPage,
  ReportingCurrencyPage,
  TaxPage,
  TaxRulesPage,
} from './lazyPages'

export interface PageRoute {
  /** Path relative to the app's root route, i.e. without the leading slash. */
  path: string
  element: ReactElement
}

/**
 * The pages reachable from the sidebar, declared here rather than inline in main.tsx so that a
 * test can check them against NAV_TREE. The two lists have to agree: a page in NAV_TREE with no
 * route 404s when clicked, and a route missing from NAV_TREE is unreachable from the sidebar.
 * Neither failure surfaces at build time, and both used to be invisible until someone hit them.
 *
 * The index redirect and the not-found catch-all stay in main.tsx: they are routing plumbing
 * rather than sidebar destinations, so they have no NAV_TREE entry to agree with.
 */
export const PAGE_ROUTES: PageRoute[] = [
  { path: 'investments/dashboard', element: <DashboardPage /> },
  { path: 'investments/active-investments', element: <ActiveInvestmentsPage /> },
  { path: 'investments/historic-investments', element: <HistoricInvestmentsPage /> },
  { path: 'investments/dividend-check', element: <DividendCheckPage /> },
  { path: 'investments/current-values', element: <CurrentValuesPage /> },
  { path: 'investments/tax', element: <TaxPage /> },
  { path: 'cashflow/accounts', element: <AccountsPage /> },
  { path: 'cashflow/investment-snapshots', element: <InvestmentSnapshotsPage /> },
  { path: 'cashflow/annual-summary', element: <AnnualSummaryPage /> },
  { path: 'cashflow/mensais', element: <MensaisPage /> },
  { path: 'admin/investment/assets', element: <AssetsPage /> },
  { path: 'admin/investment/brokers', element: <BrokersPage /> },
  { path: 'admin/investment/portfolios', element: <PortfoliosPage /> },
  { path: 'admin/investment/tax-rules', element: <TaxRulesPage /> },
  { path: 'admin/cashflow/accounts', element: <AdminAccountsPage /> },
  { path: 'admin/cashflow/categories', element: <CategoriesPage /> },
  { path: 'admin/cashflow/income-sources', element: <IncomeSourcesPage /> },
  { path: 'admin/cashflow/investment-accounts', element: <InvestmentAccountsPage /> },
  { path: 'admin/cashflow/recurring-bills', element: <RecurringBillsPage /> },
  { path: 'settings/appearance', element: <AppearancePage /> },
  { path: 'settings/reporting-currency', element: <ReportingCurrencyPage /> },
  { path: 'settings/integrations', element: <IntegrationsPage /> },
]

export const DETAIL_ROUTES: PageRoute[] = [
  { path: 'cashflow/accounts/:accountId', element: <AccountScreenPage /> },
]

export const REDIRECT_ROUTES: PageRoute[] = [
  { path: 'admin/cashflow/banks', element: <Navigate replace to="/admin/cashflow/accounts" /> },
  { path: 'admin/cashflow/credit-cards', element: <Navigate replace to="/admin/cashflow/accounts" /> },
  { path: 'admin/cashflow/reserve-buckets', element: <MovedPageRedirect page="reserva" tab="pots" /> },
  { path: 'cashflow/monthly', element: <MonthlyRedirect /> },
  { path: 'cashflow/controle-mae', element: <MovedPageRedirect page="controle-mae" /> },
  { path: 'cashflow/reserva', element: <MovedPageRedirect page="reserva" tab="pots" /> },
]

export const ALL_ROUTES: PageRoute[] = [...PAGE_ROUTES, ...DETAIL_ROUTES, ...REDIRECT_ROUTES]
