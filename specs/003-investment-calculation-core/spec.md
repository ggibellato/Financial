# Feature Specification: Investment Calculation Core

**Feature Branch**: `003-investment-calculation-core`

**Created**: 2026-09-10

**Status**: Draft

**Input**: User description: "docs/investment-performance-roadmap.md — Wave 0 (P46 · Investment
calculation core): make what exists correct and server-owned before adding anything. Date-ordered
position replay; a single owner for market value, cost basis, unrealised gain and return; reject
oversell, correct the invested amount, market-based portfolio weight; server-computed valuation
consumed by both front ends; portfolio- and broker-level return; a data-quality report and backfill."

## Clarifications

### Session 2026-09-10

- Q: When a purchase and a sale are recorded on the same date, which order should the position be
  worked out in? → A: Purchases first — all purchases on a date are applied before any sales on that
  date.
- Q: When a change is checked against the impossible-sale rule, how much of the resulting history must
  be valid? → A: The whole resulting history — the change is refused if any sale, at any date, would
  sell more than was held at that date.
- Q: Which return measures should exist, and at which levels? → A: Both — a price-only return that
  excludes income and a total return that includes it — reported for a holding, a portfolio and a
  broker alike.
- Q: How should "older than one trading day" be measured, given UK and Brazilian holdings? → A:
  Calendar days skipping weekends — current if dated today or the most recent preceding weekday; no
  per-market holiday calendar.
- Q: Which holdings should the allocation chart include? → A: Only those with an invested amount above
  nought, which is today's behaviour stated explicitly rather than changed.
- Q: What should the income yield percentages be measured against, given FR-049 froze a denominator
  that is negative for one holding? → A: The corrected invested amount — still measured on cost, never
  on market value. Four holdings' percentages change as a result, one flipping sign; FR-049 and SC-011
  were amended accordingly.
- Q: How should the 90 unclassified holdings be classified? → A: By hand, one instrument at a time.
  There is no mechanical conversion, so the feature reports the gap and never writes a classification;
  holdings already classified directly are left as they are, and nothing may require a classification
  — least of all in Historic Investments.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Position figures that don't depend on the order I typed (Priority: P1)

The investor remembers a purchase they never recorded, dated before a sale they entered months ago.
They add it. Today the average price and realised gain they end up with differ from what they would
have had if they had entered the same three transactions in the order the transactions actually
happened — silently, with nothing on screen to say which of the two figures is real. After this
story a position is always worked out in date order, so the figures are the same whichever order
they were typed in, and correcting a transaction's date re-derives them.

**Why this priority**: every other story in this specification reads the average price or the cost
of the units still held. Built on top of a figure that changes with typing order, a market value
would be correct arithmetic over a wrong basis, and every number verified in a later story would
have to be verified again once this landed. It is also the only defect here that corrupts meaning
silently — the other two announce themselves, as a negative quantity and as an invested amount of
nought.

**Independent Test**: fully testable by taking a set of transactions containing at least one
back-dated purchase entered after a sale, recording it chronologically against one holding and
shuffled against another, and confirming both report identical quantity, average price, realised
gain and average sell price. Then by editing a transaction's date backwards and confirming the
figures move to what chronological entry would have produced.

**Acceptance Scenarios**:

1. **Given** a holding with a purchase of 10 units at 100 dated March and a sale of 5 units at 110
   dated June, **When** the user records a purchase of 15 units at 100 dated April, **Then** the
   average price, quantity, realised gain and average sell price are the same as if all three had
   been entered in date order.
2. **Given** two holdings receiving the same four transactions in different entry orders, **When**
   both are read, **Then** every derived figure is identical between them.
3. **Given** a holding whose transactions were entered out of order, **When** the application is
   restarted and the holding is reopened, **Then** the figures are unchanged from before the
   restart.
4. **Given** a transaction recorded with the wrong date, **When** the user corrects the date to an
   earlier one, **Then** the position is re-derived and reports what chronological entry of the
   corrected set would have produced.
5. **Given** a holding with a purchase of 100 units and a sale of 100 units recorded on the same
   date, **When** the position is derived, **Then** the purchase is applied first, the position ends
   flat, the sale is not treated as selling units not held, and the result is the same on every
   reading and survives a restart.
6. **Given** the 160 holdings already stored, **When** this change is applied, **Then** exactly two
   of them report different figures — one holding's average price and realised gain, and another's
   realised gain — and every other holding is unchanged. Each of the two stores a sale ahead of a
   purchase on the same date, so applying FR-002 moves the sale after the purchase and re-derives
   the figures.

---

### User Story 2 - The application refuses a sale of units I don't hold (Priority: P2)

The investor types 1,000 instead of 100 in a sale. Today the position drops to minus 900 units, is
labelled a short position, and quietly poisons the realised gain, the cost basis and every
percentage computed from them. After this story the entry is refused, the refusal says how many
units were actually held on that date, and nothing is stored. Holdings recorded before this rule
existed still load, still open, and can still be edited and corrected.

**Why this priority**: it protects the same cost-basis chain User Story 1 has just made
deterministic, and it is the only story that removes an outright failure — after a position has
been driven negative, a later purchase that brings the quantity to exactly nought makes the holding
impossible to open at all. It must follow User Story 1 because until positions are replayed in date
order, "how many units did I hold on that date" is not a well-defined question.

**Independent Test**: fully testable by attempting a sale larger than the units held from each
front end and confirming it is refused with the held quantity named and nothing persisted; and by
confirming the application starts, lists all 160 stored holdings, and opens the three whose stored
history already contains a sale larger than the units held. All three sit in Historic Investments, so
the load-tolerance half of this story has to be exercised there rather than in the active view.

**Acceptance Scenarios**:

1. **Given** a holding of 100 units, **When** the user records a sale of 1,000 units, **Then** the
   sale is refused, the message states that 100 units are held, and nothing is stored.
2. **Given** a holding of 100 units, **When** the user records a sale of exactly 100 units,
   **Then** it is accepted and the position becomes flat.
3. **Given** a holding of 2,128.37599271 units, **When** the user records a sale of 2,128.38 units,
   **Then** it is refused and the message states the held quantity to its full precision, so the
   user can enter that figure instead.
4. **Given** a holding whose purchase funded a later sale, **When** the user deletes that purchase,
   **Then** the deletion is refused because it would leave more units sold than held, and both
   transactions remain.
5. **Given** an existing sale of 50 units against a holding of 100, **When** the user edits it to
   500 units, **Then** the edit is refused and the original sale is unchanged.
6. **Given** the three stored holdings whose history already contains a sale larger than the units
   held, **When** the application starts, **Then** it starts normally, lists all 160 holdings, and
   each of the three can be opened.
7. **Given** one of those three holdings, **When** the user edits or deletes one of its
   transactions to correct it, **Then** the correction is allowed provided the resulting history
   does not itself sell more than is held.
8. **Given** a holding whose stored history has driven its quantity negative, **When** a purchase
   brings the quantity to exactly nought, **Then** the holding still opens and reports its figures
   rather than failing.
9. **Given** a holding that bought 100 units in January and sold 80 in June, **When** the user
   records a back-dated sale of 50 units in March, **Then** the sale is refused — although 50 units
   were held in March, the June sale would be left 30 units short — and the refusal names the June
   sale and the shortfall.

---

### User Story 3 - One meaning for "the amount I have invested" (Priority: P3)

The investor bought 100 units at 10 and sold 50 at 20. The application tells them they have nothing
invested, while 50 units sit there at 500 of cost. Meanwhile the allocation chart, the grid column
and the total above it each work the figure out differently, so the parts do not add up to the
whole. After this story there is one definition — for a position still open, the cost of the units
still held; for a closed one, the money that went in over its life — and the same number appears in
the row, in the total, and in the allocation chart.

