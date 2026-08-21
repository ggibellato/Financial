# Feature Specification: Move Assets Between Portfolios

**Feature Branch**: `002-move-assets-between-portfolios`

**Created**: 2026-08-21

**Status**: Draft

**Input**: User description: "Move assets between portfolios. Users can move an asset from one portfolio to another. The target portfolio can be an existing portfolio, or a new portfolio created as part of the move. Special case: an asset with quantity = 0 is allowed to be moved from a portfolio in Active Investment to a portfolio in Historic Investment; the target Historic portfolio can be existing or created as part of the move. After an asset is moved, if the source portfolio ends up with no assets, the portfolio can be deleted — allowed but not mandatory. In both WPF and Web it must be possible to move via drag and drop; to move to a new portfolio the asset is dropped on the broker and a new portfolio name is then asked for."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Move an asset into another existing portfolio (Priority: P1)

An investor realises an asset is filed under the wrong portfolio — a share bought in a general
account was actually held in a tax-wrapped one, or a fund belongs in the "Pension" grouping rather
than "Default". From the asset they are looking at, they choose to move it, pick an existing
portfolio of the same broker as the destination, and confirm. The asset disappears from the old
portfolio and appears under the new one, carrying its full history: every transaction, every
credit, and every recorded price. No figure changes — the same average price, quantity, realised
gain, and dividend history are shown after the move as before.

**Why this priority**: This is the whole point of the feature and the smallest slice that delivers
value on its own. Today the only way to correct a misfiled asset is to hand-edit the data file and
restart the application, which is error-prone and unavailable to the user through either front end.
Every other story in this spec is an extension of this one.

**Independent Test**: Fully testable by taking any asset that has transactions and credits,
moving it to a second portfolio under the same broker, and confirming that the asset is listed
under the destination, absent from the source, and that its details page shows identical
quantity, average price, transaction count, credit count, and price history to before the move.

**Acceptance Scenarios**:

1. **Given** broker "Trading212" has portfolios "Default" (containing asset "AAPL" with 3
   transactions and 2 credits) and "ISA", **When** the user moves "AAPL" from "Default" to "ISA",
   **Then** "AAPL" is listed under "ISA", is no longer listed under "Default", and its details show
   the same 3 transactions, 2 credits, quantity, and average price as before the move.
2. **Given** an asset has recorded price history, **When** it is moved to another portfolio,
   **Then** every price entry — including which entries were manually set — is present afterwards
   and unchanged.
3. **Given** the user is moving an asset, **When** they select the portfolio it is already in as
   the destination, **Then** the move is rejected with a message saying source and destination must
   differ, and nothing changes.
4. **Given** a move has completed, **When** the application is restarted, **Then** the asset is
   still under the destination portfolio.
5. **Given** the destination portfolio already contains an asset with the same name, **When** the
   user attempts the move, **Then** the move is rejected with a message naming the conflict, and
   both portfolios are left exactly as they were.
6. **Given** a broker's portfolio summary showed totals for both portfolios, **When** an asset is
   moved between them, **Then** both portfolios' totals are recalculated so the asset's value now
   counts towards the destination and no longer towards the source.

---

### User Story 2 - Move an asset into a portfolio created during the move (Priority: P2)

The investor wants the asset somewhere that does not exist yet — they are introducing a "SIPP"
grouping, or splitting a crowded "Default" portfolio. Rather than having to create the portfolio
first and then move the asset, they type a new portfolio name in the move dialog, and the portfolio
is created and receives the asset in a single confirmed action.

**Why this priority**: Without it the user cannot reach a portfolio that does not exist yet, and
there is no other way to create a portfolio in either front end today — portfolios only come into
being through data import. It is separable from Story 1 (which works entirely with existing
destinations) but makes the feature genuinely usable.

**Independent Test**: Fully testable by moving an asset to a portfolio name that does not exist
under the broker and confirming that a portfolio with that name now exists, contains exactly that
one asset, and appears in the navigation tree and portfolio summaries.

**Acceptance Scenarios**:

