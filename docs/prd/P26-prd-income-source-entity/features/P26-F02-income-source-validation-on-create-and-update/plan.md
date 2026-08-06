# Implementation Plan: Income Source Validation on Create and Update

**Prerequisites:**
- F01 merged (seeded `IncomeSource` entity, `ICashFlowRepository.GetIncomeSources()`)
- No new NuGet packages required

### Stage 1: Validation

**1. IncomeSourceNameResolver** - Add a case-insensitive name resolver for the seeded `IncomeSource` list, mirroring the existing `BankNameResolver`'s shape and behavior exactly.

**2. Income Service Wiring** - Update income creation and update to resolve the submitted source name against the seeded list before persisting, rejecting an unresolved name with a validation error that names the invalid source, matching the existing Bank-name validation error's wording style. The check only confirms the name exists — it does not require the matched source to be active.
