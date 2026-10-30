import { describe, expect, it } from 'vitest'
import { accountTemplateGrid, accountTemplateLabel } from '../accountTemplateLabel'

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

describe('accountTemplateGrid', () => {
  it('shows banks with their balance under "Banks", totalled as "Bank total", with linked rows', () => {
    expect(accountTemplateGrid('Bank')).toEqual({ heading: 'Banks', footerLabel: 'Bank total', columns: 'balance', linksRows: true })
  })

  it('shows cards with their next statement and total owed under "Credit cards", totalled as "Total owed", with linked rows', () => {
    expect(accountTemplateGrid('CreditCard')).toEqual({ heading: 'Credit cards', footerLabel: 'Total owed', columns: 'card', linksRows: true })
  })

  it('shows Pots accounts with their balance under "Pots", totalled as "Pots total", as plain text', () => {
    expect(accountTemplateGrid('Pots')).toEqual({ heading: 'Pots', footerLabel: 'Pots total', columns: 'balance', linksRows: false })
  })

  it('shows any other template under its label with a "Total" footer, its balance and plain-text rows', () => {
    expect(accountTemplateGrid('Settlement')).toEqual({ heading: 'Settlement', footerLabel: 'Total', columns: 'balance', linksRows: false })
  })
})
