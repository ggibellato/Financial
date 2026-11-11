import { renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ApiError } from '../../api/apiError'
import { duplicateNameRejection } from '../../test-utils/duplicateName'
import { useDuplicateNameField } from '../useDuplicateNameField'

const DUPLICATE_MESSAGE = 'A broker named "XPI" already exists.'

interface Props {
  failure: unknown
  error: string | null
  name: string
}

function renderField(initial: Props) {
  return renderHook((props: Props) => useDuplicateNameField(props.failure, props.error, props.name), { initialProps: initial })
}

const duplicate = () => duplicateNameRejection(DUPLICATE_MESSAGE)

describe('useDuplicateNameField', () => {
  it('reports nothing before a save fails', () => {
    const { result } = renderField({ failure: null, error: null, name: 'XPI' })

    expect(result.current.nameError).toBeNull()
    expect(result.current.summaryError).toBeNull()
  })

  it('puts a duplicate-name rejection under the name and out of the summary', () => {
    const { result } = renderField({ failure: duplicate(), error: DUPLICATE_MESSAGE, name: 'XPI' })

    expect(result.current.nameError).toBe(DUPLICATE_MESSAGE)
    expect(result.current.summaryError).toBeNull()
  })

  it('clears the name error once the name changes, without moving it to the summary', () => {
    const failure = duplicate()
    const { result, rerender } = renderField({ failure, error: DUPLICATE_MESSAGE, name: 'XPI' })

    rerender({ failure, error: DUPLICATE_MESSAGE, name: 'XPI 2' })

    expect(result.current.nameError).toBeNull()
    expect(result.current.summaryError).toBeNull()
  })

  it('flags the name again when a later save is rejected for the name it now holds', () => {
    const first = duplicate()
    const { result, rerender } = renderField({ failure: first, error: DUPLICATE_MESSAGE, name: 'XPI' })
    rerender({ failure: first, error: DUPLICATE_MESSAGE, name: 'Clear' })
    rerender({ failure: null, error: null, name: 'Clear' })
    rerender({ failure: duplicate(), error: 'A broker named "Clear" already exists.', name: 'Clear' })

    expect(result.current.nameError).toBe('A broker named "Clear" already exists.')
  })

  it.each([
    ['a conflict without the duplicate-name code', new ApiError(DUPLICATE_MESSAGE, 409)],
    ['a validation error', new ApiError('Name is too long.', 400)],
    ['a plain error', new Error('Server unavailable.')],
  ])('keeps %s in the summary and leaves the name valid', (_, failure) => {
    const { result } = renderField({ failure, error: failure.message, name: 'XPI' })

    expect(result.current.nameError).toBeNull()
    expect(result.current.summaryError).toBe(failure.message)
  })
})
