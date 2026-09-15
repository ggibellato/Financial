# Implementation Plan: F01. Tax Rules

**Prerequisites:**
- None — F01 has no dependencies on other P51 features (PRD Section 8)

### Stage 1: Domain

**1. Jurisdiction and Event Category Enums** - Introduce the two new enums that classify a tax rule: jurisdiction and event category. Reference the spec for their exact values.

**2. Tax Rule Entity** - Create the `TaxRule` entity with its identifying fields, its label/description, and its effective date range. Enforce the entity-level validation rules the spec describes (a required label, a coherent date range) and provide a way to ask whether a given date falls within the rule's range.

**3. Tax Rule Collection on the Investment Aggregate** - Extend the aggregate root with a new collection of tax rules and the operations needed to create, update, delete, and look one up by id or by applicability. Enforce the cross-rule invariant the spec describes: no two rules may cover the same jurisdiction and event category for an overlapping period.

### Stage 2: Persistence and Application

**4. Serialization Registration** - Register the new entity type with the existing serialization metadata so it can be constructed and persisted the same way every other aggregate member already is.

**5. Tax Rule DTOs and Service** - Add the wire-format representations for reading, creating, and updating a tax rule, and the application service that maps between them and the domain aggregate, following the spec's mirrored pattern from the existing broker service.

**6. Dependency Injection Registration** - Register the new service in the Investment application layer's composition root.

### Stage 3: API Surface

**7. Tax Rules Controller** - Expose list, create, update, and delete operations for tax rules over HTTP, following the spec's endpoint contracts and status codes.

**8. OpenAPI Contract Refresh** - Regenerate the committed OpenAPI snapshot and the generated frontend types to reflect the new endpoints and DTOs, per the spec's API Contracts section.
