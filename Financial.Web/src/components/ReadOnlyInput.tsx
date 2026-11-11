import { forwardRef } from 'react'
import { Input, type InputProps } from '@fluentui/react-components'

type ReadOnlyInputProps = Omit<InputProps, 'readOnly' | 'appearance' | 'onChange'>

const ReadOnlyInput = forwardRef<HTMLInputElement, ReadOnlyInputProps>((props, ref) => (
  <Input {...props} ref={ref} readOnly appearance="filled-darker" />
))

ReadOnlyInput.displayName = 'ReadOnlyInput'

export default ReadOnlyInput