**Why this priority**: it is the figure that unrealised gain is measured against. Publishing a
market value against the old definition in User Story 4 and then changing the definition afterwards
would mean publishing a gain figure twice, differently, one increment apart. It follows User Story 1
because the cost of units still held is derived from the quantity and average price that the
date-ordered replay produces.

**Independent Test**: fully testable by partially selling a position and confirming the invested
figure equals the cost of the units remaining; then by confirming a portfolio's invested total
equals the sum of its rows, and that the allocation chart uses the same figures, in both Active and
Historic Investments.

**Acceptance Scenarios**:

1. **Given** a holding that bought 100 units at 10 and sold 50 at 20, **When** the summary is read,
   **Then** the amount invested is 500 — the cost of the 50 units still held — not nought.
2. **Given** an open holding that has never been sold, **When** the summary is read, **Then** the
   amount invested is unchanged from before this story.
3. **Given** a holding in Historic Investments, **When** the summary is read, **Then** the amount
   invested is the total of its purchases and is unchanged from before this story.
4. **Given** a portfolio in either scope, **When** its total and its rows are read, **Then** the
   total equals the sum of the rows.
5. **Given** a portfolio containing a holding that has been sold down to nought but is still filed
   under Active Investments, **When** the total and the rows are read, **Then** both treat that
   holding the same way and still reconcile.
6. **Given** an allocation chart for a broker, **When** it is read, **Then** it is built from the
   same invested figures the rows report.
7. **Given** any holding, **When** its income yield percentages are read, **Then** they are
   numerically identical to before this story.
8. **Given** an Active portfolio containing a holding that has been sold out entirely, **When** the
   allocation chart is read, **Then** that holding is omitted because it has nothing invested, while
   every holding in a Historic portfolio is included because its invested amount is its purchases.

---

### User Story 4 - What a holding is worth, worked out once by the application (Priority: P4)

The investor opens a holding, and then the portfolio it sits in. Market value, the cost of what is
held, unrealised gain and return all come from the application itself, valued at the most recent
price it has recorded on or before today, and stamped with the date that price is from. The web
application and the desktop application show the same numbers, because both are asking the same
question of the same calculation rather than each doing the arithmetic themselves. Where no price
has ever been recorded, the application says the value is unavailable instead of showing nought.

**Why this priority**: this is the headline of the feature, and it depends on User Story 1 for the
basis it compares against and User Story 3 for the invested figure it subtracts. Built before them,
both front ends would have been switched onto a calculation that was about to change twice, and the
switchover would simply have to be done again.

**Independent Test**: fully testable by taking a holding with a recorded price and confirming the
web application and the desktop application report identical market value, cost of units held,
unrealised gain and return, each naming the date the price came from; and by taking one of the
seven holdings with no recorded price and confirming both report the value as unavailable rather
than as nought.

**Acceptance Scenarios**:

1. **Given** a holding with a price recorded three days ago and none since, **When** its value is
   read, **Then** it is valued at that price and the reported as-of date is three days ago.
2. **Given** a holding with prices recorded on several dates, **When** its value is read as at
   today, **Then** the most recent price on or before today is used.
3. **Given** a holding with no recorded price at all, **When** its value is read, **Then** market
   value, unrealised gain and return are all reported as unavailable, and none of them is reported
   as nought.
4. **Given** a holding whose most recent price is older than one trading day, **When** its value is
   read, **Then** the value is marked as being derived from a stale price wherever it appears.
5. **Given** a holding whose price for a date was entered manually, **When** a fetch later obtains a
   price for that same date, **Then** the manual price is kept and the fetched one does not replace
   it — on any date, not only the current one.
6. **Given** a holding with a manually entered price dated in the future, **When** it is valued as
   at today, **Then** that price is not used, and it remains visible in the holding's price history.
7. **Given** the same holding, **When** it is opened in the web application and in the desktop
   application, **Then** every figure this story defines is identical in both, to the last digit
   displayed and in the same format.
8. **Given** a holding in Historic Investments, **When** it is read, **Then** it is not marked to
   market: its closing value is nought and no price is sought.
9. **Given** a price is fetched for a holding, **When** the value is read afterwards, **Then** the
   valuation reflects the fetched price without the user taking any further action.

---

### User Story 5 - What a portfolio and a broker are worth, and what they returned (Priority: P5)

The investor selects a portfolio, or a broker, and sees what it is worth today and the rate of
return it has produced, alongside what went in and what has come out — the first figures above the
level of a single holding. Where some of its holdings cannot be valued, the total says so and says
how many, rather than quietly leaving them out of a number that looks complete.

**Why this priority**: it sums the per-holding valuations User Story 4 produces, so built before
them there would be nothing to sum. It sits above User Story 4 rather than inside it because it
introduces a genuinely new judgement — what a total means when it is incomplete — and that deserves
acceptance scenarios of its own.

**Independent Test**: fully testable by taking a portfolio in which every holding has a recorded
price, confirming its market value equals the sum of its rows and its rate of return matches solving
the combined dated amounts with that value as the closing balance; and by taking the portfolio in
which no holding has a recorded price and confirming that neither a total nor a rate is presented as
complete, and that the count of unvalued holdings is stated.

**Acceptance Scenarios**:

1. **Given** a portfolio in which every holding has a recorded price, **When** its total is read,
   **Then** the market value equals the sum of its holdings' market values.
2. **Given** the same portfolio, **When** its rates of return are read, **Then** both a price-only
   return and a total return are reported, each solving the combined dated amounts of its holdings
   — the first excluding income, the second including it — with the portfolio's market value as the
   closing balance in both cases.
3. **Given** a portfolio in which 3 of 10 holdings have no recorded price, **When** its total is
   read, **Then** the total is not presented as complete and the number of unvalued holdings is
   stated alongside it.
4. **Given** that same portfolio, **When** its rate of return is read, **Then** no rate is reported,
   because the closing balance it would be solved against is incomplete.
5. **Given** a portfolio in which no holding has a recorded price, **When** it is read, **Then**
   this is distinguishable from an empty portfolio: one reports that there is nothing here, the
   other that nothing here can be valued.
6. **Given** a broker, **When** its total and rate are read, **Then** they follow the same rules as
   a portfolio's, aggregated over all of its portfolios.
7. **Given** a holding whose dated amounts contain fewer than two entries, or no change of sign,
   **When** a rate of return is sought, **Then** it is reported as unavailable rather than as
   nought per cent.
8. **Given** any selection the user can make, **When** totals are read, **Then** no total spanning
   more than one broker is offered.

---

### User Story 6 - Allocation by what things are worth now (Priority: P6)

The investor looks at how their portfolio is spread. Today the shares are worked out from what each
holding cost, so a position that has doubled still shows the share it had on the day it was bought.
After this story the shares are worked out from what the holdings are worth now. A holding that
cannot be valued shows an unknown share rather than nought per cent, and the portfolio says its
shares do not add up to a hundred. The income yield percentages keep the meaning they have today —
yield on cost — and are labelled as such rather than silently moving.

**Why this priority**: it needs a market value per holding to exist, so it cannot precede User
Story 4. It is last of the calculation stories because it is the only one where being wrong is
misleading rather than incorrect: a cost-based share is a true statement about a different question.

**Independent Test**: fully testable by confirming a holding that has appreciated shows a larger
share than its cost-based share did; by confirming the holdings that cannot be valued show an
unknown share and the portfolio discloses the shortfall; and by confirming the income yield
percentages are numerically unchanged and now carry a label saying they are measured on cost.

