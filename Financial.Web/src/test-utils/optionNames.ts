export function optionNames(select: HTMLElement): (string | null)[] {
  return Array.from(select.querySelectorAll('option')).map((option) => option.textContent)
}
