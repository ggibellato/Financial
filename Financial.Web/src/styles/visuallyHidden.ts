import type { GriffelStyle } from '@fluentui/react-components'

export const visuallyHidden: GriffelStyle = {
  position: 'absolute',
  width: '1px',
  height: '1px',
  overflow: 'hidden',
  clip: 'rect(0 0 0 0)',
  whiteSpace: 'nowrap',
}
