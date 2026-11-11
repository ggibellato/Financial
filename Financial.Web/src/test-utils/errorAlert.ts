import { messageBarClassNames } from '@fluentui/react-components'
import { screen, type ByRoleOptions } from '@testing-library/react'

const isMessageBar = (_name: string, element: Element) => element.classList.contains(messageBarClassNames.root)

export function getErrorAlert(options: ByRoleOptions = {}) {
  return screen.getByRole('group', { ...options, name: isMessageBar })
}

export function findErrorAlert(options: ByRoleOptions = {}) {
  return screen.findByRole('group', { ...options, name: isMessageBar })
}
