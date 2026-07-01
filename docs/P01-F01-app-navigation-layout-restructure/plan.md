# Implementation Plan: F01 — App Navigation & Layout Restructure

**Prerequisites:**
- Node.js and npm installed
- `Financial.Web` dependencies installed (`npm install`)
- React Router DOM 7.x already present in `package.json`
- Vitest + React Testing Library already configured in `vite.config.ts` and `setupTests.ts`

---

### Stage 1: App Shell & CSS

**1. Global viewport CSS** — Update `index.css` to add `height: 100%` and `overflow: hidden` on `html` and `body`, suppressing page-level scrollbars. Update `App.css` to remove the two-column sidebar grid rules and replace the `.app` layout with a flex-column structure where the nav bar occupies its natural height and the content area fills the remainder. Style the three-item nav bar with an active-link highlight (underline or contrasting background) using the existing CSS variable palette.

**2. App shell restructure** — Rewrite `App.tsx` to remove the `NavigationTreePanel` import and the `.app__sidebar` div. Replace the existing four-item header nav with a three-item nav bar containing `NavLink` elements for "Portfolio Navigator" (`/portfolio-navigator`), "Shares Dividend Check" (`/dividend-check`), and "Read Assets Current Values" (`/current-values`). Keep the `<Outlet />` in the content area. The `NavigationTreePanel` component file itself is not deleted — it remains for F02 to embed inside the Portfolio Navigator section.

---

### Stage 2: Routes & Components

**3. Portfolio Navigator placeholder** — Create `src/pages/PortfolioNavigatorPage.tsx` as a minimal functional component that renders a centred placeholder message. This component has no data fetching and no props; it will be replaced entirely by F02.

**4. Router restructure and page cleanup** — Update `main.tsx` to remove all legacy route definitions (`/brokers`, `/brokers/:brokerName`, `/assets/:brokerName/:portfolioName/:assetName`, `/credits/:brokerName`, `/credits/:brokerName/:portfolioName`, `/navigation`, `/dividends-check`). Define three section routes mapping `/portfolio-navigator` to `PortfolioNavigatorPage`, `/dividend-check` to the existing `DividendCheckPage`, and `/current-values` to the existing `CurrentValuesPage`. Change the default `/` redirect from `/brokers` to `/portfolio-navigator`. Retain the `*` catch-all 404 route. After the router is updated, delete the five now-unreachable page component files (`BrokersPage.tsx`, `BrokerDetailPage.tsx`, `AssetDetailPage.tsx`, `CreditsPage.tsx`, `NavigationTreePage.tsx`) and their co-located test files from `src/pages/__tests__/`.

**5. Tests** — Create `src/__tests__/App.test.tsx` covering: three nav labels render, default route redirects to the Portfolio Navigator placeholder, clicking each nav link mounts the correct section, the active nav item carries the active CSS class, and legacy routes (`/brokers`, `/navigation`) render the 404 page. Create `src/pages/__tests__/PortfolioNavigatorPage.test.tsx` asserting the placeholder message renders. Run the full test suite to confirm no regressions in the retained `DividendCheckPage` and `CurrentValuesPage` tests, and in the `NavigationTreePanel` component tests.
