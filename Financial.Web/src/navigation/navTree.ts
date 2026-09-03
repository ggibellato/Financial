export interface NavChild {
  id: string
  label: string
  route: string
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

export const NAV_TREE: NavCategory[] = [
  {
    id: 'investments',
    label: 'Investments',
    children: [
      { id: 'active-investments', label: 'Active Investments', route: '/investments/active-investments' },
      { id: 'historic-investments', label: 'Historic Investments', route: '/investments/historic-investments' },
      { id: 'dividend-check', label: 'Shares Dividend Check', route: '/investments/dividend-check' },
      { id: 'current-values', label: 'Read Assets Current Values', route: '/investments/current-values' },
    ],
  },
  {
    id: 'cashflow',
    label: 'CashFlow',
    // Order matches Financial.App/Navigation/NavTree.cs — WPF had the correct
    // order (docs/ui/react.md); Web's used to differ.
    children: [
      { id: 'monthly', label: 'Monthly', route: '/cashflow/monthly' },
      { id: 'reserva', label: 'Reserva', route: '/cashflow/reserva' },
      { id: 'mensais', label: 'Mensais', route: '/cashflow/mensais' },
      { id: 'controle-mae', label: 'Controle Mae', route: '/cashflow/controle-mae' },
      { id: 'investment-snapshots', label: 'Investment Snapshots', route: '/cashflow/investment-snapshots' },
      { id: 'annual-summary', label: 'Annual Summary', route: '/cashflow/annual-summary' },
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
        ],
      },
      {
        id: 'cashflow',
        label: 'CashFlow',
        children: [
          { id: 'banks', label: 'Banks', route: '/admin/cashflow/banks' },
          { id: 'categories', label: 'Categories', route: '/admin/cashflow/categories' },
          { id: 'credit-cards', label: 'Credit Cards', route: '/admin/cashflow/credit-cards' },
          { id: 'income-sources', label: 'Income Sources', route: '/admin/cashflow/income-sources' },
          { id: 'investment-accounts', label: 'Investment Accounts', route: '/admin/cashflow/investment-accounts' },
          { id: 'recurring-bills', label: 'Recurring Bills', route: '/admin/cashflow/recurring-bills' },
          { id: 'reserve-buckets', label: 'Reserve Buckets', route: '/admin/cashflow/reserve-buckets' },
        ],
      },
    ],
  },
  {
    id: 'settings',
    label: 'Settings',
    children: [{ id: 'appearance', label: 'Appearance', route: '/settings/appearance' }],
  },
]
