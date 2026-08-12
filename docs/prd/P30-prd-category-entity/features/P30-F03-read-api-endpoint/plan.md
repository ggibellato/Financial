# Implementation Plan: Read API Endpoint

**Prerequisites:**
- F01 (Category domain entity, seed migration) merged to `main`
- No new tools or packages

### Stage 1: Category Endpoint

**1. Categories Controller** - Add a read-only endpoint that returns the full seeded category list, active and inactive alike, delegating to the existing category read service.

### Stage 2: Tests

**2. Controller and Endpoint Tests** - Add the constructor null-guard test alongside this codebase's other controllers, and an end-to-end test confirming the endpoint returns every seeded category with its correct fields.
