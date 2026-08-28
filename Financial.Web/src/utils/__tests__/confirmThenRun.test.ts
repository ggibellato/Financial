import { describe, expect, it, vi } from 'vitest'
import { confirmThenRun } from '../confirmThenRun'

describe('confirmThenRun', () => {
  it('runs the callback when the prompt is accepted', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    const run = vi.fn()

    confirmThenRun('Delete this?', run)

    expect(window.confirm).toHaveBeenCalledWith('Delete this?')
    expect(run).toHaveBeenCalledTimes(1)
  })

  it('does not run the callback when the prompt is declined', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    const run = vi.fn()

    confirmThenRun('Delete this?', run)

    expect(run).not.toHaveBeenCalled()
  })
})
