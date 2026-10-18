# Implementation Plan: Parity Toolkit

**Prerequisites:**
- PowerShell 7 (`pwsh`), .NET 10 SDK and git on PATH; Node 20+ only for `-KeepRunning`
- The PRD PR (#1056) merged; tag `p60-baseline` created locally on its merge commit on `main`
- Branch `feature/P60-F01-parity-toolkit` from that commit
- A copy of the live CashFlow file outside `data/data-cashflow.json` (e.g. `data/2026-10-18-live-data-cashflow.json`)

### Stage 1: Report and compare logic

**1. Figure catalogue and report builder** - Create the parity module with the endpoint catalogue and a report builder that turns API responses into the flat, sorted figures report described in the spec. The builder takes a fetch function so it works on saved responses as well as a live API.

**2. Script-computed figures** - Add the Monthly figures the browser computes today, plus the pot balances at month end from the movements list, using exact decimal arithmetic.

**3. Compare and figure map** - Add the key-by-key compare with the figure-map rules, the difference lines and the summary messages, and add an empty `figure-map.json`.

**4. Month range and guards** - Add the month-range calculation from the input copy and the live-file, port and hash guards.

### Stage 2: Orchestration

**5. Entry script** - Create `Invoke-Parity.ps1` with the step and option contract from the spec. It runs the guards first, creates the work folder and `run.json`, and prints the progress lines and the final result.

**6. Builds and API processes** - Implement the Baseline and After steps: a baseline worktree at the tag, publishes into the work folder, the neutral environment with local stores, readiness and provider checks, report export and guaranteed cleanup. Add the web-app build and keep-running path for `-KeepRunning`, and the plain-copy Migrate step.

### Stage 3: Self-test, CI and documentation

**7. Self-test and CI step** - Add `Parity.test.ps1` covering the module and the Compare step, and run it from the `changes` job next to the existing script self-tests.

**8. README and screen checklist** - Write the README (commands, work folder, figure catalogue, extending the report and the map, safety rules) and the screen checklist for the side-by-side check.
