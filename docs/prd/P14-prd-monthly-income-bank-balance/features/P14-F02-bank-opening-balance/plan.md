# Implementation Plan: Bank Opening Balance

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- No new external dependencies or environment variables

### Stage 1: Domain

**1. Bank Opening Balance Fields** - Extend the `Bank` entity with an opening balance and its effective date, with the entity itself enforcing that the balance can never be negative, following the existing entity-owns-its-invariants pattern in this domain.

### Stage 2: Application and Presentation

**2. Bank DTOs and Service Update** - Extend the bank read model with the two new fields, add the update request DTO, and implement the update workflow: resolving the target bank by name, delegating the non-negative check to the entity, and persisting the change.

**3. Bank Opening Balance API Endpoint** - Add the HTTP endpoint for updating a bank's opening balance and effective date, following the existing controllers' routing, status code, and error response conventions.

### Stage 3: Migration Tool

**4. Bank Opening Balance Migration Console Project** - Create a new console project that takes a timestamped backup of the data file before writing, loads the data, defaults the opening balance and effective date on any bank that has never been migrated, and prints a run summary — following the exact structure of the existing bank migration tool.

**5. Solution Registration** - Register the new console project and its test project in the solution file.
