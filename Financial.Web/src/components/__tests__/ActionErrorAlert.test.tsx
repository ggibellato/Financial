import { describe, expect, it } from 'vitest'
import { render, screen } from '../../test/renderWithFluent'
import ActionErrorAlert from '../ActionErrorAlert'

describe('ActionErrorAlert', () => {
  it('takes focus and is named by its message, without a second live-region announcement', () => {
    render(<ActionErrorAlert message="The credit could not be deleted. Try again." />)

    const alert = screen.getByRole('group', { name: 'The credit could not be deleted. Try again.' })
    expect(alert).toHaveFocus()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('renders nothing without a message', () => {
    render(<ActionErrorAlert message={null} />)

    expect(screen.queryByRole('group')).not.toBeInTheDocument()
  })
})
