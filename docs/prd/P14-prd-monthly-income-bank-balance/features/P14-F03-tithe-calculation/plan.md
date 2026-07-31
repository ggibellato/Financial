# Implementation Plan: Tithe Calculation

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- No new external dependencies or environment variables

### Stage 1: Application and Presentation

**1. Tithe Summary DTO and Service** - Add the read model for a month's calculated tithe and tithe balance, and implement the calculation: summing that month's income to derive the tithe base, applying the fixed 10% rate, and subtracting that month's Dizimo-category expense total. Register the service for dependency injection.

**2. Tithe API Endpoint** - Add the read-only HTTP endpoint that returns a month's tithe summary, following the existing controllers' routing and response conventions.
