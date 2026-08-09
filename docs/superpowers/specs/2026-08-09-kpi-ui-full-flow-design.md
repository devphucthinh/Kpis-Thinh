# KPI Management UI Full-Flow Design

Date: 2026-08-09  
Status: Design approved for implementation planning  
Scope: The server-rendered UI journey for the existing KPI Management domain

## 1. Context and goals

The domain and API already model KPI Definitions, KPI Versions, Formula
Variables, Formula Test Runs, KPI Period Plans, Activations, KPI Evaluations,
Superseding Evaluations, and Audit Records. The current MVC pages expose these
capabilities as disconnected forms. A user can create or edit a formula, but
cannot follow the complete governed journey without knowing individual routes
or API calls.

This design makes the existing behavior discoverable and operable from a
cohesive UI. It does not change the KPI domain semantics, add production
identity, or introduce a dashboard/employee-assignment product. The prototype
continues to use the Development persona switcher and Vietnamese-first copy;
the structure is ready for English localization later.

The user outcome is a guided path from KPI draft through approval, publication,
period activation, official evaluation, correction, and audit review. Every
mutation remains owned by an existing Application operation and is reflected
in the state and audit information shown by the UI.

## 2. Chosen approach

Use server-rendered MVC pages with progressive enhancement:

- Razor views remain the primary rendering boundary.
- Existing MVC actions and `/api/v1` contracts remain the source of behavior;
  controllers only map view models, invoke Application operations, and choose
  the next page.
- Small JavaScript modules enhance formula validation, AST preview, Test Run,
  modal forms, theme switching, and unsaved-change warnings. JavaScript is not
  required to complete a governed state transition.
- Markup and layout are Bootstrap 5-compatible and use a local design-token
  stylesheet. The implementation may reuse an existing Bootstrap-compatible
  CSS dependency if one is already approved, but this design does not require
  a SPA or a new frontend build pipeline.
- Mutating MVC actions use Post/Redirect/Get. A successful action redirects to
  the relevant workbench section and shows a short success message; validation
  errors return to the same section with field-level and summary messages.

This approach fits the existing `Kpi.Web` MVC boundary, keeps behavior easy to
test at HTTP seams, and is straightforward to copy into the larger `.cshtml`
project described in `HUONG_DAN_TICH_HOP_KPI.txt`.

## 3. Information architecture

The shared shell has a consistent top bar and navigation:

1. **Tổng quan** — counts and next actions for the current Development
   persona; no new analytics domain is introduced.
2. **KPIs** — list, create, and open a KPI Workbench.
3. **Kỳ KPI** — list and open KPI Period Plans, selections, approval, and
   activation status.
4. **Đánh giá** — official evaluation entry, current result, immutable history,
   and correction entry where the domain permits it.
5. **Audit** — filterable Audit Record timeline.

The top bar also shows the current simulated persona and a Light/Dark theme
toggle. Persona switching remains Development-only and keeps the existing
capability checks; the UI must not imply that it is production authentication.

## 4. Visual system and responsive behavior

### 4.1 Visual language

- Use a restrained indigo primary action, neutral surfaces, and semantic
  status badges for Draft, In Review, Approved, Published, Retired, Scheduled,
  Active, Closed, Success, and Failure.
- Use a consistent page grid: navigation/sidebar, page heading and actions,
  then cards for the current task and supporting history.
- Show state through badge text and icons in addition to color. Error and
  warning messages must remain understandable in monochrome.
- Formula text and AST use a monospace face; numeric outputs use a stable
  Decimal display and never imply binary floating-point behavior.
- Keep Vietnamese labels as the default. Every new label is assigned a stable
  resource key so English can be added without changing routes or domain
  terminology.

### 4.2 Theme

Light and Dark themes share the same semantic tokens (`surface`, `surface-
raised`, `text`, `muted`, `border`, `primary`, `success`, `warning`, `danger`).
The initial theme follows `prefers-color-scheme`; the user can override it
with the top-bar toggle. The override is stored locally in the browser and is
not persisted as product data. All interactive controls, focus rings, tables,
formula editors, and diagnostics must pass the repository's contrast and
keyboard checks in both themes.

