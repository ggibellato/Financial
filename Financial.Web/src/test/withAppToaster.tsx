import { FluentProvider, Toaster } from '@fluentui/react-components'
import type { ReactNode } from 'react'
import { APP_TOASTER_ID } from '../hooks/useSuccessToast'
import { financialLightTheme } from '../theme/fluentTheme'

export function WithAppToaster({ children }: { children: ReactNode }) {
  return (
    <FluentProvider theme={financialLightTheme}>
      <Toaster toasterId={APP_TOASTER_ID} />
      {children}
    </FluentProvider>
  )
}
