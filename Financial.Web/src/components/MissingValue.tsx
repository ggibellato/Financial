import { MISSING_VALUE_PLACEHOLDER } from '../utils/formatters'

interface MissingValueProps {
  label?: string
}

export default function MissingValue({ label = 'Not available' }: MissingValueProps) {
  return (
    <span role="img" aria-label={label}>
      {MISSING_VALUE_PLACEHOLDER}
    </span>
  )
}
