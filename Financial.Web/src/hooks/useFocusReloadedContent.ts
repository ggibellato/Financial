import { useEffect, useRef, type RefObject } from 'react'
import { beginReload } from '../utils/retryOutcomeFocus'

export function useFocusReloadedContent(regionRef: RefObject<HTMLElement | null>, isLoading: boolean) {
  const wasLoading = useRef(false)
  const endReload = useRef<ReturnType<typeof beginReload> | null>(null)

  useEffect(() => {
    if (isLoading && !wasLoading.current) endReload.current = beginReload()
    if (wasLoading.current && !isLoading && regionRef.current) endReload.current?.(regionRef.current)
    wasLoading.current = isLoading
  }, [isLoading, regionRef])
}
