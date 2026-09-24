import type { CorporateActionFormField } from './useCorporateActions'

export function mapCorporateActionErrorToField(message: string): CorporateActionFormField | null {
  if (/^An asset named ".+" already exists/.test(message)) {
    return 'formTargetAsset'
  }
  return null
}
