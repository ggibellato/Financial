import { Tooltip } from '@fluentui/react-components'

interface TruncatedTextProps {
  text: string | null | undefined
  className?: string
}

export default function TruncatedText({ text, className }: TruncatedTextProps) {
  if (!text) return null

  return (
    // The trigger already contains the full text as its own DOM content (only visually truncated
    // via CSS), so screen readers get the whole string from it directly — relationship="inaccessible"
    // avoids Fluent's Tooltip permanently duplicating the text into the DOM for aria-describedby,
    // which it does for "label"/"description" even while hidden.
    <Tooltip content={text} relationship="inaccessible" withArrow>
      <span className={`truncated-text${className ? ` ${className}` : ''}`} tabIndex={0}>
        {text}
      </span>
    </Tooltip>
  )
}
