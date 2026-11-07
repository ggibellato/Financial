const TEMPLATE_LABELS: Record<string, string> = {
  Bank: 'Bank',
  CreditCard: 'Credit card',
  Pots: 'Pots',
}

export type AccountGridColumns = 'balance' | 'card' | 'settlement'

export interface AccountTemplateGrid {
  heading: string
  footerLabel: string
  columns: AccountGridColumns
  linksRows: boolean
  rowLinkTab?: string
}

const TEMPLATE_GRIDS: Record<string, AccountTemplateGrid> = {
  Bank: { heading: 'Banks', footerLabel: 'Bank total', columns: 'balance', linksRows: true },
  CreditCard: { heading: 'Credit cards', footerLabel: 'Net owed', columns: 'card', linksRows: true },
  Pots: { heading: 'Pots', footerLabel: 'Pots total', columns: 'balance', linksRows: true, rowLinkTab: 'pots' },
  Settlement: { heading: 'Settlement', footerLabel: 'Total', columns: 'settlement', linksRows: false },
}

export function accountTemplateLabel(templateName: string): string {
  return TEMPLATE_LABELS[templateName] ?? templateName
}

export function accountTemplateGrid(templateName: string): AccountTemplateGrid {
  return (
    TEMPLATE_GRIDS[templateName] ?? {
      heading: accountTemplateLabel(templateName),
      footerLabel: 'Total',
      columns: 'balance',
      linksRows: false,
    }
  )
}
