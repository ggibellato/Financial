import { MISSING_VALUE_PLACEHOLDER } from '../utils/formatters'

export default function MissingValue() {
  return <span role="img" aria-label="Not available">{MISSING_VALUE_PLACEHOLDER}</span>
}
