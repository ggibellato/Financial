import { describe, expect, it } from 'vitest'
import { accountTemplateGrid, accountTemplateLabel, accountTemplateOrder } from '../accountTemplateLabel'

describe('accountTemplateLabel', () => {
  it('names the Bank template "Bank"', () => {
    expect(accountTemplateLabel('Bank')).toBe('Bank')
  })

  it('names the CreditCard template "Credit card"', () => {
    expect(accountTemplateLabel('CreditCard')).toBe('Credit card')
  })

  it('names the Pots template "Pots"', () => {
    expect(accountTemplateLabel('Pots')).toBe('Pots')
  })

  it('shows any other template by its name', () => {
    expect(accountTemplateLabel('Settlement')).toBe('Settlement')
  })
})

describe('accountTemplateOrder', () => {
  it('orders templates as the Accounts page groups them, any other template last', () => {
    const templates = ['Cash', 'Savings', 'Settlement', 'Pots', 'CreditCard', 'Bank']

    expect(templates.sort((a, b) => accountTemplateOrder(a) - accountTemplateOrder(b))).toEqual(['Bank', 'CreditCard', 'Pots', 'Settlement', 'Savings', 'Cash'])
  })
})

describe('accountTemplateGrid', () => {
  it('shows banks with their balance under "Banks", totalled as "Bank total", with linked rows', () => {
    expect(accountTemplateGrid('Bank')).toEqual({ heading: 'Banks', footerLabel: 'Bank total', columns: 'balance', linksRows: true })
  })

  it('shows cards with their next statement and total owed under "Credit cards", totalled as "Net owed", with linked rows', () => {
    expect(accountTemplateGrid('CreditCard')).toEqual({ heading: 'Credit cards', footerLabel: 'Net owed', columns: 'card', linksRows: true })
  })

  it('shows Pots accounts with their balance under "Pots", totalled as "Pots total", with rows linked to the Pots tab', () => {
    expect(accountTemplateGrid('Pots')).toEqual({ heading: 'Pots', footerLabel: 'Pots total', columns: 'balance', linksRows: true, rowLinkTab: 'pots' })
  })

  it('shows Settlement accounts with who owes whom under "Settlement", with linked rows and no footer', () => {
    expect(accountTemplateGrid('Settlement')).toEqual({ heading: 'Settlement', columns: 'settlement', linksRows: true })
  })

  it('[AC P64-F04-savings-web-03] shows Savings accounts with their balance under "Savings", totalled as "Savings total", with linked rows', () => {
    expect(accountTemplateGrid('Savings')).toEqual({ heading: 'Savings', footerLabel: 'Savings total', columns: 'balance', linksRows: true })
  })

  it('shows any other template under its label with a "Total" footer, its balance and plain-text rows', () => {
    expect(accountTemplateGrid('Loans')).toEqual({ heading: 'Loans', footerLabel: 'Total', columns: 'balance', linksRows: false })
  })
})