**Acceptance Scenarios**:

1. **Given** a holding whose market value has grown faster than the rest of its portfolio, **When**
   its share is read, **Then** it is larger than the share it had when computed from cost.
2. **Given** a holding with no recorded price, **When** its share is read, **Then** it is reported
   as unknown and not as nought per cent.
3. **Given** a portfolio containing at least one unvalued holding, **When** its shares are read,
   **Then** the portfolio states once that the shares do not total a hundred per cent.
4. **Given** a portfolio in which no holding can be valued, **When** its shares are read, **Then**
   it states that no share can be computed, rather than repeating "unknown" against every row.
5. **Given** any holding, **When** its income yield percentages are read, **Then** they are
   numerically identical to before this story and are labelled as measured on cost.
6. **Given** a holding in Historic Investments, **When** its share is read, **Then** it continues to
   be computed from cost, because a closed position has no market value.
7. **Given** the same share figure, **When** it is read anywhere in either front end, **Then** it is
   presented in one consistent format.

---

### User Story 7 - Knowing what the application cannot calculate (Priority: P7)

The investor asks the application what it cannot work out, and why. One report names every holding
with a problem: those whose sales exceed their purchases, those with no recorded price, and those the
application cannot classify. It changes nothing. Classifying a holding is a judgement the investor
makes one instrument at a time, so the report's job is to make the list visible and to keep it
shrinking as they work through it — not to guess.

**Why this priority**: it is the only story that delivers value with no dependency on any other,
which is exactly why it goes last — it can be deferred without stranding anything. It belongs in
this feature rather than a later one because it is where the anomalies the other stories deliberately
refuse to repair are surfaced, and where the holdings the other stories refuse to value are counted.

**Independent Test**: fully testable by running the report against a copy of the stored data and
confirming it names all three holdings whose sales exceed their purchases with the shortfall for
each, all seven open holdings with no recorded price, and every unclassified holding separated by
scope; and by confirming that running it leaves the data byte-for-byte unchanged.

**Acceptance Scenarios**:

1. **Given** the stored data, **When** the report is run, **Then** it names the 3 holdings whose
   sales exceed their purchases and states the shortfall for each.
2. **Given** the stored data, **When** the report is run, **Then** it names the 7 open holdings that
   have no recorded price.
3. **Given** the stored data, **When** the report is run, **Then** it names the 90 unclassified
   holdings, separated into the 3 in Active Investments and the 87 in Historic Investments, so the
   handful that still earn attention are not buried in the closed ones.
4. **Given** the stored data, **When** the report is run, **Then** it does not alter the data in any
   way, and running it twice produces the same result.
5. **Given** a holding that carries no local type code but has been given an asset class directly —
   today Bitcoin, BOVA11 and IVVB11 — **When** the report is run, **Then** it is treated as
   classified and is not listed as a gap.
6. **Given** the user has classified some holdings by hand, **When** the report is run again,
   **Then** the counts have fallen by exactly those holdings, and no others have changed.
7. **Given** an unclassified holding, **When** the user classifies it from within the application,
   **Then** the change takes effect immediately with no restart, and the holding no longer appears in
   the report. Only a change made by editing the stored file by hand requires a restart.
8. **Given** an unclassified holding that is in fact a bond or a cryptocurrency, **When** the report
   lists it, **Then** it makes clear that classifying it may also be what allows a price to be
   obtained for it, rather than listing the missing class and the missing price as unrelated
   problems.

---

### Edge Cases

- **Two transactions on the same date**: a transaction carries a date but no time of day, so a
  same-day purchase and sale are simultaneous as far as the stored history is concerned. Purchases on
  a date are applied before sales on that date (FR-002), which makes a same-day round trip valid
  rather than an apparent sale of units never held, and gives the same answer on every reading and
  after a reload. The consequence to accept is that a genuine intraday sequence of buy, sell, buy is
  flattened into both purchases and then the sale, so its average price is the blended one rather than
  the one the intraday order would have produced.
- **A sale dated before every purchase**: one stored holding is exactly this — a single sale of one
  unit with no purchase at all. It has no cost basis, so the realised gain is the whole proceeds. It
  must load, open and be correctable, and must be named in the report rather than silently valued.
- **A full redemption that overshoots by a fraction of a unit**: two stored holdings sell about four
  thousandths of a unit more than they hold, from fund redemption rounding rather than a typing
  error. Under a no-tolerance rule the user's remedy is to enter the exact held quantity, which is
  why the refusal must state it to full precision.
- **Editing or deleting a transaction on a holding that already sells more than it holds**: the
  position is re-derived from scratch on every change, so a rule applied during re-derivation would
  refuse the correction as well as the original mistake, leaving the three affected holdings frozen.
  Only the proposed resulting history may be judged, never the replay itself.
- **A purchase that brings a negative quantity to exactly nought**: divides by nought in the
  average-price calculation and fails outright. The failure is worse than it first appears: positions
  are rebuilt by replaying transactions as the stored file is read, and nothing on that path catches
  the error, so the application would fail to start rather than merely fail to open one holding — the
  same outcome FR-013 exists to avoid. No stored history triggers it today, and it is unreachable once
  sales are refused, but it remains reachable through a hand edit to the file.
- **A holding with no recorded price at all**: seven open positions. In three portfolios no holding at
  all can be valued, and in five the largest holding is the unpriced one. Value, unrealised gain,
  return, share of the portfolio and contribution to every total above it are all unavailable, not
  nought.
- **A price recorded days ago**: how stale the portfolio is depends entirely on when a fetch was last
  run, and it moves under the feature's feet — while this specification was being written, five
  holdings acquired a price for the current day that they had not had an hour earlier. At the time of
  measurement the priced open positions ranged from current to seven days old, with the bulk five days
  old. A value from an earlier day is still the best available answer; what must not happen is
  presenting it as today's.
- **A price dated in the future**: cannot be entered — recording a price dated later than today is
  refused — but the stored file can still contain one, because loading bypasses that check and writes
  the values as they are found. The current data contains none. A valuation as at today would ignore
  such a price anyway under FR-028, so it is harmless here. This becomes a real case only when a
  later wave values a holding as at an earlier date, where prices recorded after that date do exist
  and must be ignored — which is why FR-029 fixes the valuation date at today rather than leaving it
  open.
- **A manual and an automatic price for the same date**: cannot both be stored — a holding keeps at
  most one price per date, and a second write for that date replaces the first. There is therefore no
  read-time contest to resolve; the only question is which write wins. Today a fetch declines to
  overwrite a manual entry dated the current day, but on any earlier date the last write wins and a
  manual correction can be replaced without warning. FR-033 states the precedence as a property of the
  stored history so that it holds on every date rather than only on today's.
- **A position sold down to nought inside an otherwise open portfolio**: it costs nothing, is worth
  nothing and contributes no share. Today it is counted by the rows and excluded from the total above
  them, so the parts do not sum to the whole; after this feature both must treat it the same way.
- **A historic holding that still holds units**: four exist, one of them 28 units at just over 2,000
  of cost, and three more carrying the small negative quantities of the impossible sales. Because
  Historic is never marked to market (FR-034), a position that genuinely still holds units is reported
  as worth nothing there. That is the correct behaviour for the scope it is filed under and this
  feature does not change it, but it means the filing is wrong rather than the arithmetic, so the
  report names them (FR-074) and the remedy is for the user to move the holding back to Active
  Investments.
- **Historic Investments**: filed as closed and never marked to market — the closing value is
  nought and stays nought. Every rule about missing prices, staleness and market-based shares is inert
  there, and both the invested figure and the share basis stay on cost.
