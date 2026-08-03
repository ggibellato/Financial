export interface NavChild {
  id: string
  label: string
  route: string
}

export interface NavCategory {
  id: string
  label: string
  children: NavChild[]
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
    children: [
      { id: 'monthly', label: 'Monthly', route: '/cashflow/monthly' },
      { id: 'investment-snapshots', label: 'Investment Snapshots', route: '/cashflow/investment-snapshots' },
      { id: 'annual-summary', label: 'Annual Summary', route: '/cashflow/annual-summary' },
      { id: 'reserva', label: 'Reserva', route: '/cashflow/reserva' },
      { id: 'mensais', label: 'Mensais', route: '/cashflow/mensais' },
      { id: 'controle-mae', label: 'Controle Mae', route: '/cashflow/controle-mae' },
    ],
  },
]
