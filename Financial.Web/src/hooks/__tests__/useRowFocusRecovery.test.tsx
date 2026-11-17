import { useRef, useState } from 'react'
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
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

function NoteButton() {
  const [notes, setNotes] = useState(0)
  return (
    <>
      <button tabIndex={-1} onClick={() => setNotes((count) => count + 1)}>
        Add note
      </button>
      <ul>
        {Array.from({ length: notes }, (_, index) => (
          <li key={index}>Note {index + 1}</li>
        ))}
      </ul>
    </>
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
      <NoteButton />
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

function SelfDeletingGrid({ redrawsBody = false }: { redrawsBody?: boolean }) {
  const [rows, setRows] = useState(['Rent', 'Food', 'Fuel'])
  const remove = (name: string) => setRows((current) => current.filter((row) => row !== name))
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
          <tbody key={redrawsBody ? rows.join() : 'rows'}>
            {rows.map((row) => (
              <tr key={row}>
                <td tabIndex={-1} onClick={() => remove(row)}>
                  {row}
                </td>
                <td>
                  <button onClick={() => remove(row)}>Edit {row}</button>
                  <button onClick={() => remove(row)}>Delete {row}</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}

function removeWhileFocused(element: HTMLElement) {
  element.focus()
  fireEvent.click(element)
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

  it.each([
    ['the same table body', false, 'Edit'],
    ['a table body drawn again', true, 'Delete'],
  ])('focuses the same control in the next row of %s when the focused row goes away', async (_, redrawsBody, control) => {
    render(<SelfDeletingGrid redrawsBody={redrawsBody} />)

    removeWhileFocused(screen.getByRole('button', { name: `${control} Food` }))

    await waitFor(() => expect(screen.getByRole('button', { name: `${control} Fuel` })).toHaveFocus())
  })

  it('leaves focus to the page when a cell rather than a control of the row had it', async () => {
    render(<SelfDeletingGrid />)

    removeWhileFocused(screen.getByText('Food'))

    expect(screen.queryByText('Food')).not.toBeInTheDocument()
    expect(document.body).toHaveFocus()
  })

  it('keeps focus in an open dialog while other parts of the page change', async () => {
    deleteFoodWhileTheOverlayHoldsFocus()

    fireEvent.click(screen.getByRole('button', { name: 'Add note' }))
    await act(async () => {})

    expect(screen.getByRole('button', { name: 'Confirm' })).toHaveFocus()
  })
})
