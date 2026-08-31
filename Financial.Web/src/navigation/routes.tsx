import type { ReactElement } from 'react'
import {
  ActiveInvestmentsPage,
  AdminEntityPlaceholderPage,
  AnnualSummaryPage,
  AssetsPage,
  BanksPage,
  BrokersPage,
  CategoriesPage,
  ControleMaePage,
  CreditCardsPage,
  CurrentValuesPage,
  DividendCheckPage,
  HistoricInvestmentsPage,
  IncomeSourcesPage,
  InvestmentAccountsPage,
  InvestmentSnapshotsPage,
  MensaisPage,
  MonthlyPage,
  PortfoliosPage,
  ReservaPage,
  ReserveBucketsPage,
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
  { path: 'investments/active-investments', element: <ActiveInvestmentsPage /> },
  { path: 'investments/historic-investments', element: <HistoricInvestmentsPage /> },
  { path: 'investments/dividend-check', element: <DividendCheckPage /> },
  { path: 'investments/current-values', element: <CurrentValuesPage /> },
  { path: 'cashflow/monthly', element: <MonthlyPage /> },
  { path: 'cashflow/investment-snapshots', element: <InvestmentSnapshotsPage /> },
  { path: 'cashflow/annual-summary', element: <AnnualSummaryPage /> },
  { path: 'cashflow/reserva', element: <ReservaPage /> },
  { path: 'cashflow/mensais', element: <MensaisPage /> },
  { path: 'cashflow/controle-mae', element: <ControleMaePage /> },
  { path: 'admin/investment/assets', element: <AssetsPage /> },
  { path: 'admin/investment/brokers', element: <BrokersPage /> },
  { path: 'admin/investment/portfolios', element: <PortfoliosPage /> },
  { path: 'admin/cashflow/banks', element: <BanksPage /> },
  { path: 'admin/cashflow/categories', element: <CategoriesPage /> },
  { path: 'admin/cashflow/credit-cards', element: <CreditCardsPage /> },
  { path: 'admin/cashflow/income-sources', element: <IncomeSourcesPage /> },
  { path: 'admin/cashflow/investment-accounts', element: <InvestmentAccountsPage /> },
  { path: 'admin/cashflow/recurring-bills', element: <AdminEntityPlaceholderPage entityLabel="Recurring Bills" /> },
  { path: 'admin/cashflow/reserve-buckets', element: <ReserveBucketsPage /> },
]
