# Feature Specification: Transaction and Income Event Vocabulary

**Feature Branch**: `004-transaction-income-vocabulary`

**Created**: 2026-09-12

**Status**: Draft

**Input**: User description: "Wave 1 at docs/investment-performance-roadmap.md" — Wave 1 (P47) of the
Investment Performance & Tax Reporting roadmap: "Transaction and income event vocabulary… the root gap.
Everything downstream depends on it."

## Context

`docs/investment-performance-roadmap.md` §3 (G1) and §6 (Wave 1) is the source of truth for *why* this
feature exists. In short: `Transaction.TransactionType` is `{ Buy, Sell }` and `Credit.Type` is
`{ Dividend, Rent, JCP }` with a single positive `Value`. Because of this:

- A fee, a withheld tax, a return of capital, an in-specie transfer, a bond redemption, a fund
  maturity, and a standalone cash contribution or capital call are all **unrepresentable** — they can
  only be approximated by distorting a Buy or Sell, or not recorded at all.
- Net-of-tax return does not exist: there is no field anywhere holding tax withheld.
- A plain cash contribution to a value-based holding (no market price, no units — e.g. "Inco",
  §4 of the roadmap) cannot be recorded today because `Transaction` rejects `quantity <= 0` and
  `unitPrice <= 0`.

Wave 0 (`specs/003-investment-calculation-core/`) made the *existing* vocabulary's arithmetic correct
and server-owned. This feature widens the vocabulary itself. It is scoped narrowly against the
roadmap's own sequencing:

- **Out of scope, by design**: corporate actions (splits, rights issues, mergers, spin-offs) are
  Wave 7 (P53), deliberately last. Choosing a cost-basis *method* (FIFO / specific-identification) and
  persisting an immutable disposal record are Wave 4 (P50). Multi-currency and FX are Wave 3 (P49).
  Value-based holdings actually *using* the relaxed validation to represent a whole asset with no
  market price are Wave 2 (P48-F03) — this feature only removes the blocker at the transaction level.
  Tax jurisdiction, classification and reporting are Wave 5 (P51).
- **In scope**: the transaction and income *event vocabulary* and a coherent money breakdown (gross /
  fees / tax withheld / net) on both, rebuilding the cash-flow builder over it, and the resulting
  Gross-vs-Net return distinction — in both front ends.

## Clarifications

### Session 2026-09-12

- Q: Given that FII distributions are commonly called "dividends" in Brazil too, should this feature fully merge the "Rent" kind into "Dividend" (one kind, one label), relying on the asset's existing GlobalAssetClass (RealEstate) for any future tax distinction — rather than keeping a separate kind under a new name? → A: Superseded below — checking `data/data-investment.json` showed every "Rent" credit sits on BBAS3/BOVA11/GOLD11/IVVB11 (none RealEstate) and every one of the 35 RealEstate holdings already uses "Dividend", so "Rent" is not an FII-distribution naming leak at all.
- Q: What should the income kind currently mislabeled "Rent" — confirmed against the live data to be share-lending income (aluguel de ações: a fee paid for lending shares/ETF units to short sellers via the broker), not an FII/REIT distribution — be renamed to? → A: SecuritiesLendingIncome. A distinct income kind, unrelated to Dividend and unrelated to real estate; every existing `Rent` record is relabeled SecuritiesLendingIncome with no value change.
- Q: Should a correction to a previously recorded income event be its own distinct "Correction" income kind, or a negative-value entry using the same kind as the original payment? → A: Same kind, negative value — a negative Dividend/SecuritiesLendingIncome/Coupon stays that kind; no separate "Correction" kind is added.
- Q: Can a Transfer In or Transfer Out — declared as having no cash effect — still carry a non-zero fee that actually moves cash, even though the transfer itself doesn't? → A: Yes — a fee is recorded independently of the type's declared quantity/cash effect; Transfer In/Out's "no cash effect" describes the transfer's own principal movement only, not any fee attached to it.
- Q: Should the renamed "Rent" income kind use the UK term ShareLendingIncome or the US term SecuritiesLendingIncome? → A: SecuritiesLendingIncome — chosen because the US is the largest market, and "securities lending" also reads naturally for ETF units, not only shares.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Recording a cash event that isn't a buy or a sell (Priority: P1)

