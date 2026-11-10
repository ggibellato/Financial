import type { ReactElement } from 'react'
import { Table, TableHeader, TableRow } from '@fluentui/react-components'
import { describe, expect, it } from 'vitest'
import { render, screen } from '../../../test/renderWithFluent'
import ActionsColumnHeader from '../ActionsColumnHeader'

const renderInHeaderRow = (header: ReactElement) =>
  render(
    <Table>
      <TableHeader>
        <TableRow>{header}</TableRow>
      </TableHeader>
    </Table>,
  )

describe('ActionsColumnHeader', () => {
  it('names the row-actions column for assistive technology without visible text', () => {
    renderInHeaderRow(<ActionsColumnHeader />)

    const header = screen.getByRole('columnheader', { name: 'Actions' })
    expect(header).toHaveTextContent('')
    expect(header).toHaveClass('data-table__col--action')
  })

  it('keeps a wider action column class when one is given', () => {
    renderInHeaderRow(<ActionsColumnHeader className="data-table__col--action-wide" />)

    expect(screen.getByRole('columnheader', { name: 'Actions' })).toHaveClass('data-table__col--action-wide')
  })
})
