import { act, screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { ArcaAllocationDto, SelectedNode } from '../../api/types'
import { holdingsTotals } from '../../test-utils/holdingsTotals'
import { renderWithSelectedNode } from '../../test-utils/renderWithSelectedNode'
import ArcaPanel from '../ArcaPanel'

const { getBrokerArcaMock } = vi.hoisted(() => ({
  getBrokerArcaMock: vi.fn<FinancialApiClient['getBrokerArca']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { getBrokerArca: getBrokerArcaMock } as Partial<FinancialApiClient>,
}))

const BROKER_NODE: SelectedNode = { nodeType: 'Broker', brokerName: 'XPI', currency: 'BRL' }

const valued = (marketValue: number, marketShareOfBase: number | null) =>
  holdingsTotals({ holdingCount: 2, marketValue, marketShareOfBase })

const ARCA: ArcaAllocationDto = {
  buckets: [
    { bucket: 'Shares', totals: valued(200, 20) },
    { bucket: 'RealEstate', totals: valued(300, 30) },
    { bucket: 'Cash', totals: valued(100, 10) },
    { bucket: 'Pension', totals: valued(400, 40) },
  ],
  arcaTotal: valued(1000, 100),
  reserveFund: valued(250, null),
  notInArca: null,
  arcaAndReserveFundTotal: valued(1250, null),
}

async function renderPanel(arca: ArcaAllocationDto | null) {
  getBrokerArcaMock.mockResolvedValue({ currencies: ['BRL'], arca })
  renderWithSelectedNode(<ArcaPanel />, BROKER_NODE)
  if (arca) await screen.findByRole('table', { name: 'ARCA allocation' })
}

const rowTexts = () =>
  within(screen.getByRole('table', { name: 'ARCA allocation' }))
    .getAllByRole('row')
    .slice(1)
    .map(row => [
      within(row).getByRole('rowheader').textContent,
      ...within(row).getAllByRole('cell').map(cell => cell.textContent?.replace(/^[^:]+:/, '')),
    ])

describe('ArcaPanel', () => {
  beforeEach(() => {
    getBrokerArcaMock.mockReset()
  })

  it('lists_the_reserve_fund_each_bucket_and_footer_totals_with_their_currency', async () => {
    await renderPanel(ARCA)

    expect(getBrokerArcaMock).toHaveBeenCalledWith('XPI')
    expect(screen.getByText('Current values, amounts in BRL')).toBeInTheDocument()
    expect(screen.getAllByRole('columnheader').map(h => h.textContent)).toEqual(['Bucket', 'Current value', '% of ARCA'])
    expect(rowTexts()).toEqual([
      ['Reserve fund (3–12 months of expenses)', '250.00', '—'],
      ['A – Shares and businesses', '200.00', '20.00%'],
      ['R – Real estate', '300.00', '30.00%'],
      ['C – Cash and fixed income', '100.00', '10.00%'],
      ['Pension', '400.00', '40.00%'],
      ['ARCA total', '1,000.00', '100.00%'],
      ['Grand total (ARCA + Reserve fund)', '1,250.00', '—'],
    ])
    expect(screen.getAllByRole('img', { name: 'Not part of the ARCA percentages' })).toHaveLength(2)
  })

  it('is_not_shown_for_a_broker_without_arca', async () => {
    getBrokerArcaMock.mockResolvedValue({ currencies: ['BRL'], arca: null })
    renderWithSelectedNode(<ArcaPanel />, BROKER_NODE)

    await vi.waitFor(() => expect(getBrokerArcaMock).toHaveBeenCalled())
    await act(async () => {
      await getBrokerArcaMock.mock.results[0].value
    })
    expect(screen.queryByRole('heading', { name: 'ARCA allocation' })).not.toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('says_there_are_no_valued_holdings_when_the_arca_total_is_zero', async () => {
    const unvalued = holdingsTotals({ holdingCount: 2, unvaluedHoldingCount: 2 })
    await renderPanel({ ...ARCA, buckets: [{ bucket: 'Shares', totals: unvalued }], arcaTotal: unvalued, reserveFund: null, arcaAndReserveFundTotal: unvalued })

    const status = screen.getByRole('status')
    expect(within(status).getByText('No valued holdings in ARCA.')).toBeInTheDocument()
    expect(within(status).getByText('2 of 2 holdings could not be valued; the total is incomplete.')).toBeInTheDocument()
  })

  it('does_not_say_no_valued_holdings_when_the_arca_holdings_are_valued_at_zero', async () => {
    await renderPanel({ ...ARCA, buckets: [{ bucket: 'Pension', totals: valued(0, null) }], arcaTotal: valued(0, null), reserveFund: null })

    expect(screen.queryByText('No valued holdings in ARCA.')).not.toBeInTheDocument()
  })

  it('notes_the_portfolios_not_in_arca_that_both_totals_leave_out', async () => {
    await renderPanel({ ...ARCA, notInArca: valued(5000, null) })

    expect(screen.getByText(/Portfolios not in ARCA \(5,000\.00\) are left out of both totals\./)).toBeInTheDocument()
  })

  it('names_the_panel_in_its_error_message_and_offers_a_retry', async () => {
    getBrokerArcaMock.mockRejectedValue(new Error('boom'))
    renderWithSelectedNode(<ArcaPanel />, BROKER_NODE)

    expect(await screen.findByText('The ARCA allocation could not be loaded.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