As the investor, when money moves in or out of a holding for a reason that isn't buying or selling
units — a platform fee, a bond being redeemed at maturity, a fund returning capital, units arriving
from another broker, or a private-fund capital call — I can record it as what it actually is, dated and
auditable, instead of distorting a Buy/Sell entry or leaving it out of the record entirely.

**Why this priority**: This is the root gap (G1). Every other capability in this wave, and every later
wave, depends on the vocabulary existing first.

**Independent Test**: Record one of each new transaction type against a real holding; confirm each
appears in the transaction list with its correct type, correct effect on quantity (or none), and
correct effect on cash (in, out, or none); confirm existing Buy/Sell entry is completely unaffected.

**Acceptance Scenarios**:

1. **Given** a holding with units, **When** I record a Fee of £10 against it, **Then** the fee appears
   as its own dated entry, reduces total cash invested exactly like a cost, and leaves the holding's
   quantity and average price unchanged.
2. **Given** a bond nearing maturity, **When** I record a Redemption for its full remaining quantity,
   **Then** the position closes (quantity reaches zero) and the cash received is recorded as its own
   entry, distinguishable from an ordinary Sell in the transaction list.
3. **Given** units transferred in-specie from another broker, **When** I record a Transfer In for that
   quantity, **Then** the holding's quantity increases with no cash effect and no change to average
   price beyond what the transfer's own recorded cost basis implies.
4. **Given** a private-fund capital call, **When** I record it, **Then** cash paid out is recorded
   against the holding with no effect on quantity, because units have not been issued yet.
5. **Given** a fund distribution that is a return of capital rather than income, **When** I record it,
   **Then** cash received is recorded without being counted as income (Credit) and without affecting
   quantity.

---

### User Story 2 - Seeing what was actually paid, withheld, and received (Priority: P2)

As the investor, for both a buy/sell and an income payment, I can see the gross amount, any tax
withheld at source, and the net amount that actually moved — as three distinct, individually visible
figures — instead of a single blended number that hides how much tax already left before I saw the
money.

**Why this priority**: This is what makes net-of-tax return possible (the roadmap's stated deliverable
for this wave) and is the direct predecessor to Wave 5's tax reporting.

**Independent Test**: Record an income payment with tax withheld at source; confirm gross, withheld,
and net all display distinctly and gross − withheld = net; confirm every one of the 1,485 existing
income records still displays correctly with withheld reported as none.

**Acceptance Scenarios**:

1. **Given** a dividend of 100 with 15 withheld at source, **When** I view it, **Then** I see Gross 100,
   Withheld 15, Net 85 as three separate figures.
2. **Given** an existing income record from before this feature (no withholding data was ever
   captured for it), **When** I view it, **Then** it displays as it always has — net amount only
   reported, and withheld reported as none, not as zero-and-indistinguishable-from-a-verified-zero.
3. **Given** a sell with a fee taken from the proceeds and no tax withheld, **When** I view it,
   **Then** gross proceeds, the fee, and net cash received are all individually visible.

---

### User Story 3 - Seeing return with tax already taken into account (Priority: P3)

As the investor, alongside the return figure I already see, I can see a second figure that accounts for
tax actually withheld — so a holding taxed heavily at source and one that isn't don't look identical
just because their untaxed returns match.

**Why this priority**: This is the payoff of User Story 2's data existing at all, and the feature's own
stated deliverable ("net-of-tax return exists"). It depends on Stories 1 and 2 landing first.

**Independent Test**: Take a holding with at least one instance of tax withheld; confirm its net-of-tax
return differs from its existing (gross) return by an amount attributable exactly to the withheld tax,
and that a holding with zero withholding shows the same figure for both.

**Acceptance Scenarios**:

1. **Given** a holding with tax withheld on at least one income payment, **When** I view its return,
   **Then** I see both the existing return figure and a net-of-tax figure, and they differ.