- **A portfolio with no holdings, and a portfolio where none can be valued**: three stored portfolios
  are in the second state and none is in the first — every portfolio currently holds at least one
  position. Neither state can produce a total, a rate or shares, but the two must be distinguishable
  to the user, because one means "there is nothing here" and the other means "we cannot tell you", and
  only one of them is a data problem.
- **A portfolio rate of return where one holding cannot be valued**: excluding that holding while
  keeping its purchases in the amounts reports a loss that did not happen, and treating its value as
  nought does the same thing more emphatically. The total and the rate are both withheld, and the
  count of unvalued holdings is shown instead.
- **A rate of return with fewer than two dated amounts, or with no change of sign**: not solvable, and
  reported as unavailable rather than as nought per cent. This is the existing behaviour and it must
  survive being called at the portfolio and broker levels.
- **A holding bought today and never sold**: one dated amount, so no rate of return; cost equals
  value, so a return of nought per cent. Both are correct, and neither must be confused with
  "unavailable".
- **A position closed and then reopened**: the average price of the reopened position must come from
  the new purchases alone, or the cost basis carries a ghost of the closed run.
- **A holding with income but no transactions**: nothing held and nothing invested, so the yield
  percentages have no denominator. Reported as unavailable, not as an infinite yield.
- **The desktop application and the web application recording prices independently**: they are
  separate processes sharing one stored file with no coordination between their writes, and each
  loads it once when it starts. A price recorded by one is invisible to the other until it restarts,
  so the two can legitimately value the same holding differently. This is pre-existing, but this
  feature is the first to promise that the two front ends show the same figure, which is what makes it
  visible.
- **Dealing costs pushing cost above market value**: fees are folded into the average price today, so
  a holding can show an unrealised loss made entirely of dealing costs. That is correct as an economic
  return; separating acquisition cost from allowable cost is out of scope here.

## Requirements *(mandatory)*

### Functional Requirements

**Replaying a position**

- **FR-001**: A position's quantity, average price, realised gain and average sell price MUST be
  derived by replaying its transactions in the order the transactions happened, regardless of the
  order in which they were entered or stored. Entering a back-dated purchase after a sale already
  recorded currently produces a different average price from entering the same transactions
  chronologically, and nothing tells the user which of the two figures is the real one — while every
  percentage, gain and rate in the application is computed downstream of that number.
- **FR-002**: Where several transactions share a date, every purchase on that date MUST be applied
  before any sale on that date, and the order MUST NOT depend on entry order or on storage order. A
  transaction carries a date but no time of day, so same-day entries are genuinely simultaneous as far
  as the stored history is concerned; without a stated rule the average price of a same-day
  purchase-and-sale pair is decided by whichever happened to be typed first, which is exactly the
  defect this section exists to remove. Purchases go first because units cannot be sold before they
  are acquired: applying sales first would refuse a same-day round trip under FR-009 as though it sold
  units never held.
- **FR-003**: Deriving a position MUST produce the same result every time it is derived from the same
  set of transactions, within a session and across restarts.
- **FR-004**: Editing a transaction's date, deleting a transaction, and loading the stored history
  MUST each produce the figures that chronological entry of the resulting set would have produced.
  Correcting a date is the user's only remedy for a mis-dated entry; if the correction does not
  re-derive the position, the remedy silently fails and the user has no way to detect it.
- **FR-005**: Derived figures MUST NOT be stored. They are re-derived from the transactions whenever
  they are needed, so that no stored figure can disagree with the transactions it came from. Quantity,
  average price, realised gain and average sell price are already excluded from the stored file, but
  two derived figures are still written: whether a position is long, flat or short, on every holding,
  and whether a portfolio is empty, on every portfolio. Both are derived from data stored alongside
  them and are stale the moment that data changes. Neither is ever read back — both are computed
  properties with no setter, so loading cannot populate them — which is what makes removing them safe
  in both directions.
- **FR-006**: Applying date order and FR-002 together MUST change the figures of exactly two stored
  holdings, and no others, and those two changes MUST be treated as corrections rather than
  regressions. Re-ordering by date alone changes nothing: only one holding is stored out of date
  order and its average price is unaffected by re-sorting. What does change is the two holdings that
  store a sale ahead of a purchase on the same date — at the precision actually displayed, one moves
  both its average price and its realised gain, the other only its realised gain — because the sale is
  now booked against the average cost that the same-day purchase has already contributed to. At full
  stored precision both holdings' average prices move; one of them rounds to the same displayed value
  either way, so a test of this requirement MUST state which precision it asserts at or it will read
  as a contradiction. The figures they show today are an
  artefact of the order the rows happen to sit in, which is precisely what this section removes; the
  requirement is stated as a bounded, enumerable change so that anything beyond those two is a defect.
- **FR-007**: Realised gain MUST be accumulated against the average cost in force at the date of each
  sale, in date order.
- **FR-008**: A position that is closed and later reopened MUST take its average price from the
  purchases made after it was closed, and MUST NOT carry cost from the closed run.

**Refusing an impossible sale**

- **FR-009**: The system MUST refuse a sale whose quantity exceeds the quantity held on that sale's
  date, MUST leave nothing stored, and MUST state in the refusal how many units were held on that
  date. A holding is a record of units actually owned; driving it negative produces a short position
  the application has no other way to reach, and every figure derived from it — realised gain, cost
  basis, unrealised gain, return — is then arithmetic over a quantity that never existed. Naming the
  held quantity is what turns the refusal into a correction the user can act on rather than a wall.
- **FR-010**: The refusal MUST apply to every route by which a user can change a position — recording
  a sale, changing an existing transaction's quantity, type or date, and deleting a transaction — not
  only to newly recorded sales. Deleting the purchase that funded a later sale leaves the position
  exactly as short as a mistyped sale would, so a rule that guards only new entries leaves the user a
  route into the state it exists to prevent.
- **FR-011**: A sale of exactly the quantity held MUST be accepted, and a sale exceeding the held
  quantity MUST be refused whatever the size of the excess, with no tolerance for rounding. Three
  stored holdings already carry a sale larger than the units held, two of them by less than a
  hundredth of a unit from a fund redemption; a tolerance wide enough to admit those would be a
  standing licence to record a cost basis the user never paid, and the honest remedy is for the
  refusal to state the exact held quantity so the user can enter it.
- **FR-012**: The held quantity stated in a refusal MUST be given to the full precision it is held
  at, so that a user closing a position entirely can enter it exactly.
- **FR-013**: Loading the stored history MUST NOT apply the refusal in FR-009, and every holding MUST
  remain openable, editable and deletable regardless of whether its stored history satisfies it.
  Three holdings recorded before this rule existed already breach it: applying the rule at load would
  stop the application starting, and applying it while re-deriving a position after an edit would
  make those three impossible to correct — the rule would be protecting the data from the only person
  who can fix it.
- **FR-014**: Bulk loading of a previously exported or imported history MUST be treated the same way
  as loading stored history under FR-013, and MUST NOT be refused for containing a sale larger than
  the units held.
- **FR-015**: The rule MUST be evaluated against the history that would result from the user's
  change, never raised from within the re-derivation of a position. Re-derivation runs on every edit
  and deletion, including the edits that correct a breach, so a rule raised from inside it cannot
  distinguish a mistake being made from a mistake being fixed.
- **FR-065**: The check MUST cover the whole of the resulting history, refusing the change if any sale
  at any date would sell more than was held at that date — not only the transaction the user is adding
  or editing. A back-dated sale can be entirely valid at its own date and still leave a later sale
  short, so a check confined to the changed transaction would leave the negative quantity FR-009
  exists to prevent reachable by a second route.
