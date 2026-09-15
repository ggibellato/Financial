# Implementation Plan: F02. Tax Profile and Classification

**Prerequisites:**
- F01 (Tax Rules) — merged (#841, #842)

### Stage 1: Domain Foundation

**1. Classification Enums** - Introduce the source-type, calculation-status and classification-lifecycle enums, and add the one new event-category value the classifier needs for an unrecognized source event.

**2. Tax Classification Entity** - Create the entity that records a classified disposal or income event: its source reference, jurisdiction, tax year, category, the amounts relevant to that category, its calculation status and rule reference, and its own supersession lifecycle.

**3. Tax Classification Calculator** - Add the rule that builds a classification from a disposal record or a credit: deriving jurisdiction from currency, reusing the existing tax-year calculation, mapping a credit's type to a category, and resolving the applicable tax rule to decide the calculation status.

**4. Tax Classification Collection on the Asset** - Extend the asset with a new collection of classifications and the operations needed to append, find by source, supersede, and remove one. Nothing calls these yet — this stage is inert with respect to existing behavior.

### Stage 2: Backfill, Persistence and the Tax Rule Delete Guard

**5. Serialization Registration** - Register the new entity type with the existing serialization metadata.

**6. On-Load Backfill** - Add the pass that classifies every pre-existing disposal and qualifying credit that doesn't have one yet, following the same idempotent, failure-isolated pattern the existing disposal-record backfill uses, and wire it into the load sequence right after that one.

**7. Tax Rule Delete Guard** - Extend the tax rule deletion rule to refuse removing a rule that a final classification still depends on, closing the gap F01 deliberately left open for this feature to fill.

### Stage 3: Live Wiring

**8. Live Disposal Classification** - Thread the lookup needed to resolve an applicable tax rule into the disposal recording and regeneration paths, so a newly recorded, edited, retracted, or regenerated disposal is classified in the same step rather than waiting for the next load's backfill.

**9. Live Credit Classification** - Wire credit recording, editing, and deletion to create, replace, or remove the matching classification in the same step as the credit itself changes.

**PR boundary:** each stage ships as its own PR (4, 4, and 5 non-test files respectively), comfortably
under the repo's 8-non-test-file limit (`docs/rules/design.md` §PR size). The split follows a real
functional boundary, not an arbitrary file count: Stage 1 is an inert addition (nothing calls it
yet, so it's a safe no-op); Stage 2 makes every *pre-existing* disposal and credit classified via
the backfill, which is what makes F01's delete-guard meaningful, so both land together; Stage 3
wires the *live* path so new activity is classified immediately instead of waiting for the next
restart's backfill.
