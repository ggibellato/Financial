import { useEffect, useRef, type RefObject } from 'react'
import { focusReloadedContent } from '../utils/retryOutcomeFocus'

export function useFocusReloadedContent(regionRef: RefObject<HTMLElement | null>, isLoading: boolean) {
  const wasLoading = useRef(isLoading)

  useEffect(() => {
    if (wasLoading.current && !isLoading && regionRef.current) focusReloadedContent(regionRef.current)
    wasLoading.current = isLoading
  }, [isLoading, regionRef])
}
