export interface TargetAssetIdentity {
  isin: string
  exchange: string
  ticker: string
  country: string
  assetClass: string
}

export interface TargetAssetPickerValue {
  assetName: string
  createInline: boolean
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
  createInline: false,
  identity: BLANK_TARGET_ASSET_IDENTITY,
}
