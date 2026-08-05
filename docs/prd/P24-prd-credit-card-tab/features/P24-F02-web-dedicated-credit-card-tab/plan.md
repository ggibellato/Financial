# Implementation Plan: F02. Web: Dedicated Credit Card Tab

**Prerequisites:**
- Node dependencies installed in `Financial.Web` (`npm install`)
- No new tools, libraries, or environment variables required

### Stage 1: Add the Credit Card Tab to the Monthly Page

**1. Monthly Page Tab Wiring** - Add a "Credit Card" tab to `MonthlyPage`'s tab strip, positioned right after "Expense", and render the existing card-statements grid a second time under it, reading from the same shared monthly data the Summary tab already uses. Reference the spec's Component Overview and Technical Decisions for exact placement and props.
