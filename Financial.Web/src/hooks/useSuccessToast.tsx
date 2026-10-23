import { useCallback } from 'react'
import { Toast, ToastTitle, useToastController } from '@fluentui/react-components'

export const APP_TOASTER_ID = 'app-toaster'

export function useSuccessToast() {
  const { dispatchToast } = useToastController(APP_TOASTER_ID)

  const showSuccess = useCallback(
    (message: string) =>
      dispatchToast(
        <Toast>
          <ToastTitle>{message}</ToastTitle>
        </Toast>,
        { intent: 'success' },
      ),
    [dispatchToast],
  )

  return { showSuccess }
}
