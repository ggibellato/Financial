import type { MouseEvent } from 'react'
import { Tooltip } from '@fluentui/react-components'
import { Link } from 'react-router-dom'

interface TruncatedTextProps {
  text: string | null | undefined
  className?: string
  to?: string
  onClick?: (event: MouseEvent<HTMLAnchorElement>) => void
}

export default function TruncatedText({ text, className, to, onClick }: TruncatedTextProps) {
  if (!text) return null
  const fullClassName = `truncated-text${className ? ` ${className}` : ''}`

  return (
    // "inaccessible": the span's own text already gives screen readers the full string;
    // "label"/"description" would force Fluent to duplicate it into the DOM for aria-describedby.
    <Tooltip content={text} relationship="inaccessible" withArrow>
      {to ? (
        <Link to={to} className={fullClassName} onClick={onClick}>
          {text}
        </Link>
      ) : (
        <span className={fullClassName} tabIndex={0}>
          {text}
        </span>
      )}
    </Tooltip>
  )
}
