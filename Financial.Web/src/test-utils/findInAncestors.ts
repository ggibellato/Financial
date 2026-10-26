import { existsSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'

// StrykerJS runs the tests from a copy nested under Financial.Web/.stryker-tmp, so a fixed
// number of '../' segments points nowhere there.
export function findInAncestors(relativePath: string, startDirectory: string): string {
  let directory = resolve(startDirectory)
  while (!existsSync(join(directory, relativePath))) {
    const parent = dirname(directory)
    if (parent === directory) throw new Error(`${relativePath} was not found above ${startDirectory}`)
    directory = parent
  }
  return join(directory, relativePath)
}