1. **Given** broker "Trading212" has only the portfolio "Default", **When** the user moves an asset
   to a new portfolio named "SIPP", **Then** portfolio "SIPP" exists under "Trading212", contains
   only that asset, and appears in the navigation tree.
2. **Given** the user is creating a destination portfolio during a move, **When** they leave the
   name blank or enter only whitespace, **Then** the move is rejected with a message that a name is
   required, and nothing is created or moved.
3. **Given** broker "Trading212" already has a portfolio named "ISA", **When** the user tries to
   create a new destination portfolio also named "ISA", **Then** the move is rejected with a message
   that the portfolio already exists and should be chosen from the existing list instead, and no
   duplicate portfolio is created.
4. **Given** a new destination portfolio was named with leading or trailing spaces, **When** the
   move completes, **Then** the portfolio is stored under the trimmed name.

---

### User Story 3 - Archive a closed asset from Active to Historic (Priority: P3)

The investor has sold out of a holding completely — its quantity is zero, but its transaction and
dividend history still matters for tax and performance review. They move it out of Active
Investments into a Historic Investments portfolio (existing or newly named during the move), so the
active view shows only positions they still hold while the closed one remains fully browsable in the
historic view with its history intact.

**Why this priority**: This is the reason the user gives for wanting the feature at all, but it
builds directly on the mechanics of Stories 1 and 2 and can only be demonstrated once they work.
It is independently valuable — it is the only way to retire a closed position without editing the
data file.

**Independent Test**: Fully testable by taking an asset whose buy and sell quantities net to zero,
moving it from an Active portfolio to a Historic one, and confirming it appears in Historic
Investments with its full transaction, credit, and price history, and is gone from Active
Investments.

**Acceptance Scenarios**:

1. **Given** asset "VOD" under an Active portfolio has quantity 0, **When** the user moves it to an
   existing Historic Investments portfolio, **Then** "VOD" appears in the Historic Investments tree
   under that portfolio with its full transaction, credit, and price history, and no longer appears
   anywhere in Active Investments.
2. **Given** asset "VOD" has quantity 0, **When** the user moves it to a Historic portfolio name
   that does not exist yet, **Then** that Historic portfolio is created under the same broker and
   receives the asset.
3. **Given** asset "AAPL" under an Active portfolio has quantity 12, **When** the user attempts to
   move it to a Historic portfolio, **Then** the move is rejected with a message stating that only
   assets with zero quantity can be moved to Historic Investments, and nothing changes.
4. **Given** an asset under a Historic portfolio, **When** the user opens the move options, **Then**
   Active Investments portfolios are not offered as destinations.
5. **Given** an asset with a short position (quantity below zero) under an Active portfolio, **When**
   the user attempts to move it to Historic, **Then** the move is rejected for the same reason as a
   positive quantity.
6. **Given** a broker that appears in Active Investments but not yet in Historic Investments,
   **When** the user archives one of its zero-quantity assets, **Then** that broker appears in
   Historic Investments carrying the same name and currency, holding the destination portfolio and
   the archived asset — with no confirmation asked of the user.

---

### User Story 4 - Move an asset by dragging it in the tree (Priority: P4)

The investor is looking at the broker/portfolio/asset tree and can see both where the asset is and
where it should be. Rather than opening a dialog and re-picking the asset they are already pointing
at, they drag the asset's row and drop it on the destination portfolio. If the destination does not
exist yet, they drop the asset on the **broker** instead; the application asks what the new portfolio
should be called, creates it, and puts the asset in it. While dragging, the tree makes it obvious
which rows will take the asset and which will not, so an illegal move is visibly impossible rather
than merely rejected afterwards.

**Why this priority**: This is the interaction the user actually wants for everyday reorganising —
direct manipulation of the tree they are already reading. It sits at P4 because it is a second route
to a capability Stories 1 and 2 already deliver: dropping on a portfolio needs the existing-portfolio
move to work, and dropping on a broker needs portfolio-creation-during-move to work. Built before
them it would have nothing to call.

