# Financial UI and UX Standards

## Purpose

This directory is the canonical source for Financial user experience, visual
design, accessibility, responsive/adaptive behavior, and cross-platform
consistency.

It applies to:

- `Financial.Web` — React + TypeScript SPA
- `Financial.App` — WPF desktop client
- API, validation, error, formatting, and calculation changes that affect either
  front end

## Product context

Financial is a single-user, self-hosted personal financial-management tool.

The UI supports:

- Investment transactions from Brazilian and UK brokers
- Income and expenses
- Bank and card balances
- Savings reserve
- Recurring bills
- Historical transactions
- Totals, trends, charts, and reports

This product is not multi-tenant SaaS. Do not add enterprise-scale complexity,
onboarding, roles, collaboration, approvals, or configuration surfaces without
a confirmed product requirement.

## React-led cross-platform rule

`Financial.Web` is the UX source of truth.

Define, validate, and improve intended user workflows in React first.
`Financial.App` must provide an equivalent WPF desktop experience while using
appropriate native controls, MVVM patterns, keyboard behavior, window layout,
and accessibility mechanisms.

React is authoritative for:

- User task sequence
- Information hierarchy
- Terminology
- Form field order and grouping
- Action labels and priority
- Validation wording and meaning
- Loading, empty, saving, success, and error behavior
- Financial formatting
- Totals behavior
- Responsive information prioritization

WPF must preserve the same user outcomes. It may adapt presentation details where
desktop conventions, keyboard efficiency, window sizing, or native control
behavior require it.

Existing React UI is not automatically correct merely because it is the
reference. Improve React first when it conflicts with product requirements,
accessibility, financial clarity, or the standards in this directory.

## Authority order

When guidance conflicts, use this order:

1. Security, privacy, safety, and accessibility obligations
2. Confirmed product and financial-domain requirements
3. Approved decisions in `docs/ui/decisions/`
4. Existing project architecture and established platform conventions
5. Documentation in this directory
6. Nielsen Norman Group usability heuristics
7. Microsoft Fluent 2 guidance
8. Current implementation, screenshots, and visual preference

## Documentation map

| Document | Use it for |
|---|---|
| `standards-hierarchy.md` | Resolving conflicts between product requirements, WCAG, usability, Fluent, and platforms |
| `ux-principles.md` | Product workflow and interaction decisions |
| `design-tokens.md` | Shared visual language and semantic tokens |
| `forms-data-and-visualisations.md` | Forms, grids, trees, charts, totals, and financial data |
| `accessibility.md` | WCAG-aligned Web and WPF accessibility requirements |
| `react.md` | React implementation rules |
| `wpf.md` | WPF implementation rules |
| `review-checklist.md` | Required final review |
| `current-state-audit.md` | What's actually implemented today (colors, spacing, components, accessibility) on both platforms, used to ground decisions and scope the refactor |
| `decisions/` | Approved product-specific UX decisions, including the concrete component libraries (`ADR-004`) and brand/status colors (`ADR-005`) |

## Claude Code

For significant UI work, invoke:

```text
/fluent-ui
```

The implementation workflow is:

```text
.claude/skills/fluent-ui/SKILL.md
```

The skill operationalizes these documents. It does not override them.