- **FR-066**: When a change is refused because it breaks a *later* transaction rather than the one
  being edited, the refusal MUST identify the transaction that would be left short and by how much. A
  user told only that their edit was refused, when the edit looks correct in isolation, has no way to
  find the transaction that actually conflicts.
- **FR-016**: The system MUST NOT fail with an unhandled error when a purchase brings a position's
  quantity to exactly nought. This is reachable only after a position has been driven negative —
  which FR-009 prevents for new entries but not for histories already stored — and the failure is
  total: because positions are rebuilt while the stored file is being read, and nothing on that path
  catches it, the application would fail to start rather than show a wrong number for one holding.
- **FR-017**: A stored holding that breaches FR-009 MUST be reported by the data-quality report
  (FR-053) rather than repaired automatically. What the correct history should have been is the
  user's knowledge, not the application's.

**What "invested" means**

- **FR-018**: The amount invested MUST have exactly one definition per scope, used by every surface
  that reports it — the holding, the portfolio total, the broker total and the allocation chart.
- **FR-019**: For a position in Active Investments, the amount invested MUST be the cost of the units
  still held. Reporting purchases minus sales tells a user who bought a hundred units at ten and sold
  fifty at twenty that they have nothing invested, while fifty units sit there at five hundred of
  cost; the figure is also what unrealised gain is measured against, so an understated basis
  overstates the gain.
- **FR-020**: For a position in Historic Investments, the amount invested MUST be the total of its
  purchases, and this MUST be decided by the scope the position is filed under, never by whether its
  quantity happens to be nought. Historic Investments is where closed positions are filed, and 128 of
  the 132 there are indeed flat, so applying FR-019 would blank the invested column, the broker totals
  and the entire allocation chart across the historic view, and would remove the denominator the
  income yield percentages are expressed against. But "closed" is a filing convention the application
  does not enforce: four historic positions still carry a quantity, one of them 28 units at just over
  2,000 of cost. Keying the rule to scope rather than to quantity is what keeps those four consistent
  with the 128 beside them instead of making the invested column mean two different things within one
  view.
- **FR-021**: The invested amount reported for a holding MUST NOT be negative in either scope.
- **FR-022**: A portfolio's or broker's reported invested amount MUST equal the sum of the amounts
  reported for the holdings within it, in both scopes. Five parts of the application derive this
  figure independently today and they disagree, so a user reading a total and then reading the rows
  beneath it is shown two different answers to the same question with nothing to distinguish them —
  and each front end already prints two of those answers on the same screen, a portfolio total above a
  column footer that sums the rows differently.
- **FR-023**: A holding that has been sold down to nought but is still filed under Active Investments
  MUST be treated identically by a portfolio's rows and by its total. It is currently counted by one
  and excluded from the other, which is one of the reasons the two disagree.
- **FR-070**: The allocation chart MUST include exactly those holdings whose invested amount is above
  nought, and MUST omit the rest. A holding with nothing invested has no share of how the money is
  spread, so it has no slice to draw; in Active Investments this omits positions that have been sold
  out entirely but are still filed there, and in Historic Investments — where the invested amount is
  total purchases (FR-020) — it omits nothing. This states the behaviour the chart already has rather
  than changing it, which matters because FR-019 redefines the figure the rule is applied to.
- **FR-024**: The allocation chart MUST be built from the same invested figures the rows report, and
  any rule about which holdings it includes MUST be stated rather than emerging from the figure's
  definition. The chart currently omits holdings whose invested figure is not above nought, so
  redefining the figure silently changes which holdings appear.
- **FR-025**: The amount invested, the basis for a holding's share of its portfolio, and the
  denominator of the income yield percentages MUST each be a separately identified figure from this
  point on, even where they currently hold the same value. One number serves all three purposes
  today, so any change to one silently moves the other two — and the market-based share required by
  FR-046 cannot be introduced without converting yield on cost into yield on market value by
  accident.

**Valuing a holding**

- **FR-026**: Market value, the cost of the units held, unrealised gain, price-only return and total
  return MUST be defined once and computed by the application, not by the front ends.
- **FR-067**: The system MUST report two rates of return wherever a return is reported: a **price-only
  return**, solved over the dated amounts of purchases and sales alone, and a **total return**, solved
  over those amounts together with income received. Both MUST be available for a holding, a portfolio
  and a broker, and MUST be labelled so the reader can tell which is which. This portfolio's return is
  substantially income — 1,485 income records across 160 holdings, 35 of them property funds — so a
  single blended figure would conceal the split between what the holdings grew and what they paid out,
  which is the distinction the asset view already draws today.
- **FR-068**: The two returns MUST be solved over the same closing value as each other, and that
  closing value MUST be the holding's or the level's market value. Income is already carried as its
  own dated amount in the total-return series, so adding income to the closing value as well would
  count it twice.
- **FR-027**: A holding's market value MUST be its quantity valued at the price applicable on the
  date it is being valued at.
- **FR-028**: A holding MUST be valued using the most recent price recorded on or before the date it
  is being valued at. Prices are recorded on the days they are fetched, not every day, so on any given
  day most holdings have no price bearing that day's date — when this was measured, five of the
  twenty-eight open positions carried a price for the current day and sixteen carried one between
  three and seven days old. A rule requiring an exact date match would therefore leave most of the
  portfolio unvalued, and how much of it depends only on when a fetch was last run.
- **FR-029**: Holdings MUST be valued as at today. A price cannot be dated later than today, because
  recording one is already refused, so "the most recent price on or before the valuation date"
  (FR-028) and "the most recent price recorded" are the same thing in this feature. Valuing a holding
  as at an earlier date is out of scope and MUST NOT be introduced here: it is needed by the disposal
  records and tax-year workbooks of later waves, where the as-of date is chosen by the user rather
  than always being today, and building it before there is a caller would be scaffolding.
- **FR-030**: Where no price has been recorded on or before the valuation date, the system MUST
  report the value as unavailable and MUST NOT report it as nought. Seven of the twenty-eight open
  positions have no recorded price at all, including the largest holdings in two portfolios; a nought
  would present them as worthless rather than unmeasured, and would flow into every total, share and
  rate that consumes them.
- **FR-031**: Every reported value MUST carry the date of the price it was derived from. The
  application cannot tell the difference between a price that is current and one left over from the
  last time a fetch succeeded, so without the date the user cannot either — and the difference
  between the two is the difference between a valuation and a guess.
- **FR-032**: A value MUST be marked as stale wherever it appears when the price it was derived from
  is dated earlier than the most recent weekday *strictly before* the valuation date. A price dated
  the valuation date itself is always current; a price from Friday is current when read on Monday, and
  stale when read on Tuesday. The "strictly before" matters: the most recent weekday *on or before* a
  Monday is that Monday, which would make every Friday price stale on Monday and contradict the rule's
  own purpose. Weekends are skipped
  because no price is ever recorded on them, so a rule counting plain calendar days would mark every
  holding stale every Monday and the marker would stop carrying information.
- **FR-069**: Staleness MUST NOT depend on a per-market holiday calendar. Holdings span more than one
  jurisdiction with different market holidays, and maintaining those calendars is ongoing work this
  application should not take on; the accepted consequence is that the first day after a public
  holiday marks values stale when the market was simply closed, which errs toward prompting the user
  to refresh rather than toward presenting an old price as current.
- **FR-033**: A manually entered price MUST take precedence over an automatically obtained one for the
  same date. This is not preserving an existing read-time rule, because there is none to preserve: a
  holding stores at most one price per date, so the two can never coexist and nothing chooses between
  them when reading. What exists today is a write-time guard that protects only the current day's
  manual entry from being overwritten by a fetch; on any earlier date the last write wins, and a
  manual correction can be silently replaced. Stating the precedence as a rule of the stored history
  rather than of one code path is what makes a manually corrected price survive.
