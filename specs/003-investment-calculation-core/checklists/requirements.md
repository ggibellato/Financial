# Specification Quality Checklist: Investment Calculation Core

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-09-10
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

**Source**: `docs/investment-performance-roadmap.md` §6, Wave 0 (P46). The roadmap covers eight waves
(P46–P53, ~43 PRs); this specification covers Wave 0 only, on the user's instruction. Waves 1–7 are
named in the spec's Out of Scope section so nothing here reads as an omission.

**Iteration 1 (2026-09-10)** — nine decisions had no safe default and were put to the user before
drafting. All nine were answered, so the spec was written with zero `[NEEDS CLARIFICATION]` markers
rather than carrying them into review.

| Decision | Answer | Encoded in |
|----------|--------|------------|
| Scope of this spec | Wave 0 / P46 only | Out of Scope, all seven stories |
| Where valuation gets its price | Recorded price history, carried forward | FR-028, FR-030, Assumptions |
| Enforcing the impossible-sale rule | Refuse user entry; load unjudged | FR-009, FR-013, FR-014, FR-015 |
| Meaning of "amount invested" | Cost of open units | FR-019 |
| A fetched price and the stored history | Fetch records the price | FR-035, Assumptions |
| An incomplete portfolio total | Publish, state the shortfall; withhold the rate | FR-040, FR-042 |
| Scope of the invested change | Active = open cost, Historic = total bought | FR-019, FR-020 |
| Staleness threshold | Older than one trading day | FR-032 |
| Tolerance for a rounding oversell | None; state the held quantity | FR-011, FR-012 |

**Data verification (2026-09-10)** — the roadmap's claims were checked against the 160 stored
holdings before drafting, and two of the decisions above were revised as a direct result:

| Checked | Result | Effect on the spec |
|---|---|---|
| Holdings stored out of date order | 1 of 160; average price unaffected | FR-006 and SC-002 assert the change moves no displayed figure — the story is argued as prevention, not as a visible fix |
| Holdings already selling more than held | 3, all Historic | Confirms the load-unjudged decision: a rule applied at load would stop the application starting (FR-013) |
| …of which fund-redemption rounding | 2, by ~0.004 units | Produced the no-tolerance question and FR-012's full-precision requirement |
| Open positions with no recorded price | 7 of 28 | Forced "unavailable is not nought" (FR-030) and the partial-total rules (FR-040, FR-047) |
| Prices recorded today | 0; newest 3 days, oldest 7 | Made carry-forward mandatory (FR-028) rather than a refinement, and set the staleness question |
| Classification gaps | 90 of 160 without an asset class, 93 without a local type code | Roadmap said 87 of 132 historic; it is 90 of 160 overall — 87 historic plus 3 active. FR-056 and SC-012 use the corrected figures |
| Currencies within any one broker | 1 | FR-045 forbids a total spanning brokers rather than deferring the question |

Two roadmap claims were found to be wrong and are **not** carried into the spec:

1. The roadmap describes the desktop switchover as adopting the browser's networked interface. The
   desktop application does not obtain these figures over the network. Recorded in Assumptions; it is
   why FR-026 and FR-036 name a single shared calculation rather than a service boundary.
2. The roadmap locates the duplicated valuation arithmetic in two places. There are six. SC-007
   measures the reduction from six to zero.

**Two conflicts are recorded rather than resolved**, because no requirement here depends on either:

- The repository's UI rules name the web application as the source of truth for user experience; the
  constitution at version 1.1.0 names the desktop application. Every requirement is written as the two
  front ends agreeing, so the spec is correct under either reading.
- Switching both front ends in one increment deviates from the documented slice order. It is
  deliberate: switching one alone would show two different values for the same holding, which is a
  parity regression rather than an unfinished increment.

**Structural check** — FR-001 through FR-064 and SC-001 through SC-013 are contiguous, with no gaps
and no duplicates, and each is defined exactly once. Seven user stories, each with a dependency-based
priority argument, an independent test, and acceptance scenarios. Nineteen edge cases.

**Known slicing consequence for `/speckit-plan`** — the roadmap budgets seven pull requests for this
wave. Seven independently deployable increments is not achievable at the repository's eight-file
limit: User Story 4 needs four and Stories 3, 5 and 6 need two each, so expect roughly thirteen. Two
sequencing constraints belong in the plan rather than the spec:

- The figure that currently serves as the invested amount also serves as the basis for a holding's
  share and as the denominator of the income yield percentages (FR-025). These must be separated
  before User Story 6 changes the share basis, or the yield percentages move with it.
- User Story 6's change is the one place where a backend-first split puts a false number on screen —
  an unvalued holding would read as 0.0% until the front ends can render an unknown share. Its two
  halves should land together.

**Clarification session (2026-09-10)** — `/speckit-clarify` asked 5 questions and all were answered.
All 16 checklist items were passing before the session and remain so; no item changed state. The
session's value was closing gaps that the spec itself flagged, not fixing failures:

| Gap | Resolution | Encoded in |
|---|---|---|
| FR-002 required a same-date order and never stated one | Purchases before sales on a date | FR-002, US1 scenario 5, edge case |
| FR-015 did not say how much of the resulting history must be valid | The whole history; refusal names the later transaction left short | FR-065, FR-066, US2 scenario 9 |
| FR-026 named two return measures defined nowhere | Price-only and total return, at holding, portfolio and broker | FR-067, FR-068, FR-042, US5 scenario 2 |
| "One trading day" had no calendar | Most recent weekday; no per-market holiday calendar | FR-032, FR-069, Assumptions |
| FR-024 required the chart's inclusion rule to be stated and never stated it | Holdings with an invested amount above nought | FR-070, US3 scenario 8 |

Two of these were self-referential defects — FR-002 and FR-024 each demanded that a rule "MUST be
stated" and then did not state it, which would have passed every automated check and surfaced as
disagreement during implementation.

**Follow-up clarification (2026-09-10)** — a sixth question from the user, about holdings missing a
country or type, produced the session's largest change and one factual correction.

The correction: **country is not a gap**. All 160 holdings carry one (97 UK, 62 BR, 1 US), and
cross-tabulating the two halves of the classification key shows **no holding fails to resolve because
a mapping row is missing** — every holding carrying a local type code resolves to a real class. The
apparent 90-versus-93 discrepancy is not two problems: it is 90 unclassified holdings plus 3 whose
class was set directly on the asset (Bitcoin, BOVA11, IVVB11).

The change: **there is no mechanical way to classify the 90** — the user confirmed it is a manual job,
one instrument at a time, and asked that classification not be enforced, particularly in Historic
Investments where 87 of the 90 sit. User Story 7 was rewritten from "report and backfill" to report
only, and a new requirement forbids any figure in the feature from depending on classification at all.

| Changed | From | To |
|---|---|---|
| User Story 7 | Report + automatic backfill, verified on a copy | Report only; writes nothing, changes nothing |
| FR-056 | Counts of missing class and missing local type code | Names unclassified holdings, split 3 open / 87 closed |
| FR-057 | Fill in class or code where mechanically determinable | Directly-set classes are valid and not reported as gaps |
| FR-058, FR-059 | Backfill verification and restart notice | Never infer or write a classification; report mutates nothing |
| FR-071 (new) | — | No figure may require a classification |
| FR-072 (new) | — | Restart notice after a manual edit to the file |
| SC-012, SC-014 (new) | Counts fall after backfill | Report changes nothing; all 160 holdings produce every figure |

This **diverges from the roadmap deliberately**: its Wave 0 lists a "data-quality backfill tool +
report", and its decision D5 requires the unclassified holdings to be filled in before any
class-driven rule ships. Neither is achievable. The consequence is recorded in Assumptions and reaches
later waves — P48's valuation methods and P51's tax profiles are both planned to be driven by asset
class, and each must now define its behaviour for an unclassified holding rather than assuming a
backfill has happened.

**Country semantics recorded (2026-09-10)** — investigating the classification gap established that
country is not a gap at all (all 160 holdings carry one) but that it means something other than it
appears to. It tracks the broker's jurisdiction, not the issuer's domicile: 25 recognisably
US-domiciled holdings are recorded as UK because they sit in UK accounts, and exactly one holding in
the file carries US. The convention has also drifted — AGNC Investment Corp. appears three times under
one broker, recorded as US once and UK twice.

No requirement was added. Nothing in this feature reads country, so it changes no figure here, and
adding it to the data-quality report would have widened that report beyond "what the application
cannot calculate". It is recorded in Assumptions because P51 plans to derive a tax jurisdiction from
this field, and withholding on a US-domiciled dividend is a US matter whichever account holds it —
so whether the field needs splitting into custody and domicile is a decision that belongs to that
wave, made knowingly rather than discovered.

**Ambiguity re-scan (2026-09-10)** — the spec changed materially *after* the clarify session (User
Story 7 rewritten, eight requirements added, two criteria rewritten, three assumptions added), so it
was re-scanned rather than assumed still sound. Three defects were found, all introduced by the Story
7 rewrite, all verified against code before being fixed:

| Defect | Evidence | Fix |
|---|---|---|
| FR-072 assumed classification is done by hand-editing the stored file, and required a restart | `AssetAdminService` and the asset form in both front ends already edit country, local type code and class; creating a holding without a class derives it from the other two | FR-072 rewritten: classification happens in the app and takes effect with no restart; only a direct file edit needs one |
| FR-071 claimed no figure depends on classification — true for calculation, but price *fetching* is routed by class | `StandardAssetPriceFetcher` treats `Unknown` as exchange-listed, so an unclassified bond or cryptocurrency is routed to the wrong source and will fail to price | FR-071 qualified ("given a price"); FR-073 added, requiring the report to link the two problems |
| SC-014 claimed every figure is produced for all 160 holdings, contradicting FR-030 | 7 holdings have no recorded price and are explicitly unvaluable | SC-014 restated: classification is never the reason a figure is unavailable |

