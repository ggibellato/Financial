import { fireEvent, render, screen, within } from '@testing-library/react'
import { Table, TableBody, TableRow } from '@fluentui/react-components'
import { describe, expect, it, vi } from 'vitest'
import RowActionsCell from '../RowActionsCell'

function renderCell() {
  const onEdit = vi.fn()
  const onDelete = vi.fn()
  render(
    <Table>
      <TableBody>
        <TableRow>
          <RowActionsCell itemName="Chase" onEdit={onEdit} onDelete={onDelete} />
        </TableRow>
      </TableBody>
    </Table>,
  )
  return { onEdit, onDelete }
}

describe('RowActionsCell', () => {
  it('names the icon buttons after the row item', () => {
    renderCell()

    expect(screen.getByRole('button', { name: 'Edit Chase' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Delete Chase' })).toBeInTheDocument()
  })

  it('calls onEdit and onDelete from their buttons', () => {
    const { onEdit, onDelete } = renderCell()

    fireEvent.click(screen.getByRole('button', { name: 'Edit Chase' }))
    fireEvent.click(screen.getByRole('button', { name: 'Delete Chase' }))

    expect(onEdit).toHaveBeenCalledOnce()
    expect(onDelete).toHaveBeenCalledOnce()
  })

  it('announces a delete in progress and keeps the button focusable but inert', () => {
    const onDelete = vi.fn()
    render(
      <Table>
        <TableBody>
          <TableRow>
            <RowActionsCell itemName="bill INSS" isDeleting onEdit={vi.fn()} onDelete={onDelete} />
          </TableRow>
        </TableBody>
      </Table>,
    )

    const deleting = screen.getByRole('button', { name: 'Deleting bill INSS' })
    deleting.focus()
    fireEvent.click(deleting)

    expect(deleting).toHaveFocus()
    expect(deleting).toHaveAttribute('aria-disabled', 'true')
    expect(within(deleting).getByRole('progressbar')).toBeInTheDocument()
    expect(onDelete).not.toHaveBeenCalled()
  })

  it('shows only Edit for a row that cannot be deleted', () => {
    render(
      <Table>
        <TableBody>
          <TableRow>
            <RowActionsCell itemName="snapshot Chase" onEdit={vi.fn()} />
          </TableRow>
        </TableBody>
      </Table>,
    )

    expect(screen.getAllByRole('button').map((button) => button.getAttribute('aria-label'))).toEqual(['Edit snapshot Chase'])
  })

  it('shows only Delete for a row that cannot be edited, and nothing for a row with no actions', () => {
    render(
      <Table>
        <TableBody>
          <TableRow>
            <RowActionsCell itemName="refund Chase" onDelete={vi.fn()} />
          </TableRow>
          <TableRow>
            <RowActionsCell itemName="price 15/03/2024" />
          </TableRow>
        </TableBody>
      </Table>,
    )

    expect(screen.getAllByRole('button').map((button) => button.getAttribute('aria-label'))).toEqual(['Delete refund Chase'])
  })

  it('keeps Delete focusable but inert while another delete runs, and says why', () => {
    const onDelete = vi.fn()
    render(
      <Table>
        <TableBody>
          <TableRow>
            <RowActionsCell itemName="bill INSS" isOtherDeleting onEdit={vi.fn()} onDelete={onDelete} />
          </TableRow>
        </TableBody>
      </Table>,
    )

    const blocked = screen.getByRole('button', { name: 'Delete bill INSS' })
    fireEvent.click(blocked)

    expect(blocked).toHaveAttribute('aria-disabled', 'true')
    expect(blocked).toBeEnabled()
    expect(blocked).toHaveAccessibleDescription('Another delete is in progress.')
    expect(onDelete).not.toHaveBeenCalled()
  })

  it('gives an available Delete no blocked description', () => {
    renderCell()

    expect(screen.getByRole('button', { name: 'Delete Chase' })).not.toHaveAccessibleDescription()
  })
})
