# Feature Specification: Valuation Methods and Provenance

**Feature Branch**: `005-valuation-methods-provenance`

**Created**: 2026-09-12

**Status**: Draft

**Input**: User description: "for WAVE 2 P48 at E:\dev\projetos\financial\docs\investment-performance-roadmap.md"
— Wave 2 (P48) of the Investment Performance & Tax Reporting roadmap: "Valuation methods and
provenance… unblocks the two instrument types that cannot be represented today."

## Context

`docs/investment-performance-roadmap.md` §3 (G8, G10) and §6 (Wave 2) is the source of truth for *why*
this feature exists. In short:

- Every asset today is priced by ticker lookup, routed purely by `GlobalAssetClass`. There is no
  explicit valuation method, so a value-based fund or a property-platform investment ("Inco") — whose
  worth is a periodically-supplied total figure, not a per-unit market price — cannot be honestly
  represented at all (G8, roadmap §4).
- A recorded price snapshot carries only a date, a price, and a manual/automatic flag. It has no
  currency, no source, no reference back to that source, no market status, and no record of when it
  was actually retrieved — so a stale or provider-corrected price cannot be told apart from a fresh,
  trustworthy one (G10).

Wave 0 (P46, `specs/003-investment-calculation-core/`) made the existing valuation arithmetic correct
and server-owned. Wave 1 (P47, `specs/004-transaction-income-vocabulary/`) — merged — already widened
the transaction vocabulary enough that a contribution or withdrawal against a holding with no units can
be recorded (`CapitalCall`, `ReturnOfCapital`; both require zero quantity, per
`Financial.Investment.Domain/Entities/Transaction.cs`). What remains open, and what this feature
delivers, is the **valuation** side: classifying how a holding is priced, letting a holding with no
market price be *valued* directly, and making that value's origin and trustworthiness visible.

This feature is scoped narrowly against the roadmap's own sequencing:

- **Out of scope, by design**: multi-currency conversion and a reporting currency are Wave 3 (P49) —
  currency captured here is provenance (what currency a price is denominated in), never a conversion
  capability. Choosing a cost-basis method and persisting disposal records are Wave 4 (P50). Tax
  jurisdiction, classification, and reporting are Wave 5 (P51). A dashboard consuming data-quality
  signals is Wave 6 (P52) — this feature only records and displays the signals themselves. Corporate
  actions are Wave 7 (P53). Separating a bond's clean, dirty, and accrued price remains an open G1
  follow-up, independent of the bond-quote valuation method this feature adds.
- **In scope**: an explicit valuation method per holding; automatic price-fetch routing driven by that
  method; recording a holding's value directly when it has no market price; and provenance (currency,
  source, source reference, market status, retrieved-at) on every price/value record, visible in both
  front ends.

## Clarifications

### Session 2026-09-12

- Q: When a price is fetched automatically, should the snapshot's source record exactly which
  provider supplied it, or just that it was automatic? → A: Record the specific named source — which
  automatic provider (e.g. Google, Yahoo, StatusInvest), or "Manual", or "Provider Valuation" — not a
  coarse "Automatic"/"Manual" category.
- Q: Should a snapshot's "market status" describe the trading venue's session state (market
  open/closed at fetch time), or the snapshot's own freshness/availability (current / stale /
  unavailable)? → A: Freshness/availability of the snapshot itself. This is what directly drives the
  stale-data warning and needs no per-market trading-calendar data the app doesn't otherwise track.
- Q: FR-011 requires a holding's price history to still show which valuation method each old snapshot
  was recorded under, even after the holding's method changes — should each snapshot explicitly record
  the valuation method that produced it, rather than relying on source to imply it? → A: Yes, store it
  explicitly. Source alone can't reliably imply it — the same provider can serve either a market price
  or a NAV — so valuation method is recorded as its own field on every snapshot, independent of source.
