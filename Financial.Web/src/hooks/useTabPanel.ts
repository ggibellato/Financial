import { useId } from 'react'

export function useTabPanel(activeTab: string) {
  const baseId = useId()
  const panelId = `${baseId}-panel`
  const tabId = (value: string) => `${baseId}-tab-${value}`

  return {
    tabProps: (value: string) => ({ id: tabId(value), 'aria-controls': panelId }),
    panelProps: { id: panelId, role: 'tabpanel', 'aria-labelledby': tabId(activeTab) },
  }
}
