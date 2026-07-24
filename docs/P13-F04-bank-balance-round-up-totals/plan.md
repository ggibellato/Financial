# Implementation Plan: F04. Bank Balance & Round-Up Totals

**Prerequisites:**
- F01, F02, and F03 merged (bank list, per-expense round-up amount, and both already available client-side)
- Node toolchain; no new packages

### Stage 1: Balance Computation

**1. `bankTotals` adjusted balance and round-up total** - Change the per-bank aggregation to produce both an adjusted balance (value minus round-up) and a separate round-up total in a single pass over the month's expenses, and expose the round-up sum across all banks alongside the existing balance sum. See spec Sections 3 and 4.

### Stage 2: Banks Panel UI

**2. Round-up column and footer** - Add the round-up total as its own column in the Banks panel table, next to the renamed balance column, and extend the footer row to show both sums. See spec Section 4.

### Stage 3: Verification

**3. Full-solution verification** - Run the frontend test suite and confirm in a running instance that saving an expense with a round-up amount immediately updates both the balance and round-up figures for its bank, and that a non-round-up bank always shows a zero round-up total.
