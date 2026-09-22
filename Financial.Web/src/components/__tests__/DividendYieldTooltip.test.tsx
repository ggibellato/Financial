import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { render, screen } from '../../test/renderWithFluent'
import DividendYieldTooltip from '../DividendYieldTooltip'
import type { CreditDto } from '../../api/types'

const BASE_CREDIT: CreditDto = {
  id: 'c1',
  date: '2024-06-01T00:00:00',
  type: 'Dividend',
  value: 400,
  withheld: 0,
  intermediationFee: 0,
  netAmount: 400,
  currency: 'GBP',
  fxRateSnapshot: null,
  sharesForDividend: null,
  attributedShares: null,
  averageCostPerShare: null,
  investedAmount: null,
  priceOnDate: null,
  marketValueOnDate: null,
  yieldOnInvested: null,
  yieldOnMarket: null,
}

describe('DividendYieldTooltip', () => {
  it('renders_nothing_when_attributedShares_is_null', () => {
    render(<DividendYieldTooltip credit={BASE_CREDIT} />)

    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('shows_the_entered_shares_when_sharesForDividend_was_explicit', async () => {
    const user = userEvent.setup()
    const credit: CreditDto = {
      ...BASE_CREDIT,
      sharesForDividend: 800,
      attributedShares: 800,
      averageCostPerShare: 9,
      investedAmount: 7200,
    }
    render(<DividendYieldTooltip credit={credit} />)

    await user.hover(screen.getByRole('button', { name: 'Dividend yield details' }))

    expect(await screen.findByText('800 shares attributed')).toBeInTheDocument()
  })

  it('shows_the_attributed_shares_are_the_entire_position_when_sharesForDividend_was_left_blank', async () => {
    const user = userEvent.setup()
    const credit: CreditDto = {
      ...BASE_CREDIT,
      sharesForDividend: null,
      attributedShares: 1000,
      averageCostPerShare: 9,
      investedAmount: 9000,
    }
    render(<DividendYieldTooltip credit={credit} />)

    await user.hover(screen.getByRole('button', { name: 'Dividend yield details' }))

    expect(await screen.findByText('1000 shares attributed (entire position - none specified)')).toBeInTheDocument()
  })
})
