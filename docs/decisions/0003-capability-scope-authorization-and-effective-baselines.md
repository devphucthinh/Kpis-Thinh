# ADR 0003: Use capability-scope authorization and immutable effective baselines

- Status: Accepted
- Date: 2026-08-11
- Updated: 2026-08-12

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
immutable Audit Record. Decisions are never cached across governed actions;
identical checks may be memoized only within one action execution while every
authorization input remains identical.

Organization editing uses an optimistic mutable workspace. Submission freezes
an immutable structure revision; independent approval creates an immutable
Organization Structure Baseline with a non-overlapping half-open UTC effective
range. Before the first baseline begins, dependent operations are denied. From
that instant onward baseline applicability is a gapless chain: successor
approval locks the Organization chain, atomically closes predecessor
applicability at the successor start, and opens the successor segment. The
reviewed baseline content remains immutable. Approval routes have optimistic
heads and immutable versions. Every new or changed version is frozen on
submission, independently approved by an eligible actor outside its maker/
editor set, and activated only by an eligible non-maker. One optimistic
activation slot per Organization and artifact type serializes replacement:
activation retires the prior active version/route and activates the approved
target atomically, so standalone retirement cannot create an unroutable gap.

The first Organization is provisioned with two distinct Bootstrap Principals:
one setup identity and one independent-approval identity. Their product-owned
grants are Organization-scoped, non-delegable, restricted to a fixed bootstrap
task allowlist, fully audited, and still subject to same-artifact separation of
duty. The first baseline freezes only structure and workforce facts; Role
Assignments are separately governed and never baseline members. After the
baseline exists, the principals establish independently approved replacement
Role Assignments against it. When exact effective assignments cover both
duties, one immutable handoff atomically expires both bootstrap grants.

If one principal becomes unavailable before handoff, only a time-bounded
platform break-glass request approved by two distinct Platform Security
Administrators may replace it. Neither platform approver may be a Bootstrap
Principal, the replacement cannot equal the remaining principal, and the
request, decisions, expiry, replacement, grant, and audit evidence are
immutable. This recovery authority cannot inspect or mutate ordinary KPI facts.

Typed route selectors resolve from the applicable baseline. An Organization
Unit Head is an explicitly configured Employee validated in the relevant unit;
a Direct Manager starts from artifact Position context and falls back to the
applicable primary Position only when context is absent. Named groups use
Organization-scoped internal Approval Groups with effective-dated Employee
memberships. Submission freezes Position resolution, group membership,
candidates, scope, and fallback evidence. Later structure or group changes never
rewrite earlier route, baseline content, assignment, decision, or audit evidence.

A mid-period replacement baseline creates an immutable change-impact fact and a
deterministic proportional re-cascade preview. Existing weights are scaled into
the percentage remaining after fixed new weights, then rounded by largest
remainder with prior order and stable assignment identity as tie breakers. The
foundation does not mutate a KPI Plan or calculate official segment results;
later Planning/Evaluation modules consume the frozen impact and effective
segment contract. The impact itself has no mutable lifecycle field. A later KPI
Planning approval command resolves it only by registering a separate immutable
Baseline Impact Resolution containing the exact approved Plan Amendment
revision and decision evidence. The Planning command and resolution fact commit
in one unit of work; duplicate registration of the same evidence is idempotent,
while a different reference, unapproved evidence, or cross-Organization
reference is rejected.

PostgreSQL remains the race-safe guard: row locking plus atomic predecessor
close/successor insert protects continuity, range/exclusion constraints protect
non-overlap, `xmin` protects workspace/policy/role/route/group/activation-slot/
assignment heads,
immutable facts have no update/delete interface, and the explicit migrator
remains the only schema writer.

## Consequences

- Custom role names and bundles can evolve without changing command code or
  granting their creator authority.
- Every authorization decision is scoped, effective-dated, explainable, and
  reusable across MVC, JSON, background work, tests, and future target hosts.
- UI action visibility improves usability but cannot bypass backend decisions.
- Historical approvals remain reproducible from exact baseline, route, role
  version, Position/group selector evidence, assignment, delegation, and audit
  evidence.
- Approval Route configuration cannot be self-approved or activated by its
  maker, and replacement cannot leave an artifact type without an active route.
- Mid-period structure changes preserve before/after causality and provide one
  deterministic weight algorithm for future Planning consumers.
- A Baseline Change Impact is resolved only by durable, independently approved
  KPI Plan Amendment evidence; no UI/API status toggle can bypass that link.
- The first Organization can become governed without a permanent hard-coded
  administrator or a self-approved first user, and its bootstrap recovery path
  cannot silently weaken two-person control.
- Baselines remain immutable structure/workforce evidence instead of becoming
  authorization snapshots; Role Assignment history evolves independently.
- Revocation and other authorization changes apply to the next governed action
  without a stale cross-action decision window.
- The model requires additional immutable/versioned tables, effective-range
  indexes, and explicit route/scope queries; this complexity is accepted because
  mutable role checks cannot satisfy the governance requirements.
- Production identity integration remains an adapter concern. It links a
  sign-in subject to Employee/account status but does not redefine authorization.
