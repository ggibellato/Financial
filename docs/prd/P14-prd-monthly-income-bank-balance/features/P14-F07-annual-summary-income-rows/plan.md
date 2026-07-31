# Implementation Plan: Annual Summary Income Rows

**Prerequisites:**
- .NET 10 SDK and Node/npm toolchain already configured
- No new dependencies

### Stage 1: Backend Calculation

**1. Income Annual Summary DTO and Service** - Add the read model and the aggregation itself: per month, sum Gleison/Ariana gross and net values into the Salary and Salary-after-taxes rows, derive the Tax difference row from their gap, and sum DividendoJuros net values into their own row, each with a annual total.

**2. Income Summary API Endpoint** - Add the read-only HTTP endpoint that returns the year's income summary, following the existing Annual Summary endpoints' routing and response conventions.

### Stage 2: Frontend Integration

**3. Fetch and Render the Income Summary Table** - Add the corresponding type and API client method, fetch the income summary alongside the page's existing data, and render a new table matching the Category Totals table's column layout, with the header row, four data rows, and the intentionally blank row in their specified order.