**Independent Test**: Fully testable by dragging an asset onto a sibling portfolio and confirming
the same outcome as the dialog route, then dragging another asset onto its broker, naming a new
portfolio, and confirming the portfolio was created with that asset in it — and by confirming that
invalid targets refuse the drop and change nothing.

**Acceptance Scenarios**:

1. **Given** broker "Trading212" has portfolios "Default" (containing "AAPL") and "ISA", **When** the
   user drags "AAPL" onto the "ISA" node and releases, **Then** "AAPL" moves to "ISA" with the same
   outcome, history, and figures as the dialog route would have produced.
2. **Given** the user drags an asset onto its broker "Trading212" and releases, **When** they are
   asked for a portfolio name and enter "SIPP", **Then** portfolio "SIPP" is created under
   "Trading212" containing that asset.
3. **Given** the user has dropped an asset on a broker and is being asked for a portfolio name,
   **When** they cancel the prompt, **Then** no portfolio is created, the asset stays where it was,
   and nothing is persisted.
4. **Given** a drag is in progress, **When** the pointer passes over the asset's own portfolio, a
   node of a different broker, another asset, or the tree root, **Then** each is shown as an invalid
   destination and releasing there changes nothing.
5. **Given** a drag is in progress, **When** the pointer passes over a valid destination portfolio,
   **Then** that node is visibly distinguished as the one that will receive the asset.
6. **Given** the destination portfolio already contains an asset with the same name, **When** the
   user drops the asset on it, **Then** the same rejection message the dialog route gives is shown
   and both portfolios are unchanged.
7. **Given** a drag is in progress, **When** the user releases outside the tree or on empty space,
   **Then** the drag is cancelled silently with no change and no error.
8. **Given** a drop has moved the last asset out of its portfolio, **When** the move completes,
   **Then** the user is offered the chance to delete the now-empty source portfolio, exactly as after
   a dialog move.

---

### User Story 5 - Remove a portfolio left empty by a move (Priority: P5)

After moving the last asset out of a portfolio, the investor is left with an empty grouping
cluttering the navigation tree. The application points this out as the move finishes and offers to
delete it; they can take the offer or decline it. Either way the portfolio can also be deleted
later, on its own, from the portfolio itself — so a portfolio emptied days ago is no harder to
remove than one emptied a second ago. The deletion is always their choice: an empty portfolio they
want to keep for future use is left alone until they say otherwise.

