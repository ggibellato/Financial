import { matchPath } from 'react-router-dom'

export interface NavDetail {
  label: string
  route: string
}

export interface NavChild {
  id: string
  label: string
  route: string
  detail?: NavDetail
}

export interface NavGroup {
  id: string
  label: string
  children: NavChild[]
}

export interface NavCategory {
  id: string
  label: string
  /** Leaves directly under this category. Mutually exclusive with `groups` — a category has one or the other. */
  children: NavChild[]
  /** Sub-groups nested under this category (3-level nav). Only the `admin` category uses this today. */
  groups?: NavGroup[]
}

export type NavMatch = 'page' | 'detail' | null

export function matchNavChild(pathname: string, child: NavChild): NavMatch {
  if (pathname === child.route) return 'page'
  return child.detail && matchPath(child.detail.route, pathname) ? 'detail' : null
}

export const NAV_TREE: NavCategory[] = [
  {
    id: 'investments',
    label: 'Investments',
    children: [
      { id: 'dashboard', label: 'Dashboard', route: '/investments/dashboard' },
      { id: 'active-investments', label: 'Active Investments', route: '/investments/active-investments' },
      { id: 'historic-investments', label: 'Historic Investments', route: '/investments/historic-investments' },
      { id: 'dividend-check', label: 'Shares Dividend Check', route: '/investments/dividend-check' },
      { id: 'current-values', label: 'Read Assets Current Values', route: '/investments/current-values' },
      { id: 'tax', label: 'Tax', route: '/investments/tax' },
    ],
  },
  {
    id: 'cashflow',
    label: 'CashFlow',
    children: [
      {
        id: 'accounts',
        label: 'Accounts',
        route: '/cashflow/accounts',
        detail: { label: 'Account', route: '/cashflow/accounts/:accountId' },
      },
      { id: 'monthly', label: 'Monthly', route: '/cashflow/monthly' },
      { id: 'annual-summary', label: 'Annual Summary', route: '/cashflow/annual-summary' },
      { id: 'mensais', label: 'Mensais', route: '/cashflow/mensais' },
      { id: 'investment-snapshots', label: 'Investment Snapshots', route: '/cashflow/investment-snapshots' },
    ],
  },
  {
    id: 'admin',
    label: 'Admin',
    children: [],
    groups: [
      {
        id: 'investment',
        label: 'Investment',
        children: [
          { id: 'assets', label: 'Assets', route: '/admin/investment/assets' },
          { id: 'brokers', label: 'Brokers', route: '/admin/investment/brokers' },
          { id: 'portfolios', label: 'Portfolios', route: '/admin/investment/portfolios' },
          { id: 'tax-rules', label: 'Tax Rules', route: '/admin/investment/tax-rules' },
        ],
      },
      {
        id: 'cashflow',
        label: 'CashFlow',
        children: [
          { id: 'accounts', label: 'Accounts', route: '/admin/cashflow/accounts' },
          { id: 'categories', label: 'Categories', route: '/admin/cashflow/categories' },
          { id: 'income-sources', label: 'Income Sources', route: '/admin/cashflow/income-sources' },
          { id: 'investment-accounts', label: 'Investment Accounts', route: '/admin/cashflow/investment-accounts' },
          { id: 'recurring-bills', label: 'Recurring Bills', route: '/admin/cashflow/recurring-bills' },
        ],
      },
    ],
  },
  {
    id: 'settings',
    label: 'Settings',
    children: [
      { id: 'appearance', label: 'Appearance', route: '/settings/appearance' },
      { id: 'reporting-currency', label: 'Reporting Currency', route: '/settings/reporting-currency' },
      { id: 'integrations', label: 'Integrations', route: '/settings/integrations' },
    ],
  },
]

export interface NavLocation {
  trail: string[]
  page: NavChild
  detail?: NavDetail
}

export function findNavLocation(pathname: string): NavLocation | null {
  for (const category of NAV_TREE) {
    const sections = [
      { trail: [category.label], children: category.children },
      ...(category.groups ?? []).map((group) => ({ trail: [category.label, group.label], children: group.children })),
    ]
    for (const { trail, children } of sections) {
      for (const page of children) {
        const match = matchNavChild(pathname, page)
        if (match === 'page') return { trail, page }
        if (match === 'detail') return { trail, page, detail: page.detail }
      }
    }
  }
  return null
}
