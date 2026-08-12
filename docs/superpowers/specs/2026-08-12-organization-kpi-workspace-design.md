# Organization KPI Workspace UI/UX Design

Date: 2026-08-12

Status: Product-owner approved for implementation planning

Scope: Organization-tree navigation into a one-level KPI neighborhood

## 1. Context and outcome

The BSC-KPI product needs a discoverable way to move from the approved
organization structure to the KPIs owned by a Position. The experience must
retain organization context while showing how a Position's KPIs contribute to
their direct parents and are composed from their direct children. It must also
show the Employee responsibility assignments behind a Position-level KPI
without turning the primary table into one row per Employee.

The chosen experience is an **Organization KPI Workspace**. On desktop, an
organization tree remains visible on the left and the selected Position's KPI
neighborhood is shown on the right. On mobile, Position selection moves into a
drawer and the KPI content uses the full viewport. The selected Position,
period, baseline, effective segment, result mode, and filters are represented
in the URL so refresh and browser navigation preserve context.

This design does not authorize the target repositories for implementation.
Following the approved reference-first workflow, the interaction and contracts
must first be implemented and accepted end-to-end in `Kpis-Thinh`. The
`BSC-KPIs-API` and `BSC-KPIs` repositories remain read-only until the explicit
port gate is released.

## 2. Product decisions

The following decisions are approved:

1. Organization Unit nodes only expand or collapse the tree. They do not open
   a KPI list.
2. Position nodes are selectable and open the KPI neighborhood for that
   Position.
3. Desktop uses a master-detail workspace: organization tree on the left and
   KPI table on the right. Mobile uses a Position-selection drawer and a
   full-screen KPI view.
4. The selected Position is the center of a three-layer view:
   `direct parent KPI -> selected Position KPI -> direct child KPI`.
   Ancestors and descendants more than one KPI relationship away are not
   included.
5. Opening KPI details does not change the selected Position. A separate
   **Đi tới vị trí sở hữu** action changes the organization-tree selection.
6. A Position KPI is one summary row even when several Employees share
   responsibility. Employee assignments appear in an expandable detail.

## 3. Information architecture and navigation

### 3.1 Workspace layout

At desktop widths the page has two persistent regions:

```text
+------------------------+-----------------------------------------------+
| Organization tree      | Selected Position KPI neighborhood            |
|                        |                                               |
| v Company              | Position / Period / Baseline / Segment        |
|   v Division           | Filters                                       |
|     v Department       |                                               |
|       o Manager        | Direct parent KPIs                             |
|       * Specialist     | Selected Position KPIs                         |
|       o Analyst        |   `- Direct child KPIs                         |
|                        |      `- Employee assignments on disclosure     |
+------------------------+-----------------------------------------------+
```

The tree panel is approximately 320-360 pixels wide, sticky while the KPI
region scrolls, and collapsible. At a 390-pixel viewport the tree is opened by
a **Chọn vị trí** control in a drawer. The KPI region then uses the full
viewport.

### 3.2 Tree behavior

Organization Unit and Position nodes must be visually and semantically
distinct. Organization Units expose an expand/collapse control. Positions
expose selection. Selecting a Unit must never be interpreted as selecting all
KPIs in that subtree.

The tree supports search by Organization Unit, Position, or Employee. A search
result reveals the path to a matching Position, but the Position changes only
after explicit user selection. Large structures load children by branch rather
than requiring the whole company graph on first render.

The selected Position is visibly highlighted and encoded in the URL. Browser
refresh, back, forward, and a shared link restore the Position and page
context. When no Position is selected, the detail region displays:
**Chọn một vị trí trên sơ đồ tổ chức để xem KPI**.

### 3.3 Effective context

The context bar identifies:

- KPI period;
- applicable approved Organization Structure Baseline;
- applicable Effective Segment;
- result mode: **Theo segment** or **Tổng hợp toàn kỳ**.

For an open period, the default is the segment effective at the current
instant. For a closed period, the default is the last segment. Whole-period
mode displays only an official result supplied by the backend. The frontend
must not add, average, prorate, or otherwise combine segment outcomes.

When structure or responsibility changes during a period, the context bar
shows a clear boundary notice, for example:

> Cơ cấu và trách nhiệm KPI đã thay đổi từ 01/06/2026. Bạn đang xem Segment 2
> theo baseline phiên bản 4.

Facts from different baselines or effective segments must never be silently
mixed in one displayed result.

## 4. KPI neighborhood table

### 4.1 Relationship model

The Position selected in the tree is the center of the read model. For its
KPIs the table may include:

- direct parent KPIs owned by an immediately related owning Position;
- KPIs owned by the selected Position;
- direct child KPIs owned by immediately related child Positions.

The backend, not the frontend, determines these relationships. The table must
not recursively request grandparents or grandchildren. A KPI returned through
more than one relevant relationship is transferred once with explicit
relationship metadata; the UI may display the applicable relationship badges
without duplicating its underlying record.

