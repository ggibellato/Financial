export function isAssetNameCollisionError(message: string): boolean {
  return /^An asset named ".+" already exists/.test(message)
}
