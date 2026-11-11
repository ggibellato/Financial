import { afterEach, describe, expect, it } from 'vitest'
import { collectFieldErrors } from '../fieldErrors'

function mount(html: string) {
  const container = document.createElement('div')
  container.innerHTML = html
  document.body.appendChild(container)
  return container
}

describe('collectFieldErrors', () => {
  afterEach(() => {
    document.body.innerHTML = ''
  })

  it('reads the alert message even when a hint comes first in aria-describedby', () => {
    const container = mount(`
      <input id="name" aria-invalid="true" aria-describedby="hint message" />
      <span id="hint">Up to 60 characters.</span>
      <span id="message" role="alert">Name is required.</span>`)

    expect(collectFieldErrors(container)).toEqual([{ fieldId: 'name', message: 'Name is required.' }])
  })

  it('falls back to the field label when no message describes it', () => {
    const container = mount(`<label for="code">Code</label><input id="code" aria-invalid="true" />`)

    expect(collectFieldErrors(container)).toEqual([{ fieldId: 'code', message: 'Code: check this field.' }])
  })

  it('skips an invalid control without an id, which no link could reach', () => {
    const container = mount(`<input aria-invalid="true" aria-label="Orphan" />`)

    expect(collectFieldErrors(container)).toEqual([])
  })

  it('prefixes the message with the named group the field belongs to', () => {
    const container = mount(`
      <div role="group" aria-label="Pot Savings">
        <input id="split" aria-invalid="true" aria-describedby="split-error" />
        <span id="split-error">Split must be between 0 and 100.</span>
      </div>`)

    expect(collectFieldErrors(container)[0].message).toBe('Pot Savings: Split must be between 0 and 100.')
  })
})
