import type { AssetAdminDto } from '../api/types'

export interface TargetAssetIdentity {
  isin: string
  exchange: string
  ticker: string
  country: string
  assetClass: string
}

export function findAssetByName(options: AssetAdminDto[], name: string): AssetAdminDto | undefined {
  const target = name.trim().toLowerCase()
  return options.find((asset) => asset.name.toLowerCase() === target)
}

export interface TargetAssetPickerValue {
  assetName: string
  identity: TargetAssetIdentity
}

export const BLANK_TARGET_ASSET_IDENTITY: TargetAssetIdentity = {
  isin: '',
  exchange: '',
  ticker: '',
  country: 'Unknown',
  assetClass: 'Unknown',
}

export const BLANK_TARGET_ASSET_PICKER_VALUE: TargetAssetPickerValue = {
  assetName: '',
  identity: BLANK_TARGET_ASSET_IDENTITY,
}
