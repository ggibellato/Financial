const TEMPLATE_LABELS: Record<string, string> = {
  Bank: 'Bank',
  CreditCard: 'Credit card',
  Pots: 'Pots',
}

export type AccountGridColumns = 'balance' | 'card'

export interface AccountTemplateGrid {
  heading: string
  footerLabel: string
  columns: AccountGridColumns
  linksRows: boolean
}

const TEMPLATE_GRIDS: Record<string, AccountTemplateGrid> = {
  Bank: { heading: 'Banks', footerLabel: 'Bank total', columns: 'balance', linksRows: true },
  CreditCard: { heading: 'Credit cards', footerLabel: 'Total owed', columns: 'card', linksRows: true },
  Pots: { heading: 'Pots', footerLabel: 'Pots total', columns: 'balance', linksRows: false },
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
