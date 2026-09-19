import { Tooltip } from '@fluentui/react-components'

interface TruncatedTextProps {
  text: string | null | undefined
  className?: string
}

export default function TruncatedText({ text, className }: TruncatedTextProps) {
  if (!text) return null

  return (
    // "inaccessible": the span's own text already gives screen readers the full string;
    // "label"/"description" would force Fluent to duplicate it into the DOM for aria-describedby.
    <Tooltip content={text} relationship="inaccessible" withArrow>
      <span className={`truncated-text${className ? ` ${className}` : ''}`} tabIndex={0}>
        {text}
      </span>
    </Tooltip>
  )
}
