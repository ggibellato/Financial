import { useEffect, useRef, useState, type RefObject } from 'react'
import { flushSync } from 'react-dom'
import { collectFieldErrors, type FieldError } from '../utils/fieldErrors'
import { focusField } from '../utils/focus'

function stillSeveralInvalid(shown: FieldError[], current: FieldError[]): FieldError[] {
  const remaining = shown.flatMap((error) => current.filter((invalid) => invalid.fieldId === error.fieldId))
  return remaining.length > 1 ? remaining : []
}

function sameErrors(a: FieldError[], b: FieldError[]): boolean {
  return a.length === b.length && a.every((error, index) => error.fieldId === b[index].fieldId && error.message === b[index].message)
}

export function useFieldErrorSummary(containerRef: RefObject<HTMLElement | null>) {
  const summaryRef = useRef<HTMLDivElement | null>(null)
  const [summarizedErrors, setSummarizedErrors] = useState<FieldError[]>([])

  const hasSummary = summarizedErrors.length > 0

  useEffect(() => {
    const container = containerRef.current
    if (!container || !hasSummary) return
    const observer = new MutationObserver(() =>
      setSummarizedErrors((shown) => {
        const remaining = stillSeveralInvalid(shown, collectFieldErrors(container))
        return sameErrors(remaining, shown) ? shown : remaining
      }),
    )
    observer.observe(container, { subtree: true, childList: true, attributes: true, attributeFilter: ['aria-invalid'] })
    return () => observer.disconnect()
  }, [containerRef, hasSummary])

  const submitWithSummary = (save: () => void, keepFocusWithin?: HTMLElement | null) => {
    flushSync(save)
    const invalidFields = collectFieldErrors(containerRef.current)
    const hasSeveral = invalidFields.length > 1
    flushSync(() => setSummarizedErrors(hasSeveral ? invalidFields : []))
    if (hasSeveral) summaryRef.current?.focus()
    else if (invalidFields.length === 1 && !keepFocusWithin?.contains(document.activeElement)) focusField(invalidFields[0].fieldId)
  }

  return { summaryRef, summarizedErrors, submitWithSummary }
}
