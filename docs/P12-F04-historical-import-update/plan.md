# Implementation Plan: F04. Historical Import Update

**Prerequisites:**
- F01 merged (importer already emits the new shape; entity invariant active)
- .NET SDK; no new packages

### Stage 1: Verification and Documentation

**1. Importer documentation** - Update the importer's documentation comment to describe the payment-state output shape (card rows import as unsettled charges; settlement is applied in-app afterward). See spec Section 4.

**2. Acceptance-criteria tests** - Add the importer-level tests covering the column-E precedence case and the whole-sheet guarantee that no imported expense carries both a bank and a card tag. See spec Section 7.

**3. Full-solution verification** - Run the complete .NET test suite to confirm everything stays green.