2. **Given** a holding with no tax ever withheld, **When** I view its return, **Then** both figures are
   present and equal.
3. **Given** a portfolio or broker total, **When** I view its return, **Then** both figures are shown
   at that level too, following the same withheld-return rules Wave 0 established for an incomplete
   total (a total missing a valuation withholds *both* figures, not just the new one).

---

### User Story 4 - Recording and viewing the new event types from either front end (Priority: P4)

As the investor, I can record and view every new transaction and income event type, and the
gross/withheld/net breakdown, from both the web application and the desktop application, with the same
terminology, field order, and outcomes in both.

**Why this priority**: Matches this repository's standing UI-parity invariant. Ordered after Stories
1–3 because the front-end work has nothing to render until the underlying vocabulary and figures exist.

**Independent Test**: Record one new transaction type and one income event with withholding from the
web application; confirm the desktop application shows identical figures, terminology and field order
for the same holding, and vice versa.

**Acceptance Scenarios**:

1. **Given** the web application, **When** I record a Fee or a Redemption, **Then** the desktop
   application shows the same transaction, type, and amounts for that holding without needing a
   restart of *its own* process once its own next data reload happens (matching this codebase's
   existing "restart required after a direct file edit, not after another process's normal write"
   convention).
2. **Given** either front end's transaction entry form, **When** I select a non-buy/sell type that has
   no unit effect, **Then** the quantity field is not required, matching Wave 1's relaxed validation.
3. **Given** either front end, **When** I view an income event with withholding, **Then** gross,
   withheld and net are labelled identically and formatted identically to the other front end.

---

### Edge Cases

- What happens to the **899 existing transactions and 1,485 existing income records** when this
  ships? They must all still load and display exactly as before: every existing transaction stays
  `Buy`/`Sell` with gross = net and zero fees beyond what was already recorded, and every existing
  income record stays its current kind with gross = net and withheld = none. Nothing already on screen
  may move as a result of this migration alone (contrast with Wave 0's Bitcoin/AGNC case, where the
  *replay rule itself* changed a stored figure — this feature changes *representable vocabulary*, not
  arithmetic over the existing one).
- What happens if a **Redemption or Transfer Out is recorded for more units than are held**? It must be
  refused the same way Wave 0's oversell refusal works for a Sell — the new types that reduce quantity
  are subject to the same coverage rule, not a separate, weaker one.