- **FR-034**: A position in Historic Investments MUST NOT be marked to market: its closing value is
  nought, and no price is sought for it.
- **FR-035**: A price obtained from an external source MUST become available to valuation without the
  user taking any further action beyond the fetch they already perform.
- **FR-036**: Market value, the cost of units held, unrealised gain and return MUST have exactly one
  implementation, and neither front end may derive any of them from a price and a quantity of its
  own. Market value alone is derived in five places in the web application and two in the desktop
  application today, with three further sites deriving the cost of units held, and they already differ
  in whether the result is expressed as a fraction or as a percentage — so a correction to any one of
  them reaches one screen and misses the others.
- **FR-037**: Unrealised gain MUST be the difference between market value and the cost of the units
  held, and MUST be reported as unavailable whenever either of those is unavailable.
- **FR-075**: In Historic Investments unrealised gain MUST NOT be reported at all. A position filed as
  closed is measured by what it realised, not by what it might still gain, and FR-034 fixes its market
  value at nought — so subtracting the cost of any units it still carries would report a loss that has
  not happened. Four historic holdings still carry a quantity, one of them 28 units at just over 2,000
  of cost, and applying FR-037 there uniformly would fabricate a 2,000 loss for a position nobody has
  sold.

**Portfolio and broker totals**

- **FR-038**: The system MUST report a market value and a rate of return for a portfolio and for a
  broker, alongside the amounts already reported.
- **FR-039**: A portfolio's or broker's market value MUST equal the sum of the market values of the
  holdings within it that can be valued.
- **FR-040**: A portfolio or broker total MUST NOT be presented as complete while any holding within
  it cannot be valued, and the number of holdings left out MUST be stated alongside it. One stored
  portfolio has no valued holding at all and two others are missing their largest; a total that
  silently omits them is more misleading than no total, because it looks authoritative and is wrong
  by an amount the user cannot see.
- **FR-041**: A portfolio containing no holdings MUST be distinguishable from one whose holdings
  cannot be valued. One means there is nothing here and the other means the application cannot say,
  and presenting them identically hides a data problem behind an empty state.
- **FR-042**: A portfolio's or broker's rates of return — both of those required by FR-067 — MUST be
  solved over the combined dated amounts of its holdings with the total value of those holdings as the
  closing balance, and MUST NOT be reported when that closing balance is incomplete under FR-040. A
  rate solved against a closing balance missing a quarter of the capital is not an approximation of
  the right answer, it is a different answer.
- **FR-043**: A rate of return MUST be reported as unavailable, rather than as nought per cent, when
  there are fewer than two dated amounts or no change of sign among them. This is the existing
  behaviour for a single holding and it MUST survive being applied at the portfolio and broker levels.
- **FR-044**: The rate-of-return calculation itself MUST NOT be changed by this feature. It already
  orders its inputs and already declines to answer when it cannot, and every new use of it here
  depends on those existing behaviours.
- **FR-045**: The system MUST NOT produce a total spanning more than one broker. Currency is a
  property of the broker, so every portfolio and every broker holds a single currency and its totals
  are sound, while a figure across brokers would add one currency to another; converting between them
  requires dated exchange rates that this application does not hold.

**Weight and allocation**

- **FR-046**: A holding's share of its portfolio MUST be derived from what the holding is worth
  rather than from what it cost, in Active Investments. A share computed from cost answers a
  different question — how the money was originally committed — and reports a holding that has
  doubled at the share it had on the day it was bought.
- **FR-047**: Where a holding's value is unavailable, its share MUST be reported as unknown rather
  than as nought per cent, and the portfolio MUST disclose that its shares do not total one hundred
  per cent. Nought per cent reads as "this holding barely matters", which for the holdings concerned
  is the opposite of true.
- **FR-048**: A portfolio in which no holding can be valued MUST state once that no share can be
  computed, rather than reporting every row as unknown without explanation.
- **FR-049**: The income yield percentages MUST remain measured on cost and MUST NOT move to market
  value when the portfolio share does, and they MUST be labelled as yield on cost. They MUST be
  measured against the same cost figure the invested amount reports (FR-019, FR-020), so that a row's
  income percentage and its invested amount refer to the same money. This changes four holdings'
  percentages as a consequence of correcting the invested amount, one of them from negative to
  positive: the denominator in use today is purchases minus sales, which for a holding that has
  returned more cash than it consumed is a negative number, and a yield measured against a negative
  base has its sign inverted. Preserving those values exactly would mean preserving that defect and
  printing a yield against minus 683 beside an invested amount of 28.
- **FR-050**: In Historic Investments, a holding's share MUST continue to be derived from cost,
  because a closed position has no market value to derive it from.
- **FR-051**: A holding's share MUST be presented in one consistent format everywhere it appears in
  either front end. The same underlying figure is rendered at four sites in two different
  formats — the grids of both front ends agree with each other at one decimal place, and the detail
  views of both agree at two — so the same holding appears to have two different shares depending on
  whether it is read in a grid or on its own page.
- **FR-052**: Shares MUST be derived from the same valuations the totals are derived from, so that a
  holding's share and its contribution to the total cannot disagree.

**Knowing what the application cannot calculate**

- **FR-053**: The system MUST be able to produce a report naming every holding it cannot fully
  calculate, stating for each what is missing.
- **FR-054**: The report MUST name every holding whose sales exceed its purchases and state the
  shortfall for each — today, 3 holdings.
- **FR-055**: The report MUST name every open holding with no recorded price — today, 7 of the 28
  open positions.
- **FR-056**: The report MUST name every holding the application cannot classify, separating those in
  Active Investments from those in Historic Investments — today 3 and 87 of 160 respectively. The
  split is what makes the list usable: classification is a manual judgement made one instrument at a
  time, and the three open holdings are the only ones where it still affects anything the user is
  looking at.
- **FR-057**: A holding that carries no local type code but has been given an asset class directly
  MUST be treated as classified and MUST NOT be reported as a gap — today Bitcoin, BOVA11 and IVVB11.
  Some instruments have no local type code that fits, so setting the class directly is a legitimate
  outcome rather than an incomplete one.
- **FR-058**: The system MUST NOT infer, guess or automatically write an asset class or a local type
  code. There is no mechanical rule that recovers them: the classification of an instrument is the
  user's knowledge, applied one instrument at a time, and a guessed classification is worse than an
  absent one because it will be trusted. The report exists to make the remaining work visible, not to
  do it.
- **FR-059**: The report MUST NOT modify the stored data in any way, and running it twice MUST produce
  the same result.
- **FR-071**: Nothing in this feature may require a holding to carry an asset class or a local type
  code. Every figure this specification defines MUST be computable without one, so that an
  unclassified holding is valued, totalled, weighted and reported exactly like any other, given a
  price. Ninety holdings are unclassified and will be for as long as it takes to work through them by
  hand; a calculation that depended on classification would leave those holdings broken indefinitely.
- **FR-074**: The report MUST name every holding filed under Historic Investments that still carries a
  quantity — today four, one of them 28 units at just over 2,000 of cost. Historic holdings are never
  marked to market, so a position filed there while it still holds units is reported as worth nothing
  and contributes nothing to any total, which is correct for its scope and wrong for the facts. The
  application cannot tell whether the filing or the quantity is the error, so it reports and does not
  repair.
