# ADR 0003: Use capability-scope authorization and immutable effective baselines

- Status: Accepted
- Date: 2026-08-11

## Context

The BSC/KPI system must derive responsibility, approval routing, and data scope
from a governed Organization structure. Organization-specific roles must be
customizable like business-task administration in Microsoft 365 Admin Center,
but role names must not become authorization rules. Employees, sign-in
accounts, Positions, role assignments, delegation, and structure can change at
different times. Approved decisions must remain reproducible after those
changes, including a structure change inside an open KPI period.

Simple role checks cannot express an atomic task plus Organization/UnitSubtree/
Assigned/Self data scope, effective time, employment/account eligibility,
delegated represented authority, and same-artifact separation of duty. A mutable
current organization tree also cannot explain which structure resolved an old
approver or KPI responsibility.

## Decision

Use product-owned atomic KPI Capability identifiers as the authorization unit.
Organizations may assemble any identifiers into immutable versioned Custom KPI
Roles. An effective approved Role Assignment grants one exact role version to
one Employee inside one KPI Data Scope and effective interval. High-risk tasks
or scope above the safe threshold require independent approval. The system
defines a mandatory security floor; an Organization may only tighten it.

Every governed Application command performs resource-based authorization from
current durable facts through one decision interface. It evaluates account and
employment eligibility, effective Role Assignments, atomic capability, exact
scope containment, delegation intersection, and separation of duty. Role names,
claims copied at sign-in, menu visibility, controllers, and Razor pages are not
authoritative. Each allow/deny result has a stable reason and transactional
immutable Audit Record.

Organization editing uses an optimistic mutable workspace. Submission freezes
an immutable structure revision; independent approval creates an immutable
Organization Structure Baseline with a non-overlapping half-open UTC effective
range. Approval routes resolve from the applicable baseline and are snapshotted
at submission. Later structure changes never rewrite earlier route, baseline,
assignment, decision, or audit evidence.

A mid-period replacement baseline creates an immutable change-impact fact and a
deterministic proportional re-cascade preview. Existing weights are scaled into
the percentage remaining after fixed new weights, then rounded by largest
remainder with prior order and stable assignment identity as tie breakers. The
foundation does not mutate a KPI Plan or calculate official segment results;
later Planning/Evaluation modules consume the frozen impact and effective
segment contract.

PostgreSQL remains the race-safe guard: range/exclusion constraints protect
absolute non-overlap rules, `xmin` protects editable heads, immutable facts have
no update/delete interface, and the explicit migrator remains the only schema
writer.

## Consequences

- Custom role names and bundles can evolve without changing command code or
  granting their creator authority.
- Every authorization decision is scoped, effective-dated, explainable, and
  reusable across MVC, JSON, background work, tests, and future target hosts.
- UI action visibility improves usability but cannot bypass backend decisions.
- Historical approvals remain reproducible from exact baseline, route, role
  version, assignment, delegation, and audit evidence.
- Mid-period structure changes preserve before/after causality and provide one
  deterministic weight algorithm for future Planning consumers.
- The model requires additional immutable/versioned tables, effective-range
  indexes, and explicit route/scope queries; this complexity is accepted because
  mutable role checks cannot satisfy the governance requirements.
- Production identity integration remains an adapter concern. It links a
  sign-in subject to Employee/account status but does not redefine authorization.
