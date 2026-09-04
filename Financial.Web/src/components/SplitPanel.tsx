import { useCallback, useRef, useState } from 'react'
import type { KeyboardEvent, ReactNode } from 'react'
import './SplitPanel.css'

const DEFAULT_LEFT_WIDTH = 300
const MIN_LEFT_WIDTH = 300
const KEYBOARD_STEP = 20

interface SplitPanelProps {
  left: ReactNode
  right: ReactNode
  defaultWidth?: number
  minWidth?: number
}

export default function SplitPanel({
  left,
  right,
  defaultWidth = DEFAULT_LEFT_WIDTH,
  minWidth = MIN_LEFT_WIDTH,
}: SplitPanelProps) {
  const [leftWidth, setLeftWidth] = useState(defaultWidth)
  const startX = useRef(0)
  const startWidth = useRef(0)

  // Matches the drag handler's own bound below, so keyboard and mouse resizing agree on the
  // same maximum.
  const maxWidth = useCallback(() => window.innerWidth / 2, [])

  const onHandleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      startX.current = e.clientX
      startWidth.current = leftWidth
      document.body.style.cursor = 'col-resize'
      document.body.style.userSelect = 'none'

      const handleMouseMove = (ev: MouseEvent) => {
        const delta = ev.clientX - startX.current
        setLeftWidth(Math.max(minWidth, Math.min(startWidth.current + delta, maxWidth())))
      }

      const handleMouseUp = () => {
        document.removeEventListener('mousemove', handleMouseMove)
        document.removeEventListener('mouseup', handleMouseUp)
        document.body.style.cursor = ''
        document.body.style.userSelect = ''
      }

      document.addEventListener('mousemove', handleMouseMove)
      document.addEventListener('mouseup', handleMouseUp)
    },
    [leftWidth, minWidth, maxWidth],
  )

  // WAI-ARIA "window splitter" pattern: arrow keys resize, Home/End jump to the bounds.
  const onHandleKeyDown = useCallback(
    (e: KeyboardEvent) => {
      switch (e.key) {
        case 'ArrowLeft':
          e.preventDefault()
          setLeftWidth((w) => Math.max(minWidth, w - KEYBOARD_STEP))
          break
        case 'ArrowRight':
          e.preventDefault()
          setLeftWidth((w) => Math.min(w + KEYBOARD_STEP, maxWidth()))
          break
        case 'Home':
          e.preventDefault()
          setLeftWidth(minWidth)
          break
        case 'End':
          e.preventDefault()
          setLeftWidth(maxWidth())
          break
      }
    },
    [minWidth, maxWidth],
  )

  return (
    <div className="split-panel">
      <div className="split-panel__left" style={{ width: leftWidth }}>
        {left}
      </div>
      <div
        className="split-panel__handle"
        role="separator"
        aria-orientation="vertical"
        aria-label="Resize panel"
        aria-valuenow={Math.round(leftWidth)}
        aria-valuemin={minWidth}
        aria-valuemax={Math.round(maxWidth())}
        tabIndex={0}
        onMouseDown={onHandleMouseDown}
        onKeyDown={onHandleKeyDown}
      />
      <div className="split-panel__right">{right}</div>
    </div>
  )
}