Selected-Position KPI rows are visually emphasized. Parent relationships use
the label **Đóng góp vào**. Child relationships use **Được cấu thành bởi** and
one indentation level. Relationship meaning must remain understandable without
color or indentation alone.

### 4.2 Columns

The primary desktop columns are:

| Column | Content |
|---|---|
| Relationship | Parent, selected Position, or direct child |
| KPI | Code, name, version, and lifecycle status |
| Owning Position | Position with primary business responsibility |
| Target | Target for the selected period and segment |
| Actual | Latest applicable official Actual |
| Variance | Absolute variance, percentage variance, and semantic state |
| KPI Score | Official score or an explicit incomplete-data state |
| Assignment | Number of responsible Employees |
| Actions | Details, history, and go to owning Position |

The UI must not expose an ambiguous generic `Weight` column. Where relevant,
detail content distinguishes:

- **KPI Plan Weight**: the KPI's contribution within its approved Plan;
- **Child-to-Parent Contribution Weight**: a child KPI's contribution to a
  composite parent KPI;
- **Employee Responsibility Weight**: the responsibility allocation among
  Employees assigned to the Position KPI.

### 4.3 Employee disclosure

A Position KPI remains one summary row. Its assignment control shows the
number of responsible Employees. Expanding it loads rows containing Employee,
Position Assignment, responsibility weight, Target, Actual, Variance, and
applicable data status.

The frontend does not repair or normalize weights. It displays the approved
Plan revision and the backend validation outcome. Missing values are shown as
missing; they are never converted to zero.

### 4.4 KPI detail interaction

Selecting a KPI name opens a detail panel while preserving the selected
Position, period, segment, filters, and scroll context. Desktop uses a side
panel; mobile uses a detail screen whose back action restores the prior state.

The detail contains KPI definition/version information, owner, formula,
Target, Actual, Variance, score, segment history, correction evidence, and the
authorized audit timeline. It provides a separate **Đi tới vị trí sở hữu**
action. Only that action changes the organization-tree Position.

## 5. Filters and screen states

The workspace supports filters for:

- KPI code or name;
- relationship layer;
- BSC Perspective;
- lifecycle status;
- Actual state: complete, missing, pending approval, or corrected;
- Variance band: achieved, warning, or not achieved;
- responsible Employee.

Position, period, segment, result mode, and filters are all URL state. Filters
apply to the KPI region without changing the organization-tree selection.

Required states are:

- **Loading**: stable skeletons for the tree and KPI region;
- **No Position selected**: instructional state;
- **Position has no KPI**: explicit empty state and, when authorized, a link to
  the appropriate KPI Plan operation;
- **No filter results**: retained filters and a **Xóa bộ lọc** action;
- **No official result**: explicit incomplete status, never a synthetic zero;
- **Out of scope**: an authorization-aware explanation rather than a false
  empty-data state;
- **Context conflict**: the baseline or segment changed, with a non-destructive
  reload action;
- **API failure**: retry that preserves URL state plus a stable error code and
  correlation ID where available.

Status colors always have text or icon reinforcement. Missing data, pending
approval, correction, and adverse Variance are separate states.

## 6. Component and service boundaries

The production target remains .NET 9 ASP.NET Core MVC/Razor. No new JavaScript
framework is required. Small progressive-enhancement scripts may manage a
drawer, side panel, focus, or branch expansion, but the core navigation and
governed operations remain usable through server-rendered C# flows.

The UI is decomposed into focused components:

1. `OrganizationTreeNavigator` renders authorized hierarchy nodes and Position
   selection from a supplied read model.
2. `KpiContextBar` renders period, baseline, segment, and result mode.
3. `KpiNeighborhoodTable` renders the one-level relationship read model.
4. `KpiAssignmentDisclosure` renders Employee responsibility details.
5. `KpiDetailPanel` renders KPI details without changing tree context.

None of these components owns authorization, relationship traversal,
calculation, or aggregation policy.

The target data flow is:

```text
Browser
  -> BSC-KPIs MVC/Razor and typed API client
  -> BSC-KPIs-API authorization/application query
  -> domain and persistence ports
  -> PostgreSQL
```

`BSC-KPIs` must never access the database. It consumes API read models and
renders presentation behavior. `BSC-KPIs-API` applies capability plus KPI Data
Scope, resolves effective baselines and segments, traverses KPI relationships,
and supplies authoritative Target, Actual, Variance, score, and allowed-action
projections. Every mutation is authorized again by the backend even when the
UI hides or disables an action.

## 7. Proposed read contracts

The exact transport shape is finalized during planning, but the UI requires
coarse-grained page contracts rather than one request per row:

```http
GET /api/v1/organization-tree
    ?baselineId=...
    &effectiveAt=...
    &parentUnitId=...

GET /api/v1/positions/{positionId}/kpi-neighborhood
    ?periodId=...
    &segmentId=...
    &resultMode=segment|wholePeriod

GET /api/v1/kpis/{kpiId}/details
    ?periodId=...
    &segmentId=...

GET /api/v1/kpis/{kpiId}/assignments
    ?periodId=...
    &segmentId=...
```

The KPI-neighborhood response includes the selected Position, applicable
baseline and segment, selected-Position KPIs, direct relationship edges,
parent and child KPI summaries, official metric values and states, assignment
counts, and backend-authorized actions. It must be possible to verify from the
response that every returned relationship is exactly one edge from a selected-
Position KPI.

Stable errors distinguish missing capability, out-of-scope data, missing
approved baseline, unavailable segment, stale context, and ordinary not-found
without leaking protected organization facts.

## 8. Visual, responsive, and accessibility behavior

The visual language follows the existing Vietnamese-first, light/dark capable
MVC shell. KPI and Position identity use stable text labels; semantic badges
show lifecycle, data, and Variance states. A Variance display contains the
absolute value, percentage, and label, for example `-120 triệu`, `-8,4%`, and
`Không đạt`.

At narrow widths the KPI table becomes relationship-aware cards. Each card
retains KPI identity, owner, Target, Actual, Variance, score, status, and
actions. Parent/selected/child meaning remains explicit through labels rather
than spacing alone. Filters open in a bottom sheet or modal.

The tree follows accessible tree interaction: arrow keys navigate and expand,
and Enter selects a Position. Focus remains stable when the KPI region reloads.
Drawers and side panels trap focus while open and restore focus to their
trigger when closed. All journeys must remain operable by keyboard at a
390-pixel viewport, and every new label receives a stable localization key.

## 9. Error and concurrency behavior

The UI preserves submitted or selected state across validation and transport
failures. If a baseline, segment, or Plan revision changes while a user is
viewing the workspace, the backend returns a stable context conflict. The UI
shows the current server context and offers reload; it does not merge old and
new result facts.

An API timeout offers a retry without dropping Position or filters. Raw
exceptions, connection details, and unauthorized entity identifiers are never
rendered. Correlation IDs support troubleshooting without replacing a useful
business explanation.

## 10. Verification strategy

The reference implementation requires tests for:

1. Unit nodes only expand/collapse, while Position selection loads KPIs and
   updates restorable URL state.
2. The KPI neighborhood contains only selected-Position KPIs plus direct
   parents and direct children; no second-degree relationship is returned.
3. Opening and closing KPI details preserves Position, period, segment,
   filters, and focus; **Đi tới vị trí sở hữu** changes Position explicitly.
4. A multi-Employee KPI remains one summary row and expands to the correct
   Employee, Position Assignment, responsibility weight, Target, Actual, and
   Variance records.
5. Mid-period baseline changes produce distinct segment views and never mix
   assignments or official results across the effective boundary.
6. Capability and KPI Data Scope filter tree nodes, KPI data, detail fields,
   timelines, and action projections; direct URLs and mutations are
   independently reauthorized.
7. Missing Actual remains missing, timeout retry preserves state, and stale
   context produces a reload flow rather than an overwrite.
8. The primary journey is keyboard-operable in light and dark themes at
   desktop and 390-pixel viewport widths.
9. PostgreSQL integration evidence proves the displayed baseline, Position,
   relationship, assignment, Target, Actual, Variance, and segment facts are
   durable rather than UI fixtures.

The accepted end-to-end journey is:

```text
Select KPI period
  -> load applicable baseline
  -> navigate organization Units
  -> select Position
  -> view direct parent/current/direct child KPIs
  -> expand Employee responsibility
  -> open KPI details
  -> optionally go to owning Position
```

## 11. Delivery boundaries and gates

The Organization and Authorization foundation can deliver the authorized tree,
Position selection, baseline context, and applicable-scope decisions. Complete
KPI-neighborhood data depends on later KPI Planning, Cascade, Actual, and
Evaluation contracts. Those later modules own the real relationship graph,
Plan and responsibility weights, official Target and Actual facts, Variance,
and score.

The reference UI must not use production-looking fixtures to claim those later
flows are complete. The UI may integrate a later contract only when its
backend, persistence, authorization, and acceptance evidence exist.

Porting remains blocked until the product owner has approved the reference
implementation end-to-end across UI/UX, backend behavior, authorization,
PostgreSQL persistence, responsive behavior, and the full repository harness.

## 12. Explicit non-goals

- Selecting an Organization Unit to aggregate every KPI in its subtree.
- Recursively displaying the complete KPI dependency graph in the table.
- Replacing the approved Strategy Map visualization.
- Calculating Target, Actual, Variance, segment aggregation, score, or weights
  in the frontend.
- Adding a SPA framework or requiring JavaScript for core governed actions.
- Porting code into `BSC-KPIs-API` or `BSC-KPIs` before the release gate.
