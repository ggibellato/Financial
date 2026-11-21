import { describe, expect, it } from 'vitest'
import { render, screen } from '../../test/renderWithFluent'
import StatementPotWithdrawalsNote from '../StatementPotWithdrawalsNote'

const GLEISON = { operationId: 'op1', potsAccountName: 'Reserva', potName: 'Gleison', amount: 120 }
const HOUSE_TREATS = { operationId: 'op2', potsAccountName: 'Reserva', potName: 'House Treats', amount: 30.5 }

describe('StatementPotWithdrawalsNote', () => {
  it('lists one line per pot withdrawal, naming the bank and bolding only the amount', () => {
    render(<StatementPotWithdrawalsNote withdrawals={[GLEISON, HOUSE_TREATS]} bankName="Barclays" currency="GBP" />)

    const lines = screen.getAllByRole('listitem')
    expect(lines.map((line) => line.textContent)).toEqual([
      'Also takes £120.00 from Reserva (Gleison) on Barclays.',
      'Also takes £30.50 from Reserva (House Treats) on Barclays.',
    ])
    expect(lines[0].querySelector('strong')?.textContent).toBe('£120.00')
  })

  it('says "the paying bank" until a bank is chosen', () => {
    render(<StatementPotWithdrawalsNote withdrawals={[GLEISON]} currency="GBP" />)

    expect(screen.getByRole('listitem')).toHaveTextContent('Also takes £120.00 from Reserva (Gleison) on the paying bank.')
  })

  it('renders nothing for a statement with no pot withdrawals', () => {
    render(<StatementPotWithdrawalsNote withdrawals={[]} bankName="Barclays" currency="GBP" />)

    expect(screen.queryByRole('list')).not.toBeInTheDocument()
    expect(screen.queryByText(/Also takes/)).not.toBeInTheDocument()
  })
})
