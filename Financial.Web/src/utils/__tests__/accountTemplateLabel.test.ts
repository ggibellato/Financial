import { describe, expect, it } from 'vitest'
import { accountTemplateLabel } from '../accountTemplateLabel'

describe('accountTemplateLabel', () => {
  it('names the Bank template "Bank"', () => {
    expect(accountTemplateLabel('Bank')).toBe('Bank')
  })

  it('names the CreditCard template "Credit card"', () => {
    expect(accountTemplateLabel('CreditCard')).toBe('Credit card')
  })

  it('shows any other template by its name', () => {
    expect(accountTemplateLabel('Pots')).toBe('Pots')
  })
})