### 4.3 Responsive layout

At desktop width the KPI Workbench uses a collapsible left list and a main
editor. At narrow widths the list becomes a top selector/drawer and editor
cards stack vertically. Period and evaluation tables become horizontally
scrollable or card-based summaries; no essential action is hidden only on
mobile.

## 5. Slice 1 — KPI Workbench

The Workbench is the primary entry point for a KPI Definition.

### 5.1 List and selection

The KPI list shows code, human-readable name, current version, lifecycle
status, owner, and last update. Search/filter state is reflected in the URL so
refresh and back/forward navigation are safe. Selecting a row opens the
Workbench for that Definition and the newest version first.

### 5.2 Editor

The editor keeps the two-panel interaction already established by the product
requirements:

- **Formula Variables:** ordered rows/cards with name, type, required flag,
  default value, and description. “+ Thêm biến” opens an inline card or modal;
  duplicate names and invalid identifiers are rejected immediately.
- **KPI Formula:** source text editor with function/operator help and a
  lightweight autocomplete for known variables and supported functions.
- **Diagnostics:** validation errors are grouped by stable code, show the
  affected token/field where available, and prevent Submit until resolved.
- **AST preview:** the server-produced AST is shown after successful parsing.
  The preview is read-only and includes the formula version so a user can see
  when the preview belongs to a newer edit.
- **Test Run:** users enter Decimal/Boolean/Null values for the declared
  variables and receive a transient result or a stable calculation error.
  Test Runs are never written to official Evaluation history.

The editor shows version number, change summary, concurrency token state, and
the version timeline. A version loaded from PostgreSQL must rehydrate the same
source text, ordered variables, and AST semantics.

### 5.3 Workbench actions

The visible primary action is determined by current state and persona:

- Draft: Save Draft, Validate, Test Run, Submit for Review, Clone.
- In Review: show reviewer queue/status; creator cannot silently edit the
  submitted content.
- Rejected: Return to Draft is visible with reviewer comment; the creator can
  edit and resubmit as a new review attempt.
- Approved: Publish is visible only to the permitted publisher/policy persona.
- Published: show effective date, successor/version actions, Archive/Restore
  where allowed, and read-only formula content.
- Retired/Archived: read-only history with Restore only where domain rules
  allow it.

All disabled actions include a short reason (for example, “Cần validation
thành công” or “Chờ KPI Policy Approver”) instead of silently disappearing.

## 6. Slice 2 — Governance journey

The version timeline and action panel make the full lifecycle explicit:

`Draft → In Review → Approved/Rejected → Published → Retired`

Submission shows a confirmation containing the version, change summary, and
the target approver role. Review shows read-only formula/variable/AST content,
approve/reject controls, and a required comment for rejection. Publishing
requires an effective date and explains that an overlapping active version is
not allowed; the UI displays the predecessor/successor relationship after
success.

Clone, archive, restore, ownership transfer, and draft delete appear in an
overflow menu with confirmation and an audit reason field when required. The
UI never offers an edit control for a Published or Retired version.

Concurrency conflicts return the user to the Workbench with a non-destructive
message, the current server version summary, and an explicit reload action.

## 7. Slice 3 — KPI Period and Evaluation

### 7.1 Period Plan

The Period page is a guided plan form:

1. Define name/code, cadence, start/end, timezone, and effective boundary.
2. Select KPI Definitions from a searchable list.
3. Select one eligible KPI Version per Definition, newest first, with the
   version status/effective date visible.
4. Review the frozen selection summary and submit for KPI Period Approver.

The page explains why an ineligible version cannot be selected (not Published,
outside the period, wrong organization, or overlap). After approval, the plan
is read-only except for the governed Amendment path. Activation is shown as a
distinct event and exposes the effective selections that evaluations will use.

### 7.2 Official Evaluation

The Evaluation page is available only for an Active Period Activation. It
shows the selected KPI Version, immutable formula snapshot, ordered inputs,
current result, and failure details. A successful official evaluation becomes
the Current KPI Evaluation; a failure never replaces it. History defaults to
the latest 25 records and offers an explicit “Tính evaluation mới” action.

