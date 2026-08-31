import { useState, type KeyboardEvent } from 'react'
import { Button, Field, Input, Tag, TagGroup } from '@fluentui/react-components'
import { AddRegular } from '@fluentui/react-icons'

interface AliasesInputProps {
  aliases: string[]
  onChange: (aliases: string[]) => void
  disabled?: boolean
}

export default function AliasesInput({ aliases, onChange, disabled = false }: AliasesInputProps) {
  const [newAlias, setNewAlias] = useState('')

  const addAlias = () => {
    const trimmed = newAlias.trim()
    if (trimmed.length === 0) return
    if (aliases.some((a) => a.toLowerCase() === trimmed.toLowerCase())) {
      setNewAlias('')
      return
    }

    onChange([...aliases, trimmed])
    setNewAlias('')
  }

  const removeAlias = (alias: string) => onChange(aliases.filter((a) => a !== alias))

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      addAlias()
    }
  }

  return (
    <Field label="Aliases">
      <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
        <Input
          value={newAlias}
          onChange={(e) => setNewAlias(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={disabled}
          aria-label="New alias"
        />
        <Button icon={<AddRegular />} onClick={addAlias} disabled={disabled} aria-label="Add alias">
          Add
        </Button>
      </div>
      {aliases.length > 0 && (
        <TagGroup dismissible onDismiss={(_, data) => removeAlias(String(data.value))} aria-label="Aliases">
          {aliases.map((alias) => (
            <Tag key={alias} value={alias} disabled={disabled}>
              {alias}
            </Tag>
          ))}
        </TagGroup>
      )}
    </Field>
  )
}