- **FR-073**: The report MUST state that an unclassified holding may also be one whose price cannot be
  obtained, and MUST NOT present the two problems as unrelated. Which source a price is fetched from
  is chosen by asset class: an unclassified holding is treated as exchange-listed, which is right for
  a share, a fund or an exchange-traded fund but wrong for a bond or a cryptocurrency, and those will
  therefore fail to price until they are classified. This feature neither changes that routing nor
  depends on it — the valuation rules treat a missing price identically however it came to be missing
  — but a user reading two separate lists has no way to know that classifying a holding may be what
  fixes its missing price.
- **FR-072**: Classifying a holding MUST be possible from within the application, and doing so MUST
  take effect without a restart. The application already offers this — a holding's country, local type
  code and class are all editable — so the report has somewhere to send the user, and the manual work
  this feature declines to automate is work the application already supports. Correcting an existing
  holding's country and local type code MUST also re-derive its class rather than leaving it as it was:
  today only creation derives the class from the other two, while editing keeps whatever class the form
  submits, so a user who fixes the two fields the report complains about is left with the holding still
  unclassified and no indication why. Only a hand edit made directly to the stored file requires a
  restart, because the file is read once when a process starts.

**Availability and parity**

- **FR-060**: Every figure this specification defines MUST be identical in the web application and
  the desktop application, to the last digit displayed, and MUST be presented in the same format in
  both. The portfolio share alone is already rendered in two different formats for the same
  underlying number — one decimal place in each front end's grid, two in each front end's detail view
  — so the same holding appears to have two different shares depending on where it is read.
- **FR-061**: Neither front end may re-derive any figure this specification defines. They present
  what the application computes.
- **FR-062**: A refusal MUST use the same wording and give the same reason in both front ends.
- **FR-063**: After a transaction is recorded, changed or deleted, every affected figure MUST reflect
  the change without a manual refresh or an application restart.
- **FR-064**: Values reported as unavailable, and values marked as stale, MUST be visually
  distinguishable from computed values in both front ends, and MUST NOT be rendered as nought or as
  an empty cell that reads as nought.

### Key Entities

- **Holding**: an asset held within a portfolio, owning its transactions, its income and its recorded
  prices. It is the unit everything in this specification is computed for. Its quantity, average
  price, realised gain and average sell price are derived from its transactions, never stored.
- **Transaction**: a dated purchase or sale of a quantity at a unit price, with dealing costs. It
  carries a date but no time of day, which is why a same-date ordering rule is needed.
- **Recorded price**: a price for a holding on a given date, either obtained automatically or entered
  by the user. Valuation reads the most recent one on or before the date being valued at.
- **Valuation**: what a holding is worth on a given date — its market value, the cost of the units
  held, unrealised gain and return — together with the date of the price it came from and whether
  that price is stale. It is either computed or explicitly unavailable; it is never nought by default.
- **Investment Scope**: the two groupings the user browses, Active and Historic Investments. Historic
  is where closed positions are filed and its holdings are never marked to market, which is why
  several rules here read differently in each. "Closed" is a convention rather than an enforced rule —
  four historic holdings still carry a quantity — so every rule in this specification keys off which
  scope a holding is filed under, not off whether its quantity is nought.
- **Portfolio and Broker**: the levels totals are reported at. A broker carries a currency, which its
  portfolios and holdings inherit, so every total defined here is single-currency.
- **Data-quality report**: the statement of what the application cannot calculate and why — the
  holdings that sell more than they hold, those with no price, and those with no classification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Entering the same set of transactions in any order produces identical quantity, average
  price, realised gain and average sell price — for every ordering of a representative set, not
  merely chronological and reversed.
- **SC-002**: Applying the replay-order change to a copy of the stored data changes the displayed
  figures of exactly two of the 160 holdings — both of which store a same-date sale ahead of a
  purchase — and leaves the other 158 identical, confirming the change is bounded and every departure
  from it is explained by FR-002.
- **SC-003**: 100% of attempted sales exceeding the units held are refused with the held quantity
  named, and no holding can be driven to a negative quantity through any route either front end
  offers — recording, editing or deleting.
- **SC-004**: The application starts, lists all 160 holdings and opens every one of them, including
  the 3 whose stored history already sells more than it holds, with zero manual edits to the data
  file.
- **SC-005**: A portfolio's reported invested amount equals the sum of its holdings' reported
  amounts, in both Active and Historic Investments, for 100% of portfolios. Today all 15 Historic
  portfolios disagree. The 10 Active ones happen to reconcile, but only because no active holding
  currently has a quantity of nought — the code paths diverge and the data does not yet expose it,
  which is exactly the kind of agreement that breaks silently the first time a position is closed.
- **SC-006**: The same holding shows identical market value, cost of units held, unrealised gain,
  return and portfolio share in both front ends — to the last digit displayed and in the same format
  — measured across every open position.
- **SC-007**: The number of places outside the single calculation owner that derive a value from a
  price and a quantity falls to zero, from ten today — five in the web application and two in the
  desktop application deriving market value, and three more deriving the cost of units held.
- **SC-008**: 100% of reported values name the date of the price they came from, and 100% of values
  derived from a price older than one trading day are marked as stale on every screen they appear on.
- **SC-009**: No portfolio or broker total or rate of return is presented as complete while any of its
  holdings is unvalued, and where holdings are unvalued the count is stated. Today that affects 7 of
  the 28 open positions, spread across five portfolios, three of which have no valued holding at all.
- **SC-010**: A holding whose value is unavailable never renders as 0% of its portfolio, and the
  shortfall from 100% is stated once per affected portfolio.
- **SC-011**: The income yield percentages are unaffected by the move to market-based shares — the
  share basis changes and they do not follow it — and are labelled as measured on cost on every
  surface that shows them. They are measured against the same cost figure the invested amount reports,
  so no holding's yield is measured against a negative base, which one is today.
- **SC-012**: One report names every holding the application cannot fully calculate — today 3 selling
  more than they hold, 7 with no recorded price, and 90 unclassified split as 3 open and 87 closed —
  and running it leaves the stored data byte-for-byte unchanged.
- **SC-014**: No figure this specification defines is withheld because a holding carries no
  classification. Among the 28 open positions the only ones missing a market value are the 7 with no
  recorded price; the 132 historic holdings have none by design (FR-034). Classification is never the
  reason a figure is unavailable.
- **SC-013**: A user can tell, for any figure on screen, whether it is computed, unavailable, or
  derived from a stale price, without leaving the screen.

## Out of Scope

This feature is the first of eight in a programme. What follows is deliberately deferred, with the
wave that owns it named so that nothing here is mistaken for an omission.

- **Transaction and income vocabulary (P47)**: any transaction type beyond a purchase and a sale;
  dealing costs, tax withheld, return of capital, capital calls, transfers, redemption and maturity as
  events in their own right; gross, withheld and net amounts on income; recording income corrections
  or negative income; net-of-tax return.
- **Valuation methods and provenance (P48)**: how a holding is valued — market price, net asset value,
  provider valuation or quote — as a property of the holding; the source, source reference, market
  status, currency and retrieval time of a recorded price; holdings measured in money rather than
  units, which is what the property-platform holding and the value-based funds among the seven
  unvalued positions require; recording a contribution without inventing units.
- **Currency (P49)**: a currency per transaction; dated exchange rates; a reporting currency; and any
  total spanning more than one broker.
- **Disposals and cost basis (P50)**: first-in-first-out and specific-identification methods; persisted
  disposal records; the policy that recalculating never rewrites an already-filed result.
- **Tax (P51)**: jurisdiction, tax year, event classification, withheld and exempt amounts, evidence
  references, calculation status and per-year workbooks. This feature records nothing tax-related.
