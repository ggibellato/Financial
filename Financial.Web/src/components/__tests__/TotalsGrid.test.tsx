import { fireEvent, render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import TotalsGrid from '../TotalsGrid'

interface Row {
  id: string
  label: string
  amount: number
}

const ROWS: Row[] = [
  { id: 'b', label: 'Bravo', amount: 20 },
  { id: 'a', label: 'Alpha', amount: 5 },
]

const COLUMNS = [
  { key: 'label', header: 'Label', render: (r: Row) => r.label, sortAccessor: (r: Row) => r.label },
  {
    key: 'amount',
    header: 'Amount',
    numeric: true,
    render: (r: Row) => String(r.amount),
    sortAccessor: (r: Row) => r.amount,
  },
]

describe('TotalsGrid', () => {
  it('renders rows in their original order with no sort applied', () => {
    render(<TotalsGrid columns={COLUMNS} rows={ROWS} rowKey={(r) => r.id} footerItems={[]} />)

    const dataRows = screen.getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Bravo')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('Alpha')).toBeInTheDocument()
  })

  it('sorts rows by the clicked column ascending, then descending, then back to original order', () => {
    render(<TotalsGrid columns={COLUMNS} rows={ROWS} rowKey={(r) => r.id} footerItems={[]} />)

    const amountHeader = screen.getByRole('button', { name: 'Amount' })

    fireEvent.click(amountHeader)
    let dataRows = screen.getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Alpha')).toBeInTheDocument()

    fireEvent.click(amountHeader)
    dataRows = screen.getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Bravo')).toBeInTheDocument()

    fireEvent.click(amountHeader)
    dataRows = screen.getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Bravo')).toBeInTheDocument()
  })

  it('renders footer items outside the sortable row set, unaffected by sorting', () => {
    render(
      <TotalsGrid
        columns={COLUMNS}
        rows={ROWS}
        rowKey={(r) => r.id}
        footerItems={[{ label: 'Total', value: '25' }]}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Amount' }))

    expect(screen.getByText('Total:')).toBeInTheDocument()
    expect(screen.getByText('25')).toBeInTheDocument()
  })

  it('renders a sort button per column and nothing else interactive', () => {
    render(<TotalsGrid columns={COLUMNS} rows={ROWS} rowKey={(r) => r.id} footerItems={[]} />)

    expect(screen.getAllByRole('button')).toHaveLength(2)
  })
})