- What happens when a **Transfer In / Transfer Out pair should be cost-basis-neutral** (moving the same
  holding between two of the user's own brokers)? This feature does not attempt to link the two sides
  automatically — each is its own independently recorded event, consistent with this codebase's
  existing `MoveAsset`/`ArchiveAsset` being the mechanism for moving a *holding*, not a transaction-level
  transfer. A future wave may revisit this; recorded here as a known limitation, not a defect.
- What happens to a **CapitalCall that is later followed by units actually being issued**? This feature
  does not model that linkage (that is closer to Wave 4/disposal-record territory); the capital call is
  simply its own recorded cash event, and the eventual unit issuance is recorded separately (as a Buy,
  or as a Transfer In if the units arrive without a further cash movement).
- What happens if **gross, fees, and tax withheld are entered such that net would be negative**? The
  system must refuse it and name which figure doesn't add up, the same way today's validation refuses
  an internally inconsistent transaction.
- What happens to the **`Credit.Type.Rent` naming leak**? Roadmap G12 describes it as an FII/REIT
  distribution naming leak, but checking the live data overturns that: every `Rent` credit sits on
  BBAS3, BOVA11, GOLD11 or IVVB11 — none of them RealEstate-class — while every one of the 35
  RealEstate (FII) holdings already records its distributions as `Dividend`. `Rent` is actually
  **share-lending income** (a fee for lending shares/ETF units to short sellers via the broker). This
  feature renames it to `SecuritiesLendingIncome`, a kind distinct from `Dividend` and unrelated to real
  estate; every existing `Rent` record must display and total as `SecuritiesLendingIncome` with no
  previously recorded value changed.

## Requirements *(mandatory)*

### Functional Requirements

**New transaction types**

- **FR-001**: The system MUST support recording a transaction of type Fee: a cash outflow against a
  holding with no effect on quantity or average price.
- **FR-002**: The system MUST support recording a transaction of type Redemption: a cash inflow that
  reduces quantity (fully or partially), distinguishable in every view from an ordinary Sell.
- **FR-003**: The system MUST support recording a transaction of type Transfer In: a quantity increase
  with no cash effect of its own (a non-zero fee MAY still be recorded against it per FR-009; the fee
  is an independent cash outflow, not a change to the type's declared cash effect).
- **FR-004**: The system MUST support recording a transaction of type Transfer Out: a quantity decrease
  with no cash effect of its own (subject to the same fee independence as FR-003), subject to the same
  held-quantity coverage rule Wave 0 established for Sell (FR-009 of
  `specs/003-investment-calculation-core/spec.md`) — Transfer Out and Redemption MUST NOT be permitted
  to leave a later transaction short of the units it needs, the same way an oversell is refused today.
- **FR-005**: The system MUST support recording a transaction of type Capital Call: a cash outflow
  against a holding with no effect on quantity.
- **FR-006**: The system MUST support recording a transaction of type Return of Capital: a cash inflow
  against a holding with no effect on quantity and MUST NOT be counted as income.
- **FR-007**: Every transaction type introduced by this feature MUST declare its own quantity effect
  (increase / decrease / none) and cash effect (in / out / none) as data, not as scattered per-type
  code branches, so a future type can be added by declaring its effects rather than editing every
  consumer. A type's declared cash effect governs its own principal movement only; a non-zero fee
  (FR-009) always produces its own cash outflow regardless of that declaration.
- **FR-008**: Recording a transaction whose type has no quantity effect MUST NOT require a quantity or
  unit price to be entered.

**Money breakdown**

- **FR-009**: Every transaction MUST record a gross amount, fees, and tax withheld, and MUST derive
  net cash from them; the three MUST be independently visible wherever a transaction's amount is shown
  today.
- **FR-010**: Every income event MUST record a gross amount and tax withheld, and MUST derive a net
  amount from them; all three MUST be independently visible wherever an income amount is shown today.
- **FR-011**: The system MUST refuse to record a transaction or income event whose gross, fees, and
  withheld amounts would produce a negative net, naming which figure is inconsistent.
- **FR-012**: Every one of the transactions and income records stored before this feature ships MUST
  continue to display with gross equal to its existing recorded amount, fees and withheld reported as
  none, and net equal to what is displayed today. No previously displayed figure may change as a
  direct result of this feature's migration.

**Income event vocabulary**

- **FR-013**: The system MUST rename the income kind currently labelled "Rent" to "SecuritiesLendingIncome"
  (a fee for lending shares/ETF units to short sellers via the broker — confirmed against the live
  data, not an FII/REIT distribution as the roadmap's G12 assumed), without changing any previously
  recorded value, and MUST update every place that name is displayed in both front ends.
- **FR-014**: The system MUST support recording a bond coupon as its own income kind, distinct from a
  dividend.
- **FR-015**: The system MUST support a negative-value income record (a correction to a previously
  recorded income event), auditable as its own dated entry rather than a silent edit to the original.
  A correction MUST use the same income kind as the payment it corrects (e.g. a negative `Dividend`
  corrects a `Dividend`) — this feature MUST NOT introduce a separate "Correction" kind.

**Cash flow and return**

- **FR-016**: The system MUST rebuild its cash-flow construction to draw dated amounts from the full
  transaction and income vocabulary this feature introduces, not only Buy/Sell/Credit as today.
- **FR-017**: The system MUST compute a gross return series and a net-of-tax return series as two
  distinct results, both available wherever a return figure is shown today (asset, portfolio, and
  broker level, per Wave 0's existing levels).
- **FR-018**: Where a holding, portfolio, or broker has never had tax withheld, the gross and net-of-tax
  return figures MUST be numerically identical, not merely close.
- **FR-019**: The rate-of-return calculation method itself (the solver) MUST NOT change; only the
  dated-amount series it is given changes.
- **FR-020**: Both front ends MUST distinguish gross return from net-of-tax return with a label, never
  relying on position or column order alone to convey which is which.

**Validation and parity**

- **FR-021**: Both front ends MUST offer every transaction type and income kind this feature
  introduces, with identical terminology, field order, and validation wording between them.
- **FR-022**: A transaction entry form, in either front end, MUST NOT display a quantity or unit-price
  field as required when the selected type has no quantity effect.

### Key Entities

- **Transaction**: widened with an explicit type-to-effect declaration (quantity effect, cash effect)
  and a gross/fees/withheld/net money block, replacing the current implicit "buy adds, sell subtracts,
  price × quantity is the whole story" shape. Existing `Buy`/`Sell` behavior is preserved exactly.
- **Income event** (today's `Credit`): widened with gross/withheld/net and an income-kind vocabulary
  where the mislabeled "Rent" kind is renamed `SecuritiesLendingIncome` (confirmed against live data to be
  share-lending fees, not an FII/REIT distribution) and a new bond-coupon kind is added; supports a
  negative-value entry as a correction.
- **Transaction type effect declaration**: the data (not code) that says, for a given type, whether
  quantity increases, decreases, or is unaffected, and whether cash moves in, out, or not at all. This
  is what a later type (e.g. a Wave 7 corporate action) will also need to declare.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can record a fee, a redemption, a transfer, a capital call, or a return of capital
  against an existing holding without approximating it as a buy or a sell.
- **SC-002**: For every transaction and income event recorded going forward, gross, fees/withheld, and
  net are all visible without navigating away from the record's own row or detail view.
- **SC-003**: All 899 existing transactions and 1,485 existing income records display identically to
  today immediately after migration — zero displayed figures change.
- **SC-004**: A holding with tax withheld shows a net-of-tax return figure that is measurably different
  from its gross return figure; a holding with none shows the two as equal.
- **SC-005**: Every figure this feature defines is identical between the web and desktop applications,
  to the last digit displayed, in the same format — matching Wave 0's own parity bar.
- **SC-006**: Recording a transaction type with no quantity effect takes no more form fields to complete
  than the type actually needs — no user is asked for a quantity or price that means nothing for what
  they are recording.

## Assumptions

- **Corporate actions, disposal-method selection, multi-currency, valuation methods, and tax
  jurisdiction/classification are explicitly out of scope**, per the roadmap's own wave sequencing
  (P53, P50, P49, P48, P51 respectively). This feature only removes the *vocabulary* blocker each of
  those later waves depends on.
- **The exact list of new transaction types is drawn from the roadmap's own gap analysis (G1)**, not
  from an attempt to reproduce every one of "the seventeen" types the underlying research brief names —
  that document is not present in this repository, so the eight types this spec defines (existing
  Buy/Sell plus Fee, Redemption, Transfer In, Transfer Out, Capital Call, Return of Capital) are treated
  as the right-sized set for a single-user tool, per Constitution Principle IV. Nothing here prevents a
  further type being added later the same way.
- **Migrating existing data is additive only**: every existing `Transaction`/`Credit` row is
  reinterpreted with gross = net, fees = 0, withheld = none — never re-derived, guessed, or backfilled
  from any other source. This mirrors Wave 0's own established convention (`docs/rules/*`, and
  `specs/003-investment-calculation-core/spec.md`'s FR-058 precedent) that nothing in this codebase
  infers a fact it cannot verify.
- **`Transaction.TotalPrice` (fees folded into average price) is untouched by this feature for Buy/Sell
  specifically** — the roadmap (G3) flags this as a tax-basis limitation for a future wave, not
  something this feature's gross/fees/withheld/net block is meant to retroactively fix for the two
  existing types.
- **Existing `Credit` rows storing the legacy `"Rent"` type are rewritten on load**, via a document
  version the JSON serializer upgrades on the fly (no separate migration tool or manual run), with
  the existing `Value` field preserved as the derived net amount for backward compatibility — verified
  against a temp copy first, exactly as Wave 0's own migrations verify against
  `data/data-investment.json` never directly (research.md #6).