- Q: `Asset.PriceHistory` (the widened entity's collection) and the unrelated Active/Historic filing
  concept both use "Historic(al)" language and caused confusion in review — should this feature rename
  the property? → A: Yes — rename `Asset.PriceHistory` to `Asset.PriceSnapshots` (Domain-internal only;
  `AssetDetailsDTO.PriceHistory` and React/WPF names stay as-is). The JSON key renames with it via an
  in-place versioned migration in `InvestmentDataMigrations.cs`, the same mechanism already used for
  P47's Credit.Type "Rent" → "SecuritiesLendingIncome" rename — not a separate migration-tool run. This
  is a plan/implementation-note only; no code changes were made as part of this clarification session.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Valuing a holding that has no market price (Priority: P1)

As the investor, when I hold a value-based fund or a property-platform investment ("Inco") whose worth
is a total figure the provider (or I) supply periodically — not a per-unit price I can look up — I can
record that value directly and see the holding's market value, gain/loss, and return calculated from
it, the same as any other holding.

**Why this priority**: These are the two instrument types the roadmap identifies as **not representable
at all** today (§4). Every other capability in this feature exists to support this one.

**Independent Test**: Classify a real value-based or Inco holding with the appropriate valuation method,
record a total value for it, and confirm the holding's market value, unrealised gain/loss, and return
reflect that recorded value — with no unit quantity or per-unit price required anywhere in the entry.

**Acceptance Scenarios**:

1. **Given** a holding classified as provider-valued, **When** I record a total value for it as of
   today, **Then** the holding's market value updates to that figure without requiring a quantity or a
   per-unit price.
2. **Given** a provider-valued holding with a prior recorded value, **When** I record a new value that
   is lower (a write-down) or exactly zero, **Then** the holding's market value and unrealised gain/loss
   reflect the new figure exactly, distinguishable from a holding that has no value recorded at all.
3. **Given** a holding classified as manually-valued, **When** an automatic price fetch runs for the
   portfolio, **Then** that holding is skipped — its value only ever changes when the investor enters
   one.
4. **Given** a holding whose valuation method changes later in its life (e.g., it starts as
   provider-valued and later becomes exchange-listed), **When** I view its price history, **Then** every
   value recorded before the change is unchanged and still attributed to the method it was recorded
   under.

---

### User Story 2 - Trusting the price shown on screen (Priority: P2)

As the investor, whenever I look at a holding's current price or value, I can immediately tell how
current it is, where it came from, and whether it should be trusted — rather than having to assume every
displayed figure is fresh and reliable.

**Why this priority**: Without this, a stale or failed price fetch is indistinguishable from a live one
in daily use, undermining trust in every other number that price feeds — market value, gain/loss,
return. It depends on the provenance fields this feature adds to every snapshot.

**Independent Test**: View a holding whose most recent price is stale, one that failed to fetch, and one
that is fresh; confirm each is visibly distinguishable (source, as-of date, and status shown, with a
warning on the stale/failed one) in both React and WPF.

**Acceptance Scenarios**:

1. **Given** a holding with a price fetched an hour ago, **When** I view it, **Then** I see its as-of
   date, its source, and a status indicating it is current — with no warning shown.
2. **Given** a holding whose most recent price is older than expected for its market, **When** I view
   it, **Then** I see a visible stale-data warning alongside the as-of date and source.
3. **Given** a holding whose last automatic fetch failed and produced no snapshot, **When** I view it,
   **Then** I see a visible "unavailable" status rather than an old price silently presented as current.
4. **Given** the same holding, **When** I view it in the WPF app instead of the web app, **Then** the
   as-of date, source, market status, and any warning are the same in both.

---

### User Story 3 - Getting the price from the right source (Priority: P3)

As the investor, I want each holding's automatic price fetch to be routed by how it is actually valued —
not only by its instrument classification — so a bond or a cryptocurrency holding is never queried
through an equity ticker lookup, and a holding with an unresolved classification still gets a reasonable
default rather than failing outright.

**Why this priority**: This is a correctness fix on top of existing behaviour (today's routing is by
`GlobalAssetClass` alone), lower-impact than unblocking new instrument types or restoring price trust,
but it removes a latent source of silently wrong or missing prices.

**Independent Test**: Classify one holding of each valuation method and trigger an automatic fetch;
confirm each is routed to a fetch mechanism appropriate to its method, and that a holding with no
explicit classification still resolves to today's default behaviour without error.

**Acceptance Scenarios**:

1. **Given** a holding classified with the bond-quote valuation method, **When** an automatic fetch
   runs, **Then** it is never routed through the exchange-listed equity price lookup.
2. **Given** a holding with an unresolved (`Unknown`) instrument classification, **When** an automatic
   fetch runs, **Then** it still resolves to the same default behaviour as today — this feature does not
   require reclassifying it first.

---

### Edge Cases

- What happens when a holding's valuation method changes mid-life? Every value recorded before the
  change keeps the method it was recorded under; only new snapshots use the new method (User Story 1,
  Scenario 4).
- What happens when a provider-valued holding has never had a value recorded? It must be visibly
  distinct from a holding that has a value recorded but it happens to be zero (User Story 1, Scenario
  2), and distinct from a stale or unavailable price (User Story 2).
- What happens to the roughly 90 holdings with an unresolved `GlobalAssetClass` once an explicit
  valuation method exists? They are not required to be reclassified by this feature — they keep
  resolving through today's default routing (User Story 3, Scenario 2).
- What happens when an automatic fetch is attempted against a manually-valued or provider-valued
  holding? It is skipped entirely; only a manually-entered value changes it (User Story 1, Scenario 3).
- What happens to snapshots recorded before this feature ships, which have no source, valuation method,
  or market status recorded at all? They must be assigned a valid classification automatically as part
  of adopting this feature, without requiring the investor to review each one (see Assumptions).
- What happens when a fetch is attempted for a holding whose valuation method has no working automatic
  source (e.g., a bond with no quote available)? It must produce a visible "unavailable" status, not a
  silently missing or stale-looking price.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST let the investor classify each holding by a valuation method: quoted market
  price, fund NAV, provider-supplied valuation, manual entry, or bond quote.
- **FR-002**: System MUST route an automatic price fetch by the holding's valuation method, not by
  instrument classification alone, so a holding is only ever queried through a source appropriate to how
  it is valued.
- **FR-003**: A holding with no explicit valuation method resolved (including one with an unresolved
  instrument classification) MUST continue to resolve through today's default routing; this feature MUST
  NOT require reclassifying it first.
- **FR-004**: System MUST let the investor record a total value for a holding classified as
  provider-valued or manually-valued directly, without requiring a unit quantity or a per-unit price.
- **FR-005**: Market value, unrealised gain/loss, and return for a provider-valued or manually-valued
  holding MUST be derived from its most recently recorded total value, not from quantity multiplied by a
  per-unit price.
- **FR-006**: A recorded total value of exactly zero MUST be treated as a valid, distinct value (e.g., a
  holding written down to nothing) and never conflated with "no value has been recorded yet."
- **FR-007**: System MUST NOT apply an automatic price fetch to a holding classified as manually-valued
  or provider-valued; only the investor may change its recorded value.
- **FR-008**: Every price or value snapshot, regardless of valuation method, MUST capture: the currency
  it is denominated in, its source, a reference identifying that specific source record, a market status,
  and the instant it was retrieved — in addition to the date it prices. Source MUST identify which
  specific automatic provider supplied it (e.g. Google, Yahoo, StatusInvest), or that it was a manual
  entry, or that it was a provider-supplied valuation — never a coarse "automatic vs. manual" category
  alone, so that a later correction from a different provider is distinguishable from one repeated by
  the same provider. It MUST also capture the valuation method that produced it, as its own field
  independent of source — the same source can serve more than one valuation method (e.g. a market price
  or a NAV from the same provider), so method cannot be safely inferred from source alone.
- **FR-009**: Whether a snapshot is manual MUST be derived from its recorded source rather than stored as
  an independent flag, so the two can never disagree with each other.
- **FR-010**: System MUST let the investor record, for each holding, whether it is expected to
  distribute income (distributing) or retain/reinvest it internally (accumulating), and this
  classification MUST be visible alongside the holding.
- **FR-011**: Changing a holding's valuation method MUST NOT alter or discard any price/value snapshot
  recorded under a previous method; existing history stays exactly as originally recorded, including the
  valuation method (FR-008) each snapshot was recorded under.
- **FR-012**: Snapshots recorded before this feature existed MUST be migrated to a valid source,
  valuation method, and market-status classification automatically, without requiring the investor to
  manually review or reclassify each one.
- **FR-013**: Both front ends MUST display, for a holding's most recent price or value, its as-of date,
  its source, and its market status — where market status describes the snapshot's own freshness and
  availability (e.g. current / stale / unavailable), not the trading venue's session state.
- **FR-014**: Both front ends MUST show a visible warning when a holding's most recent price or value is
  stale or unavailable, rather than presenting an old or missing figure as if it were current.
- **FR-015**: Both front ends MUST present identical valuation-method, source, market-status, and
  staleness information for the same holding at the same time.

### Key Entities *(include if feature involves data)*

- **AssetPriceSnapshot (widened)**: today, one dated price/manual-flag record per holding. This feature
  widens it to also carry currency, a named source (the specific automatic provider, "Manual", or
  "Provider Valuation" — not a coarse automatic/manual category), a reference identifying that specific
  source record, a market status describing the snapshot's own freshness/availability (current / stale /
  unavailable — not the trading venue's session state), the valuation method that produced it (its own
  field, independent of source, so history keeps its original method even after the holding's current
  method changes), and retrieved-at, with the manual flag derived from source. `Asset.PriceHistory` is
  not a separate entity — it is `Asset`'s existing collection of these snapshots; widening the snapshot
  widens what that collection holds, with nothing else to reconcile. **Naming**: as part of this
  feature, rename the `Asset.PriceHistory` property (and backing field) to `Asset.PriceSnapshots` — the
  existing name collides in conversation and in code review with the unrelated Active/Historic filing
  concept (`Investments.ActiveBrokers`/`HistoricBrokers`, `Asset.PositionType`/G14's "Historic" scope),
  even though the two have nothing to do with each other. `AssetDetailsDTO.PriceHistory` (the API wire
  field) and its React/WPF consumer names are unaffected — this is a Domain-internal rename only; the
  DTO shape does not change. The corresponding JSON key in `data-investment.json` renames with it: apply
  it as an in-place versioned migration in `InvestmentDataMigrations.cs` (bump `CurrentVersion`, add a
  key-rename step) — the same mechanism already used for P47's `Credit.Type` "Rent" →
  "SecuritiesLendingIncome" rename — not a separate migration-tool run.
- **Asset.ValuationMethod**: a new classification per holding (market price / NAV / provider value /
  manual / bond quote) that drives automatic fetch routing and determines whether the holding is valued
  by quantity × price or by a directly recorded total value.
- **IncomePolicy**: a new classification per holding (distributing / accumulating / unknown) recording
  whether it is expected to generate periodic income events.
- **Provider/manual valuation record**: the mechanism by which a provider-valued or manually-valued
  holding's total value is recorded directly, independent of unit quantity or per-unit price.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An investor can fully record and view a value history for at least one value-based or
  Inco-style holding that could not be recorded at all before this feature.
- **SC-002**: For every holding, the investor can see its price/value source, as-of date, and market
  status without navigating away from the holding's own view.
- **SC-003**: No holding is ever priced from a source incompatible with its valuation method (e.g., a
  bond or cryptocurrency priced through an equity ticker lookup) after this feature ships.
- **SC-004**: Every price/value snapshot that existed before this feature retains its original date and
  price after migration — zero snapshots are altered or lost.
- **SC-005**: The valuation-method, source, market-status, and staleness information shown for any given
  holding is identical between the React and WPF front ends.

## Assumptions

- The transaction-level ability to record a contribution or withdrawal against a holding with no units
  already exists (`CapitalCall`, `ReturnOfCapital`, delivered in P47); this feature only adds the
  valuation side — recording and displaying what such a holding is currently *worth*.
- Currency captured on a snapshot is provenance only (what currency the recorded figure is denominated
  in); it does not imply or require currency conversion, which is Wave 3 (P49).
- The roughly 90 holdings currently classified `GlobalAssetClass.Unknown` are not required to be
  reclassified by this feature (per the roadmap's D5); they continue to resolve through today's default
  routing behaviour.
- What counts as "stale" for a given market is a configuration detail decided during planning, not a
  scope boundary of this feature.
- `IncomePolicy` is recorded and displayed by this feature; using it to drive data-quality warnings or
  dashboard signals is Wave 6 (P52) and out of scope here.
- Existing snapshots with no recorded source are migrated to a distinct default classification (rather
  than left blank), consistent with this program's established pattern of automatic, reviewable
  migrations verified against a temp copy of the data file before running against the live one.
- Separating a bond's clean price, dirty price, and accrued interest remains out of scope; the
  bond-quote valuation method added here classifies *how* a bond is priced, not that separation.
