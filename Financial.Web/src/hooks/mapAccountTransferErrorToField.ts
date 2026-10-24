import type { TransferFormField } from './mapTransferErrorToField'

export type TransferDirection = 'to' | 'from'

const MESSAGE_FIELDS: [RegExp, TransferFormField][] = [
  [/must not be in the future/, 'date'],
  [/Destination amount|Enter the amount received/, 'destinationAmount'],
  [/Transfer amount/, 'amount'],
  [/Note must be/, 'note'],
]

const COUNTERPART_MESSAGE = /different accounts|not allowed|is inactive/

export function counterpartField(direction: TransferDirection): 'sourceBank' | 'destinationBank' {
  return direction === 'to' ? 'destinationBank' : 'sourceBank'
}

export function mapAccountTransferErrorToField(message: string, direction: TransferDirection): TransferFormField | null {
  const match = MESSAGE_FIELDS.find(([pattern]) => pattern.test(message))
  if (match) return match[1]
  return COUNTERPART_MESSAGE.test(message) ? counterpartField(direction) : null
}
