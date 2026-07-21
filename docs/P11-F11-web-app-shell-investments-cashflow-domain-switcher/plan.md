# Implementation Plan: F11. Web — App Shell: Investments/CashFlow Domain Switcher

**Prerequisites:**
- Existing Financial.Web toolchain (Vite, React Router v7, Vitest, React Testing Library) — no new dependencies

### Stage 1: Domain Persistence and Redirect Helpers

**1. Domain storage helper** - Add a small sessionStorage-backed utility for reading and writing the currently selected domain ("investments" or "cashflow"), defensive against environments where sessionStorage is unavailable.

**2. Root redirect page** - Add a component that reads the persisted domain and redirects the root path to that domain's default tab, defaulting to Investments when nothing is stored yet.

### Stage 2: Layout Components

**3. Investments layout** - Add a layout component that renders the 4 existing Investments tab links exactly as they appear today, with a nested outlet for the active page.

**4. CashFlow layout** - Add a layout component that renders the 6 CashFlow tab links (Monthly, Reserva, Mensais, Controle Mae, Investment Snapshots, Yearly Summary), with a nested outlet for the active page.

**5. Shared CashFlow placeholder page** - Add one reusable placeholder page component, parameterized by a title, to serve as the temporary destination for all 6 CashFlow routes until F12–F17 replace them individually.

### Stage 3: App Shell and Routing Restructure

**6. Top-level domain switcher** - Restructure the app shell component to render only the 2-option Investments/CashFlow switcher and persist the active domain on every route change, moving the existing tab-row markup out into the new Investments layout.

**7. Route tree restructure** - Nest the 4 existing Investments routes under the Investments layout at `/investments/*`, add the 6 CashFlow placeholder routes under the CashFlow layout at `/cashflow/*`, and wire the root path to the new redirect component, keeping the existing catch-all "not found" route.

**8. Shell and layout styling** - Update the shell stylesheet for the new two-tier nav and add styling for the two layout components and the placeholder page, consistent with the app's existing BEM-ish naming and theme variables.

### Stage 4: Verification

**9. Update and extend tests** - Update the existing app-shell test to match the new two-tier nav and add coverage for both layout components, the shared placeholder page, the root redirect, and the domain storage helper.

**10. Manual verification** - Run the app locally and confirm: the switcher shows exactly Investments and CashFlow, each existing Investments page still renders and behaves the same under its new URL, all 6 CashFlow tabs render the placeholder, and reloading after switching to CashFlow reopens on CashFlow.
