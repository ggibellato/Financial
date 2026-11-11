import { fireEvent, screen, waitFor } from '@testing-library/react'
import { expect } from 'vitest'
import { ApiError } from '../api/apiError'

export const duplicateNameRejection = (message: string) => new ApiError(message, 409, { code: 'duplicateName' })

export function saveFromTheSaveButton(saveLabel = 'Save') {
  const save = screen.getByRole('button', { name: saveLabel, hidden: true })
  save.focus()
  fireEvent.click(save)
}

export async function expectDuplicateUnderName(message: string) {
  expect(await screen.findByText(message)).toBeInTheDocument()
  const name = screen.getByLabelText(/^Name/)
  expect(name).toHaveAttribute('aria-invalid', 'true')
  expect(name).toHaveAccessibleDescription(message)
  await waitFor(() => expect(name).toHaveFocus())
}