**Why this priority**: Pure tidy-up. The feature is fully usable without it; an empty portfolio is
harmless. It is listed last because it is explicitly optional in the request ("allowed but not
mandatory").

**Independent Test**: Fully testable by moving the last asset out of a portfolio and then deleting
the now-empty source portfolio — once through the offer made as the move finishes, and once through
the standalone delete on a portfolio emptied earlier — confirming in both cases that it disappears
from the navigation tree and from broker portfolio counts while the moved asset and its new
portfolio are untouched.

**Acceptance Scenarios**:

1. **Given** a move has just emptied the source portfolio, **When** the move finishes, **Then** the
   user is told the source portfolio is now empty and is offered the choice to delete it.
2. **Given** that offer is shown, **When** the user accepts it, **Then** the portfolio disappears
   from the navigation tree, the broker's portfolio count drops by one, and the moved asset is
   unaffected in its destination.
3. **Given** that offer is shown, **When** the user declines it, **Then** the empty portfolio
   remains listed with zero assets, the move stays applied, and the portfolio can be used as a
   destination for a later move.
4. **Given** a portfolio that has been empty since earlier in the session or since a previous
   session, **When** the user deletes it from the portfolio itself, **Then** it is removed exactly
   as it would have been through the post-move offer.
5. **Given** a portfolio still contains at least one asset, **When** deletion of that portfolio is
   attempted, **Then** it is rejected with a message that only empty portfolios can be deleted, and
   the portfolio and its assets are untouched.
6. **Given** the deleted portfolio was the node currently selected in the navigation view, **When**
   the deletion completes, **Then** the view recovers to a valid selection rather than showing a
   stale or broken portfolio.
7. **Given** an empty portfolio that was left in place, **When** the application is restarted,
   **Then** the empty portfolio is still listed.

---

### Edge Cases

- **Last asset of a broker moved away**: if a move (or a subsequent portfolio deletion) leaves a
  broker with no portfolios at all, the broker itself remains — broker removal is out of scope and
  the broker must continue to be listed so future moves can target it.
- **Only one portfolio under the broker**: no existing destination can be offered, so the user's
  only route is to name a new portfolio. The move options must make that possible rather than
  presenting an empty destination list with no way forward.
- **Asset with no transactions, credits, or prices**: an empty asset moves like any other; the
  destination shows it with zero counts.
- **Names differing only by letter case or surrounding whitespace**: a destination named "isa" when
  "ISA" already exists is treated as the same portfolio for the purpose of the duplicate-name
  rejection, so the user cannot end up with two portfolios that look identical in the tree.
- **A move fails partway through persisting**: neither portfolio may be left changed — the asset
  must not be able to vanish from the source without arriving at the destination, nor exist in both
  at once.
- **Two moves in quick succession**: a second move started before the first has been written must
  not corrupt or half-apply either — the data file is rewritten as a whole on every save.
- **Move requested for an asset, portfolio, or broker that no longer exists**: rejected as not
  found, with nothing changed.
- **Deleting a portfolio that was already emptied by an earlier move, later in the session**:
  allowed — deletion of an empty portfolio is not restricted to the moment immediately after a move.
- **Dropping an asset on a broker that has no portfolios at all**: valid — the drop asks for a name
  and creates the broker's first portfolio.
- **Dropping an asset on a collapsed portfolio node**: valid — the portfolio does not have to be
  expanded to receive a drop, and the destination must be unambiguous while collapsed.
- **Dragging while the tree is filtered by asset class**: the drop targets are the real portfolios,
  not the filtered view. If the moved asset falls outside the active filter after landing, the tree
  must not appear to have lost it — the outcome has to be intelligible rather than looking like a
  failed move.
- **A drag started and abandoned without releasing** (window loses focus, the key is released away
  from any target): treated as a cancel — nothing changes and the tree returns to its normal state
  rather than staying stuck in a dragging appearance.
- **Dragging in Historic Investments**: works exactly as in Active — an asset drags between Historic
  portfolios of the same broker. What no drag can express is the crossing between the two, since the
  Active and Historic trees are never on screen together.

## Requirements *(mandatory)*

### Functional Requirements

**Moving an asset**

- **FR-001**: Users MUST be able to move an asset from the portfolio it currently belongs to into a
  different portfolio.
- **FR-002**: A move MUST transfer the asset whole — its identifying details (name, ISIN, ticker,
  exchange, country, local type code, asset class), its complete transaction history, all its
  credits, and its entire price history including which entries were manually entered — with no
  value altered, added, or dropped.
- **FR-003**: A move MUST NOT create, delete, or modify any transaction, credit, or price record.
  Derived figures (quantity, average price, realised gain, totals, summaries, breakdowns) MUST be
  recalculated from the moved records rather than carried across as stored values.
- **FR-004**: The destination MUST be selectable from the portfolios that already exist and are
  valid destinations for that asset.
- **FR-005**: Users MUST be able to name a new portfolio as the destination, which is created as
  part of the same confirmed move and receives the asset.
- **FR-006**: The system MUST reject a move whose destination is the portfolio the asset is already
  in, leaving all data unchanged.
- **FR-007**: An asset MUST only be moved between portfolios of the broker it already belongs to.
  The system MUST NOT offer a portfolio of another broker as a destination, and MUST reject such a
  move if one is requested. Brokers carry their own currency and their own broker-level reporting,
  so relocating an asset across brokers is a different operation and is out of scope here.
- **FR-008**: The system MUST reject a move when the destination portfolio already holds an asset
  with the same name, leaving both portfolios unchanged and naming the conflicting asset in the
  message. The two assets MUST NOT be merged — combining histories cannot be undone in an
  application that keeps no move history, so resolving the duplicate stays the user's decision.
- **FR-009**: A move MUST be all-or-nothing: on success the asset is present in the destination and
  absent from the source and the change is persisted; on any failure both portfolios are left
  exactly as they were and nothing is persisted.
- **FR-010**: A completed move MUST survive an application restart.
- **FR-011**: The system MUST reject a move naming an asset, source portfolio, broker, or existing
  destination portfolio that does not exist, reporting it as not found and changing nothing.

**Creating the destination portfolio**

- **FR-012**: The system MUST reject a new destination portfolio name that is blank or whitespace
  only, changing nothing.
- **FR-013**: The system MUST reject a new destination portfolio name that matches an existing
  portfolio under the same broker, ignoring case and surrounding whitespace, and MUST tell the user
  to select that existing portfolio instead. No duplicate portfolio may be created.
- **FR-014**: A new destination portfolio name MUST be stored trimmed of leading and trailing
  whitespace.
- **FR-015**: A portfolio created by a move MUST be indistinguishable from one that arrived through
  import — it appears in the navigation tree, broker portfolio counts, and portfolio summaries, and
  can serve as the source or destination of later moves.

**Active and Historic investments**

- **FR-016**: The system MUST allow an asset whose quantity is exactly zero to be moved from a
  portfolio under Active Investments to a portfolio under Historic Investments.
- **FR-017**: The system MUST reject a move from Active Investments to Historic Investments when the
  asset's quantity is not exactly zero — whether positive or negative — and MUST explain that the
  position has to be closed first. Nothing changes.
- **FR-018**: The Historic destination MUST be selectable from existing Historic portfolios of the
  same broker, or nameable as a new Historic portfolio created by the move, under the same rules as
  FR-012 through FR-015.
- **FR-019**: The system MUST NOT offer or permit moving an asset from Historic Investments back
  into Active Investments.
- **FR-020**: After an Active-to-Historic move, the asset MUST appear only in the Historic
  Investments view and MUST be absent from every Active Investments view, listing, count, summary,
  and breakdown — and the reverse for the source.
- **FR-043**: When an asset is archived into Historic Investments and its broker is not yet present
  in Historic Investments at all, the system MUST create that broker's Historic record carrying the
  same name and currency as its Active record, and MUST place the destination portfolio under it.
  The user MUST NOT be asked to confirm this: it is the same real-world broker appearing in the
  historic view for the first time, and there is nothing for the user to decide.

**Removing an emptied portfolio**

- **FR-021**: Users MUST be able to delete a portfolio that contains no assets.
- **FR-022**: The system MUST reject deletion of a portfolio that still contains at least one asset,
  leaving the portfolio and its assets untouched.
- **FR-023**: Deleting an emptied source portfolio MUST be the user's choice, never automatic — a
  portfolio left empty by a move remains until the user deletes it, and stays available as a
  destination for later moves.
- **FR-024**: When a move leaves the source portfolio with no assets, the system MUST tell the user
  and offer to delete it there and then. Declining MUST leave the empty portfolio in place, and MUST
  NOT undo or affect the move that has already succeeded.
- **FR-025**: Deleting an empty portfolio MUST also be available on its own, from the portfolio
  itself, at any time — not only in the moments after a move. A portfolio emptied earlier in the
  session, or empty since a previous session, MUST be deletable the same way.
- **FR-026**: Deleting a portfolio MUST remove it from the navigation tree, from its broker's
  portfolio count, and from portfolio summaries, and MUST survive an application restart.
- **FR-027**: When the deleted portfolio was the currently selected navigation node, the view MUST
  recover to a valid selection instead of showing a stale or broken portfolio.

**Dragging an asset in the navigation tree**

- **FR-028**: Users MUST be able to perform a move by dragging an asset's node in the navigation
  tree and dropping it on a destination node. Dragging is an additional route to the same move, not
  a different one: every rule in this specification applies identically whichever route is used, and
  the resulting data is identical.
- **FR-029**: Dropping an asset on a **portfolio** node MUST move the asset into that portfolio,
  exactly as selecting that portfolio in the move dialog would.
- **FR-030**: Dropping an asset on a **broker** node MUST ask the user for a name for a new
  portfolio, then create that portfolio under the broker and move the asset into it. The name is
  subject to the same rules as any other new destination portfolio (FR-012 through FR-014).
- **FR-031**: Dropping on the broker node MUST always be understood as "into a new portfolio under
  this broker" — it MUST remain a valid destination even though the asset already sits under that
  broker, because it is the only route to a portfolio that does not exist yet.
- **FR-032**: If the user cancels or dismisses the new-portfolio name prompt raised by a drop, the
  move MUST NOT take place: no portfolio is created, the asset does not move, and nothing is
  persisted.
- **FR-033**: While a drag is under way, the system MUST show the user which nodes will accept the
  drop and which will not, and MUST visually distinguish the node currently under the pointer when
  it is a valid destination.
- **FR-034**: The following MUST be shown as invalid and MUST refuse a drop: the portfolio the asset
  is already in, any node belonging to a different broker, another asset node, and the tree root.
- **FR-035**: A drop on a node that looks valid but is refused by a move rule — most commonly a
  destination that already holds an asset of the same name (FR-008) — MUST report the same message
  the dialog route would report, and MUST change nothing.
- **FR-036**: Releasing a drag anywhere that is not a destination — an invalid node, empty space, or
  outside the tree entirely — MUST cancel it silently: no change, no error message.
- **FR-037**: A drag MUST NOT alter anything other than the asset's portfolio. It MUST NOT reorder
  the tree or change any other node. After a successful drop the tree MUST show the asset under its
  destination, with that asset as the selected node so the user can see where it landed.
- **FR-038**: Dragging MUST NOT become the only way to move an asset. The dialog route (FR-004,
  FR-005) MUST remain available, because it is the only route that can express the Active-to-Historic
  crossing (FR-016) and the only one usable without a pointing device.
- **FR-039**: A move completed by dropping MUST trigger the emptied-source-portfolio offer (FR-024)
  on the same terms as a move completed through the dialog.

**Availability**

- **FR-040**: Moving an asset by dialog, moving an asset by dragging, the offer to delete an emptied
  source portfolio, and the standalone deletion of an empty portfolio MUST all be available from both
  front ends with equivalent behaviour, wording of rejections, and outcomes.
- **FR-041**: Every rejection MUST tell the user which rule blocked the move in plain language, and
  MUST leave the data exactly as it was.
- **FR-042**: After a successful move or deletion, the views the user is looking at MUST reflect the
  new arrangement without requiring a manual refresh or an application restart.

### Key Entities

- **Investment Scope**: the two top-level groupings the user browses — Active Investments and
  Historic Investments. Every broker, portfolio, and asset belongs to exactly one of them. Crossing
  between them is the special case governed by FR-016 through FR-020.
- **Broker**: a named holder of portfolios with its own currency (e.g. Trading212, Chase). Owns the
  portfolios beneath it; is not created or deleted by this feature.
- **Portfolio**: a named grouping of assets beneath a broker (e.g. "Default", "ISA", "Pension"). Its
  name is unique within its broker and scope. This feature can create one, empty one, and delete an
  empty one.
- **Asset**: a holding identified by name, ISIN, ticker, and exchange, owning its transactions,
  credits, and price history. It is the unit that moves — it belongs to exactly one portfolio at any
  moment, and derives quantity, average price, and realised gain from the records it carries.
- **Move**: the user's request, made up of where the asset is now (scope, broker, portfolio, asset)
  and where it should go (destination portfolio, existing or to be created). Either it fully applies
  or it changes nothing.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can move an asset from the asset they are viewing to a different portfolio in
  under 30 seconds and no more than four interactions, without leaving the application.
- **SC-002**: 100% of an asset's transactions, credits, and price entries survive a move — the
  quantity, average price, realised gain, transaction count, credit count, and price history shown
  after the move are identical to those shown before it.
- **SC-003**: 100% of rejected moves and rejected deletions leave the stored data exactly as it was,
  and each one tells the user which rule blocked it.
- **SC-004**: A closed (zero-quantity) holding can be archived into Historic Investments and its
  emptied portfolio removed entirely through the application, with zero manual edits to the data
  file and zero application restarts — neither of which is possible today.
- **SC-005**: Both front ends offer the move by dialog, the move by dragging, and the empty-portfolio
  deletion, and the same acceptance scenarios produce the same outcome and the same rejection reasons
  in each.
- **SC-006**: After a successful move, every affected view — navigation tree, portfolio asset
  summary, broker breakdown, and aggregated totals — shows the asset counted under its new portfolio
  and no longer under the old one, with no manual refresh.
- **SC-007**: A move and a portfolio deletion each complete fast enough to feel immediate to the
  user (under 2 seconds), including on the largest existing data set.
- **SC-008**: An asset can be moved into an existing portfolio in a single drag, and into a portfolio
  that does not exist yet in a single drag plus typing a name — no other steps, no dialog, and no
  need to create the portfolio beforehand.
- **SC-009**: Every move a user can express by dragging produces exactly the same stored result as
  the same move made through the dialog, and every move refused by one route is refused by the other
  with the same reason.
- **SC-010**: While an asset is being dragged, a user can tell at a glance which rows will accept it,
  so no drop on an invalid target is ever attempted in the belief that it would work.

## Assumptions

- **Scope of movement**: an asset moves between portfolios of the *same broker* — confirmed with the
  user. Moving an asset to a different broker is out of scope: a broker carries its own currency and
  broker-level reporting, so that is a different operation with rules of its own.
- **Duplicate assets are never merged**: confirmed with the user. A name clash in the destination is
  reported and the move is refused; combining two histories is not part of this feature.
- **Empty-portfolio deletion has two entry points**: confirmed with the user — offered as the move
  finishes, and available on its own from any empty portfolio at any time.
- **Dragging cannot express the Active-to-Historic crossing**: Active Investments and Historic
  Investments are separate views that are never on screen at the same time, so there is no Historic
  node for an Active asset to be dropped on. Archiving a closed asset (User Story 3) therefore stays
  with the dialog route. Adding a Historic drop target to the Active view would be a way to close
  that gap, but it is not assumed here.
- **The navigation tree is the only drag surface**: assets are dragged from the broker/portfolio/asset
  tree. The portfolio and summary grids are not drag sources or drop targets.
- **Dragging is additive, never a replacement**: the dialog route remains the complete route. This
  keeps the feature usable without a pointing device and keeps the archive flow reachable.
- **Cross-scope direction**: only Active → Historic is supported, and only at exactly zero quantity.
  Historic → Active is not offered — Historic Investments is treated as an archive of closed
  positions, consistent with how the historic view already reports every position as flat.
- **Within-scope moves are unrestricted by quantity**: the zero-quantity rule governs *only* the
  crossing from Active to Historic. An asset with any quantity moves freely between two Active
  portfolios, or between two Historic ones.
- **Asset identity**: an asset is identified within a portfolio by its name, which is how the
  application already addresses assets. The duplicate-destination rule (FR-008) is expressed in
  those terms.
- **No broker lifecycle the user would notice**: this feature never deletes a broker, and never
  creates one the user would recognise as new. The single exception is FR-043 — brokers are held
  separately per scope and the two sets are not mirrors (one broker exists in Active with no Historic
  counterpart today), so archiving that broker's first closed asset has to bring its Historic record
  into being. A broker left with no portfolios remains listed.
- **No history of moves**: the application does not record that a move happened — no audit trail,
  no undo. A move is corrected by moving the asset back. This matches the single-user, self-hosted
  nature of the tool.
- **Portfolio has no attributes of its own**: a portfolio is a name and a set of assets, so creating
  one during a move needs nothing from the user but a name.
- **Watchlists, dividend lookups, and price fetching** address assets by ticker and ISIN rather than
  by portfolio, so they are unaffected by a move and need no changes.
- **Persistence unchanged**: both scopes continue to live in the single investment data document
  that is rewritten as a whole on every save; a move is one such save, which is what makes
  all-or-nothing behaviour (FR-009) achievable without new machinery.
- **Single user, no permissions**: there is one user, so there are no authorisation rules about who
  may move an asset or delete a portfolio.
