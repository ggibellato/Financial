import { makeStyles } from '@fluentui/react-components'

export const useVisuallyHiddenStyles = makeStyles({
  visuallyHidden: {
    position: 'absolute',
    width: '1px',
    height: '1px',
    overflow: 'hidden',
    clip: 'rect(0 0 0 0)',
    whiteSpace: 'nowrap',
  },
})
