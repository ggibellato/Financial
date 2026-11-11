import type { ReactNode } from 'react'
import MissingValue from '../components/MissingValue'

export function orMissing<T>(value: T | null | undefined, format: (value: T) => ReactNode, missingLabel?: string): ReactNode {
  return value == null ? <MissingValue label={missingLabel} /> : format(value)
}