Correction is a separate path. It is available only when the domain allows a
closed-period correction, requires a reason and replacement input/result
review, and displays a diff between the original and Superseding Evaluation.
The original record remains visible and immutable.

## 8. Slice 4 — Overview and Audit

### 8.1 Overview

The overview is an operational queue, not a future analytics dashboard. It
shows counts and links for Drafts needing work, Versions awaiting review,
Periods awaiting approval/activation, active periods, failed evaluations, and
recent audit activity. Cards link into the same Workbench pages; they do not
create a second state model.

### 8.2 Audit timeline

The Audit page exposes filters already supported by the API/Application seam:
date range, actor, action, entity type, entity id, and result. Each row shows
timestamp, actor/persona, action, entity, reason/change summary, and a link to
the affected version/period/evaluation when available. Empty results explain
which filters were applied. Audit details are read-only.

## 9. Data flow and boundaries

- MVC GET actions load view models through Application query/read seams.
- MVC POST actions validate transport input, invoke the corresponding
  Application operation with the current `ActorContext`, and redirect after a
  successful commit.
- Formula editor enhancements call the existing validation and Test Run API
  endpoints; the server remains authoritative for AST generation and formula
  limits.
- Official evaluation and audit facts are read from the durable runtime store
  when the Postgres profile is selected. InMemory remains an explicit
  Development/test profile, not a fallback for durable user data.
- Views do not walk nested Domain objects to build ad-hoc DTOs. Query models
  provide the display fields needed by a page, including state, next actions,
  diagnostics, and immutable snapshots.

No new domain entity, persistence schema, external connector, or identity
provider is introduced by the UI work. If an existing operation is missing a
required behavior, that gap is recorded in the implementation plan as a
domain/application task rather than hidden in a controller.

## 10. Error, access, and safety behavior

- Use the existing stable API/error codes in form summaries and inline fields.
- Never display raw exception details or connection strings.
- Reject cross-organization records and forbidden persona actions at the
  Application boundary; UI hiding is only a usability aid.
- Require explicit confirmation for publish, archive, restore, ownership
  transfer, period activation, and correction.
- Preserve user input when validation fails. For a stale concurrency token,
  show the server revision and offer reload rather than overwriting it.
- Null input, division by zero, formula limit failures, and unsupported
  functions render as typed failures with the reason; they do not become a
  misleading numeric result.

## 11. Test and verification strategy

Each slice is implemented as a vertical slice with RED → GREEN tests:

- View/controller contract tests assert navigation, labels, next-action
  visibility, validation summary, and redirect targets.
- Application tests continue to prove lifecycle, capability, organization,
  concurrency, evaluation, and audit behavior independently of HTML.
- Formula UI tests cover variable editor, autocomplete, AST preview, Test Run,
  Decimal display, and diagnostics for both success and failure.
- Integration tests verify durable rehydration of formula source, ordered
  variables, AST, version state, periods, evaluations, and audit rows when
  the opt-in PostgreSQL profile is enabled.
- Browser/E2E tests cover one happy-path journey per slice and theme toggling;
  manual evidence remains required for visual quality and the human steps
  called out in `docs/quality.md`.

Every vertical slice ends with `./harness.cmd test`; the complete change ends
with `./harness.cmd check`. No schema migration is hidden in Web startup or
test setup.

## 12. Scope boundaries and success criteria

In scope: the cohesive MVC navigation, KPI Workbench, governed version
journey, Period Plan/Activation UI, official Evaluation/history/correction UI,
operational overview, filterable Audit timeline, light/dark theme, responsive
layout, localization keys, and tests/documentation for those journeys.

Out of scope: production authentication/authorization integration, employee
assignment, nested KPI references, external data connectors, email/Teams
notifications, drag-and-drop formula authoring, a full analytics dashboard,
and SPA migration.

The UI work is successful when a Development persona can complete the complete
Draft → Review → Publish → Period Activate → Official Evaluate → History/
Correction → Audit journey from visible navigation; a blocked action explains
why; a Postgres reload preserves formula semantics and official facts; the same
journey is usable in Light and Dark themes at desktop and narrow widths; and
the slice tests plus the full harness pass without introducing alternate
schema or runtime paths.
