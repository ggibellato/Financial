import { fireEvent, render, screen } from '@testing-library/react'
import { Table, TableBody, TableRow } from '@fluentui/react-components'
import { describe, expect, it, vi } from 'vitest'
import RowActionsCell from '../RowActionsCell'

function renderCell(deleteDisabled = false) {
  const onEdit = vi.fn()
  const onDelete = vi.fn()
  render(
    <Table>
      <TableBody>
        <TableRow>
          <RowActionsCell itemName="Chase" deleteDisabled={deleteDisabled} onEdit={onEdit} onDelete={onDelete} />
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

  it('disables only the delete button when deletion is unavailable', () => {
    renderCell(true)

    expect(screen.getByRole('button', { name: 'Delete Chase' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Edit Chase' })).toBeEnabled()
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
})
