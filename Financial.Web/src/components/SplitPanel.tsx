import { useCallback, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import './SplitPanel.css'

const DEFAULT_LEFT_WIDTH = 300
const MIN_LEFT_WIDTH = 300

interface SplitPanelProps {
  left: ReactNode
  right: ReactNode
}

export default function SplitPanel({ left, right }: SplitPanelProps) {
  const [leftWidth, setLeftWidth] = useState(DEFAULT_LEFT_WIDTH)
  const startX = useRef(0)
  const startWidth = useRef(0)

  const onHandleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      startX.current = e.clientX
      startWidth.current = leftWidth
      document.body.style.cursor = 'col-resize'
      document.body.style.userSelect = 'none'

      const handleMouseMove = (ev: MouseEvent) => {
        const delta = ev.clientX - startX.current
        const maxWidth = window.innerWidth / 2
        setLeftWidth(Math.max(MIN_LEFT_WIDTH, Math.min(startWidth.current + delta, maxWidth)))
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
    [leftWidth],
  )

  return (
    <div className="split-panel">
      <div className="split-panel__left" style={{ width: leftWidth }}>
        {left}
      </div>
      <div
        className="split-panel__handle"
        onMouseDown={onHandleMouseDown}
        aria-label="Resize panel"
      />
      <div className="split-panel__right">{right}</div>
    </div>
  )
}
