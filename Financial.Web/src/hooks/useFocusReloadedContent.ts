import { useEffect, useRef, type RefObject } from 'react'
import { focusReloadedContent, isAwaitingRetryOutcome } from '../utils/retryOutcomeFocus'

export function useFocusReloadedContent(regionRef: RefObject<HTMLElement | null>, isLoading: boolean) {
  const wasLoading = useRef(false)
  const loadsRetryOutcome = useRef(false)

  useEffect(() => {
    if (isLoading && !wasLoading.current) loadsRetryOutcome.current = isAwaitingRetryOutcome()
    if (wasLoading.current && !isLoading && loadsRetryOutcome.current && regionRef.current) focusReloadedContent(regionRef.current)
    wasLoading.current = isLoading
  }, [isLoading, regionRef])
}
