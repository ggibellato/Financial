import type { ReactNode } from 'react'
import MissingValue from '../components/MissingValue'

export function orMissing<T>(value: T | null | undefined, format: (value: T) => ReactNode): ReactNode {
  return value == null ? <MissingValue /> : format(value)
}
