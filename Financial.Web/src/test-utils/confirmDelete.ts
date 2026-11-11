import { fireEvent, screen, within } from '@testing-library/react'

export async function requestDelete(itemName: string, entityLabel: string) {
  const button = await screen.findByRole('button', { name: `Delete ${itemName}`, hidden: true })
  button.focus()
  fireEvent.click(button)
  return screen.findByRole('dialog', { name: `Delete ${entityLabel}`, hidden: true })
}

export async function confirmDelete(itemName: string, entityLabel: string) {
  const dialog = await requestDelete(itemName, entityLabel)
  fireEvent.click(within(dialog).getByRole('button', { name: 'Delete', hidden: true }))
}