- **Dashboard and wider data quality (P52)**: a portfolio dashboard; allocation by asset class,
  country, currency or provider; income to date; upcoming income, coupon and maturity dates; and the
  broader data-quality warning set. Only the report of what this feature itself cannot calculate is in
  scope here.
- **Corporate actions (P53)**: splits, consolidations, rights issues, mergers and spin-offs.

Also out of scope, with no successor named: automatically classifying holdings, or any tool that
writes an asset class or local type code — the roadmap's Wave 0 proposed one and it is deliberately
dropped, because no mechanical conversion exists; changing the rate-of-return calculation; changing
where prices are obtained from or adding a source; back-filling historic prices for the holdings that
have none; any change to the shape of the stored data; an undo or audit trail for corrected
transactions;
an affordance for selling an entire position that would remove the rounding class of impossible sale;
and coordinating writes between the desktop application and the web application.

## Assumptions

- **The amount invested is the cost of units still held in Active Investments, and total purchases in
  Historic Investments** — confirmed with the user. The two scopes answer different questions because
  a closed position has no open cost; applying the active reading to Historic would report nought for
  all 132 historic holdings and blank the historic invested column, broker totals, allocation chart
  and yield denominators.
- **Valuation uses prices the application has already recorded** — confirmed with the user. The most
  recent price on or before the valuation date, carried forward. Obtaining prices from external
  sources stays exactly where it is, in the bulk price fetch and manual price entry, and a fetched
  price is already recorded as that day's price, so no new mechanism is needed for a valuation to see
  it.
- **Stored history is loaded without being judged** — confirmed with the user. Rehydration and bulk
  import apply no rule; only what the user enters through the application is refused. Three holdings
  already sell more than they hold, so this is what keeps the application starting.
- **Impossible sales are refused with no tolerance** — confirmed with the user. Two of the three
  existing breaches are fund-redemption rounding of about four thousandths of a unit, and the remedy
  is that the refusal states the held quantity to full precision rather than that the rule bends.
- **A value is stale when its price predates the most recent weekday** — confirmed with the user.
  Weekends are skipped, so a Friday price is current on Monday and stale on Tuesday, and no per-market
  holiday calendar is kept even though holdings span two jurisdictions. How many holdings this marks
  depends on when a fetch was last run and changes constantly — at the time of measurement five of the
  twenty-eight open positions would have read as current, sixteen as stale and seven as unvalued. Most
  values reading stale until a fetch is run is the intended behaviour, not a defect.
- **Both a price-only and a total return are reported at every level** — confirmed with the user. The
  asset view already draws this distinction and it is preserved and extended to portfolios and
  brokers, rather than being collapsed into one blended figure.
- **An incomplete total is published with its shortfall stated, rather than withheld** — confirmed
  with the user, for totals. A rate of return is withheld entirely, because a rate solved against an
  incomplete closing balance is a different answer rather than an approximate one.
- **The desktop application does not obtain these figures over the network** — observed, and it
  **contradicts the roadmap**, which assumes the desktop switchover means adopting the same networked
  interface the browser uses. The desktop application performs the same calculations in its own
  process, exactly as it already does for profit and rate-of-return figures. The single owner is
  therefore a shared calculation both front ends reach directly; what the browser reaches over the
  network is an additional route to it, not the mechanism by which the two are made to agree.
- **The two front ends are switched over together in one increment each time** — a deliberate
  deviation from the documented slice order, which puts the desktop and web applications in separate
  increments. Switching one onto computed values while the other still derives its own leaves the same
  holding showing two different values on the same kind of screen, which is a parity regression rather
  than an unfinished increment.
- **Which front end is the source of truth is contested, and is not resolved here** — the repository's
  UI rules name the web application; the constitution at version 1.1.0 names the desktop application.
  Every requirement here is stated in terms of the two agreeing, so nothing in this specification
  depends on the answer, but the conflict is real and needs deciding elsewhere.
- **The rate-of-return calculation is not changed** — it already orders its inputs and already
  declines to answer when there are too few amounts or no change of sign. Every new use of it here
  relies on those existing behaviours rather than extending them.
- **No stored data shape changes** — this feature adds no field to any transaction, income record or
  price. It changes what is computed from them and what is reported to the front ends.
- **Redefining an existing figure is not caught by the automated contract check** — the check compares
  shapes, and the invested amount keeps its name and type while changing meaning, so it passes
  silently. That redefinition needs verification of its own rather than relying on the usual tripwire.
- **Each broker holds a single currency** — observed across all stored brokers. Currency belongs to
  the broker, so every portfolio and broker total defined here needs no conversion, and this is why no
  figure spanning brokers may be introduced.
- **The stored data is read once when a process starts** — so a hand edit to the file takes effect
  only after a restart, and the desktop and web applications each hold their own copy with no
  coordination between them.
- **Classification is manual and is never automated** — confirmed with the user, and it **diverges
  from the roadmap**, whose Wave 0 lists a "data-quality backfill tool + report" and whose decision D5
  requires the unclassified holdings to be filled in before any class-driven rule ships. There is no
  mechanical conversion available, so this feature reports the gap and writes nothing. The consequence
  reaches later waves: the valuation methods of P48 and the tax profiles of P51 are both planned to be
  driven by asset class, and each must therefore define what it does for an unclassified holding
  rather than assuming a backfill has happened. Historic Investments in particular may stay
  unclassified indefinitely — 87 of the 90 are closed positions where classification changes nothing
  the user is looking at.
- **A holding's country records where it is held, not where the issuer is domiciled** — observed, and
  recorded here at the user's request because nothing in this feature depends on it while two later
  waves do. Country tracks the broker's jurisdiction: every holding at the Brazilian broker is BR, and
  every holding at the three UK brokers is UK save one. Between fifty and sixty recognisably
  US-domiciled holdings — Apple, Tesla, PepsiCo, McDonald's, Johnson & Johnson, Procter & Gamble,
  Realty Income, Walmart and IBM among them — are recorded as UK because they sit in UK accounts, and
  exactly one holding in the whole file carries US. Two of them carry a US identifier and a US
  exchange while still being recorded as UK, so the field is not merely under-specified but locally
  contradicted by the other identifiers on the same holding. This is self-consistent for
  classification, since it resolves through `UK/Stock` as intended, and it is out of scope to change
  here.

  The consequence is for the waves that read the field as a jurisdiction rather than a venue. P51
  plans to tag events by tax jurisdiction, and withholding on a US-domiciled dividend is a US matter
  whichever account holds it, so a tax profile derived from this field would be wrong for those
  those holdings. P48's valuation-method routing is likely to be unaffected, since the venue is
  what determines where a price is fetched from. Whether the field needs splitting into custody and
  domicile is a decision for P51, not for this feature.

  The convention is also not enforced. One holding, AGNC Investment Corp., appears three times under
  the same broker and is recorded as US once and UK twice — so the field carries a convention that has
  drifted rather than a rule the application maintains. This feature neither repairs nor reports that,
  because no figure it defines depends on country.
- **The three holdings already classified directly stay as they are** — confirmed with the user.
  Bitcoin has no local type code that fits, and BOVA11 and IVVB11 carry a class set on the asset
  rather than derived; none of them is reported as a gap and none is "corrected" by this feature.
- **Dealing costs stay folded into the average price** — deferred. Separating what was paid to acquire
  a holding from what is allowable against it belongs with the widened transaction vocabulary.
- **The data-quality report is a report, not a screen** — it is produced on demand for the user to
  read, in the manner of the existing import tooling, rather than being surfaced in either front end.
  Surfacing data-quality warnings in the interface belongs to the later dashboard work.
- **Single user, no permissions** — as everywhere else in this application, there are no rules about
  who may record or correct a transaction.
