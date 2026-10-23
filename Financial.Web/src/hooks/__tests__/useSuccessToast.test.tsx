import { Button, Toaster } from '@fluentui/react-components'
import { describe, expect, it } from 'vitest'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import { APP_TOASTER_ID, useSuccessToast } from '../useSuccessToast'

function SaveButton() {
  const { showSuccess } = useSuccessToast()
  return <Button onClick={() => showSuccess('Expense added')}>Save</Button>
}

describe('useSuccessToast', () => {
  it('shows the message in the app toaster', async () => {
    render(
      <>
        <Toaster toasterId={APP_TOASTER_ID} />
        <SaveButton />
      </>,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Expense added')).toBeInTheDocument()
  })
})
