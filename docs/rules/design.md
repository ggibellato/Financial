# Design rules

Read this before writing a PRD, spec or implementation plan, and before deciding where a feature belongs or how to slice it.

Rules about how the code itself is written are in `implementation.md` — **do not load it at this stage**. The architecture invariants that bind here and everywhere else are in `CLAUDE.md`.

## Before you design

Answer these four in writing before proposing an approach. If you cannot answer one, the design is not ready.

1. **Where does this belong?** Which bounded context, and which layer owns the behaviour — not which layer is convenient to change.
2. **Which layers does it touch, and in which direction?** List them. A feature that touches all four is usually two features.
3. **What specifically stops Domain from learning about Infrastructure here?** Name the interface and the layer that declares it.
4. **Which SOLID principles is the design leaning on, and where?** "All of them" is not an answer.

## Slicing

Every feature is a sequence of small, independently reviewable increments.

- Each increment delivers a complete working slice, or a working compatibility boundary — never scaffolding, placeholders, disconnected infrastructure, or code that cannot be exercised or verified on its own.
- Each increment carries its own implementation, configuration, tests and documentation, so it is usable and reviewable by itself.
- Identify the scope, dependencies, acceptance criteria and review boundary of every increment **before** implementation starts.
- The established shape is one vertical slice per PR, in this order: Domain → Application → Infrastructure → API → WPF → Web → tests.

This is a hard requirement, not a preference.

## PR size

Limit each PR to a maximum of **8 non-test code files**.

**Exception — one change repeated.** A PR that applies the same mechanical change across many files may exceed 8. Review cost per file is near zero once the reviewer has read the first one, and splitting such a sweep by file count actively hides whether it is complete.

T050 of the observability work is the reference case. Adding `using var span = StartSpan("GetCategoryTotalsForYear");` and its entry/success logging to every Application service was one change repeated across 25 services, and the file limit split it into three PRs — #482 (16 files), #483 (17), #484 (24). It should have been one.

The exception holds only while the change really is identical everywhere. The moment some files need a judgement call the others do not, it is a heterogeneous change and the limit of 8 applies again.

If a slice fits under neither, it is two slices.

## Deployability

Design so that every increment leaves `main` deployable on its own. Deployable means:

- It builds via the standard process (`dotnet build`, `npm run build`).
- The required automated test suite passes.
- It starts using the standard production configuration (`docker-compose up`), not a dev-only profile.
- It does not require unfinished increments, local-only tools, dev containers, or unavailable external services to start.
- Existing production functionality still works — no regressions.
- No known defect introduced by the increment blocks a deployment.

**If an increment cannot satisfy all six on its own, re-slice it.** That decision belongs here, at design time — discovering it during implementation means the plan was wrong.

## Tests are part of the design

Decide at design time what each increment must prove and at which layer. The `testing-guide-Financial` skill maps artifact type → required tests → how to set them up.

A feature with no test plan is not designed yet.

## Not decided here

Naming, method size, logging, failure signalling, and where a class sits *within* a layer are implementation rules. Leave them to `implementation.md` — deferring them is what keeps this file safe to load during discovery and design.
