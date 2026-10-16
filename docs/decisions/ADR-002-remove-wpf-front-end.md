# Remove the WPF Front End

- Status: Accepted
- Date: 2026-10-13
- Deciders: Gleison Gibellato da Silva
- Related: `docs/pending/cashflow-accounts-generalization.md` (WPF removal, F00), `CLAUDE.md` (UI / UX invariant 1), `docs/rules/ui.md`, `docs/ui/wpf.md`, ADR-001

## Status

Accepted. Implemented by F00, the first slice of the CashFlow accounts generalization. Until
F00 merges, the current two-front-end rules still apply.

## Context

The application has two front ends built on the same backend: `Financial.Web` (React,
served by `Financial.Api`) and `Financial.App` (WPF, calling the Application services
in-process). `CLAUDE.md` UI / UX invariant 1 requires them to stay at parity: React defines
the workflow, and WPF must deliver the same tasks, terminology, field order, validation and
outcomes.

That parity has a real cost. Every user-facing change is built, tested and reviewed twice,
in two UI stacks with different test tooling. `Financial.App` is about 35k lines of C# and
XAML (389 files), `Financial.Presentation.Tests` about 30k more, plus a FlaUI end-to-end
project. CI runs two WPF jobs on Windows (`wpf`, `wpf-e2e`) and the nightly run has its own
WPF end-to-end job. `docs/rules/ui.md`, `docs/ui/wpf.md`, the review checklist, the
`ui-reviewer` agent and the `fluent-ui` and `testing-guide-Financial` skills all carry WPF
parity rules.

The next planned work, the CashFlow accounts generalization, replaces today's per-type pages
(Monthly, Reserva, Controle Mãe, card statements) with an account-centred UI: the user picks
an account and does everything for it from there. It is a large UX redesign. Building it
twice and keeping both copies in step would roughly double the UI work, for a single-user,
self-hosted tool that the user already runs through the browser.

## Decision

Remove `Financial.App` (WPF) from the whole application, CashFlow and Investment alike.
`Financial.Web` (React) becomes the only front end.

- The removal is done first, as F00, before any account-centred screen is built, so no slice
  builds WPF screens that would then be deleted.
- Before `Financial.App` is deleted, every Investment and CashFlow workflow must already
  exist in React. The F00 PRD records that check and closes any gap first.
- F00 removes the WPF projects and tests, the WPF CI and nightly jobs, the WPF coverage
  entries, the WPF parts of the local deploy scripts, and the WPF rules, docs and skills.
  The full inventory lives in the F00 PRD.
- This ADR supersedes `CLAUDE.md` UI / UX invariant 1 ("React-led UX with equivalent WPF
  outcomes"). After F00 the invariant describes a single React front end.

## Alternatives Considered

- **Keep both front ends at parity.** Rejected. It doubles the UI cost of the account-centred
  redesign and of every later UI change, for no user benefit: there is one user, and the
  browser front end already covers their workflows.
- **Freeze WPF: keep it running but stop adding features to it.** Rejected. The WPF screens
  would drift from the backend as the CashFlow model changes (accounts, linked transactions,
  new endpoints) and would either break or show wrong data. Keeping it compiling and its tests
  green would still cost work on every backend change.
- **Remove WPF only from CashFlow.** Rejected. Half a desktop app still keeps the WPF CI jobs,
  rules, skills and test tooling, so most of the cost remains, and the user would have to
  use two apps.

## Consequences

### Positive

- Every UI change is built, tested and reviewed once.
- The account-centred redesign and every later slice need about half the UI work.
- CI gets faster and simpler: no Windows WPF build, no FlaUI end-to-end job, one less
  coverage gate.
- The rules, skills and review agents lose their parity sections, so they are shorter and
  there is one UI standard to follow.
- ADR-001's note that `Financial.App` and the API controllers are both coupled to the
  `I*Service` interfaces no longer applies: the API is the only caller.

### Negative

- There is no desktop app. The application is used only through the browser, which needs
  the API running (Docker, or the local deploy's web server).
- Any workflow that exists only in WPF is lost unless it is rebuilt in React first. F00's
  parity check exists to prevent this.
- The WPF Google Calendar sign-in (its own OAuth loopback listener) goes away; only the API's
  callback flow remains.
- Reversing this decision later means rebuilding a desktop front end from scratch. The code
  stays in git history, but it would be far out of date with the backend.
