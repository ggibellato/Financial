// Confirmation belongs to the caller, not the data hook — a hook that calls
// window.confirm can only be tested by stubbing a browser global, and it
// would decide for every caller that a prompt is wanted at all.
export function confirmThenRun(message: string, run: () => void): void {
  if (window.confirm(message)) {
    run()
  }
}
