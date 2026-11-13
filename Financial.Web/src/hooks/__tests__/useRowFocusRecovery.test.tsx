import { useRef, useState } from 'react'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useRowFocusRecovery } from '../useRowFocusRecovery'

function ClosingOverlay({ onConfirm }: { onConfirm: () => void }) {
  const [isShown, setIsShown] = useState(true)
  return (
    <>
      {isShown && (
        <div role="dialog" aria-modal="true" aria-label="Confirm">
          <button onClick={onConfirm}>Confirm</button>
          <button>Elsewhere</button>
        </div>
      )}
      <button tabIndex={-1} onClick={() => setIsShown(false)}>
        Finish closing
      </button>
    </>
  )
}

function InlineForm({ onSave }: { onSave: () => void }) {
  const [isOpen, setIsOpen] = useState(true)
  if (!isOpen) return null
  return (
    <button
      onClick={() => {
        onSave()
        setIsOpen(false)
      }}
    >
      Save and close
    </button>
  )
}

function Grid() {
  const [rows, setRows] = useState(['Rent', 'Food', 'Fuel'])
  const removeFood = () => setRows((current) => current.filter((row) => row !== 'Food'))
  const regionRef = useRef<HTMLDivElement>(null)
  const headingRef = useRef<HTMLHeadingElement>(null)
  const recordRowFocus = useRowFocusRecovery(regionRef, headingRef, false)
  return (
    <>
      <h2 ref={headingRef} tabIndex={-1}>
        Rows
      </h2>
      <div ref={regionRef} onFocus={recordRowFocus}>
        <table>
          <tbody>
            {rows.map((row) => (
              <tr key={row}>
                <td>{row}</td>
                <td>
                  <button>Delete {row}</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <ClosingOverlay onConfirm={removeFood} />
      <InlineForm onSave={removeFood} />
    </>
  )
}

function deleteFoodWhileTheOverlayHoldsFocus() {
  render(<Grid />)
  screen.getByRole('button', { name: 'Delete Food' }).focus()
  const confirm = screen.getByRole('button', { name: 'Confirm' })
  confirm.focus()
  fireEvent.click(confirm)
  expect(screen.queryByRole('button', { name: 'Delete Food' })).not.toBeInTheDocument()
  expect(confirm).toHaveFocus()
}

describe('useRowFocusRecovery', () => {
  it("focuses the next row's control once an overlay that still held focus when the row went away closes", async () => {
    deleteFoodWhileTheOverlayHoldsFocus()

    fireEvent.click(screen.getByRole('button', { name: 'Finish closing' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'Delete Fuel' })).toHaveFocus())
  })

  it('leaves focus alone once the user moved it after the row went away', async () => {
    deleteFoodWhileTheOverlayHoldsFocus()
    screen.getByRole('button', { name: 'Elsewhere' }).focus()

    fireEvent.click(screen.getByRole('button', { name: 'Finish closing' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(document.body).toHaveFocus()
  })

  it('leaves the row to the page once focus went to a part of the page that is not a modal dialog', async () => {
    render(<Grid />)
    screen.getByRole('button', { name: 'Delete Food' }).focus()
    const save = screen.getByRole('button', { name: 'Save and close' })
    save.focus()

    fireEvent.click(save)

    await waitFor(() => expect(screen.queryByRole('button', { name: 'Save and close' })).not.toBeInTheDocument())
    expect(screen.queryByRole('button', { name: 'Delete Food' })).not.toBeInTheDocument()
    expect(document.body).toHaveFocus()
  })
})
