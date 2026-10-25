const TEMPLATE_LABELS: Record<string, string> = {
  Bank: 'Bank',
  CreditCard: 'Credit card',
}

export function accountTemplateLabel(templateName: string): string {
  return TEMPLATE_LABELS[templateName] ?? templateName
}