The second is the one worth remembering: the spec had been treating "unclassified" and "unpriced" as
independent data-quality problems, and for bonds and cryptocurrencies one causes the other. No current
holding is affected — the three unclassified open positions are two exchange-traded funds and a share,
all routed correctly — but the rule holds for anything added later.

Also checked and clean: no vague unquantified adjectives; no `all 160` overclaim (the four occurrences
all concern loading and listing, which FR-013 does guarantee for every holding); User Story 7's
scenarios renumbered contiguously after the additions.

**Full fact-check against code and data (2026-09-10)** — every factual assertion in the spec was
checked against the codebase and against `data/data-investment.json`, rather than only the material
that had recently changed. **Twenty claims were false.** All are now corrected. The structural checks
had passed throughout, which is the lesson: none of these was detectable without reading the code and
re-parsing the data.

The one that mattered most: **FR-006, SC-002 and User Story 1 scenario 6 all claimed the replay change
alters no displayed figure. It alters two.** Re-ordering by date alone changes nothing, but FR-002's
purchases-before-sales rule — added during the clarify session — moves a same-date sale after a
same-date purchase in two holdings, changing Bitcoin's average price (62,713.15 → 62,709.05) and
realised gain (0.25 → 0.17), and AGNC's realised gain (9.98 → 9.66). The user was told this rule cost
nothing on screen; told the true cost, they confirmed keeping the rule and accepting the change as a
correction. The three requirements now state the change as bounded and enumerable rather than absent.

Corrections by category:

| Category | Claim | Truth |
|---|---|---|
| Replay | Zero displayed figures change | Two holdings change (FR-006, SC-002, US1-6) |
| Historic scope | "Every historic position is closed by definition", all 132 flat | Four still hold units, one 28 units at 2,000+ cost. FR-020 now keys off scope, not quantity; FR-074 added to report them |
| Price freshness | No holding priced today; most recent three days old; "every open position 3–7 days old" | Five were priced the current day. These figures drift by the hour — the file gained five prices mid-session — so they are no longer load-bearing anywhere |
| Portfolio counts | One portfolio unvaluable; seven unpriced "across three portfolios" | Three portfolios have no valued holding; the seven span five portfolios (FR-040, SC-009, edge cases) |
| Empty portfolios | "One stored portfolio is in the second state" | Three are, and **no** portfolio is empty — the first arm of that edge case has no example |
| Duplication | Four sites in web, two in desktop, "six" total | Five market-value sites in web + two in desktop, plus three cost-basis sites: ten (FR-036, SC-007) |
| Formatting | Portfolio share formatted three ways | Four render sites, two distinct formats; the front ends agree with each other surface-for-surface (FR-051, FR-060) |
| Reconciliation | Totals disagree with rows "in either scope" | Historic: all 15 disagree. Active: all 10 reconcile today, because no active holding has quantity nought — the code diverges, the data does not yet expose it (SC-005) |
| Invested derivations | Three parts of the application | Five, and each front end already prints two different totals on one screen (FR-022) |
| Manual price precedence | "The manual one wins, as it does today" | A holding stores at most one price per date, so the state cannot exist and there is no read-time rule. Only today's manual entry is protected at write time; on earlier dates last write wins (FR-033, edge case, US4-5) |
| Future-dated price | "Cannot arise" | Cannot be *entered*, but loading bypasses validation, so a hand-edited file can contain one |
| Class on edit | Leaving the class unset derives it | True on create only. Editing keeps whatever the form submits, so correcting the two fields the report complains about leaves the holding unclassified (FR-072) |
| Derived figures | Not stored | Four are excluded, but whether a position is long/flat/short is written to the file on every holding despite being derived (FR-005) |
| Divide-by-zero | "The holding cannot be opened" | Positions rebuild while the file is read and nothing catches it, so the **application would fail to start** (FR-016, edge case) |
| Country | Twenty-five US-domiciled holdings recorded as UK | Fifty to sixty; two carry a US identifier and US exchange while recorded as UK |
| SC-014 | Every figure produced for all 160 holdings | The 132 historic have no market value by design (FR-034) |

A methodological note for later waves: several of these were volatile rather than wrong-at-writing —
price ages and counts changed while the spec was being written, because the application is running and
fetching. Point-in-time counts should be cited as illustration, never as the thing a requirement turns
on. The spec has been reworded on that basis.

Requirements now run FR-001 through FR-074 and SC-001 through SC-014, with no gaps, no duplicates and
no dangling cross-references; the added ids sit inside the group they belong to rather than at the
end, following the precedent set by FR-043 in
`specs/002-move-assets-between-portfolios/spec.md`. All 16 checklist items still pass.

Spec is ready for `/speckit-plan`.
