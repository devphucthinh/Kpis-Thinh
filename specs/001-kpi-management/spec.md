# Feature Specification: Governed KPI Management

**Feature Branch**: `main`

**Feature Directory**: `specs/001-kpi-management`

**Created**: 2026-08-09

**Status**: Approved — Clarified for implementation planning

**Input**: Approved KPI management findings from the completed Grill session, `CONTEXT.md`, and the current repository documentation.

## Product Context

### User Problem

Organizations need to define performance measures whose meaning, formula, inputs, approvals, effective periods, results, and corrections remain explainable over time. Editing a KPI or recalculating a result in place destroys that explanation and makes management decisions difficult to audit. Formula authors also need enough flexibility to express business logic without allowing arbitrary executable code.

### Why This Feature Exists

This feature proves that one company can govern KPIs from authoring through official evaluation while preserving an exact, immutable history. It provides a reviewable foundation for later employee assignment, nested KPIs, external data sources, identity providers, notifications, dashboards, reports, and multi-company administration without including those later capabilities now.

### Intended User Outcomes

- KPI Creators can define understandable, versioned KPIs and test formulas safely before review.
- KPI Policy Approvers can approve or reject proposed versions without altering creator-owned content.
- KPI Period Planners and KPI Period Approvers can govern which exact versions apply during a company period.
- KPI Evaluators can record official results and correct mistakes without erasing earlier attempts.
- KPI Administrators can monitor governed activity and explain the history without editing creator-owned KPI content.
- Reviewers can reproduce any historical outcome from the exact version, formula, ordered inputs, and evaluation record that produced it.

## Clarifications

### Session 2026-08-09

- Q: Khi một KPI Period đã được duyệt cần sửa, Amendment được duyệt phải có hiệu lực thế nào trong MVP? → A: Chỉ KPI Period ở trạng thái Scheduled được sửa bằng Amendment; Amendment được duyệt tạo một effective revision bất biến mới, plan gốc và lịch sử vẫn nguyên vẹn, và activation dùng revision mới nhất đã được duyệt.
- Q: Khi KPI Period bị Approver từ chối, lifecycle phải quay về Draft theo cách nào? → A: Rejection chuyển KPI Period từ In Review sang Rejected và giữ comment/Audit; chỉ KPI Period Planner mới được đưa Rejected period về Draft để sửa và gửi lại.
- Q: Trong MVP, capability và separation-of-duty có bắt buộc trên mọi governed operation hay chỉ được mô phỏng trên UI? → A: Mọi governed operation bắt buộc kiểm tra capability và separation-of-duty; chỉ production authentication, session, identity-provider integration, và deployment policy adapter nằm ngoài scope.
- Q: Khi một Formula Variable tùy chọn được công thức tham chiếu nhưng không có input và cũng không có default thì xử lý thế nào? → A: Chỉ được bỏ qua biến tùy chọn khi công thức không tham chiếu đến nó; nếu công thức có tham chiếu mà thiếu giá trị thì trả lỗi thiếu input ổn định, không biến thành Null và không tạo kết quả thành công.

## Scope & Boundaries

### In Scope

- One seeded company, while retaining company scope on stable identities.
- KPI Definition creation, reading, Draft updates, controlled deletion, archive, restore, and ownership transfer.
- Traceable KPI Versions with review, approval, publication, effective dates, retirement, cloning, change summaries, and diffs.
- Dynamic ordered Formula Variables and manual test or official Evaluation Inputs.
- Deterministic Decimal and Boolean formulas using the approved arithmetic, comparison, logical, conditional, percentage, rounding, absolute-value, and remainder operations.
- Monthly, quarterly, and annual KPI Period planning, approval, scheduling, activation, closure, cancellation, and governed Amendment of Scheduled periods through immutable effective revisions.
- Formula Test Runs that are discarded and official KPI Evaluations that are immutable.
- Corrective Superseding Evaluations with changed-input and changed-result explanations.
- Append-only Audit Records for governed actions and state changes.
- Development-only role personas sufficient to demonstrate the complete workflow without production authentication.
- Vietnamese-first interaction, core English text, machine-readable formula representation, and a human-and-agent operating guide.

### Out of Scope

- Production authentication, session management, identity-provider integration, and deployment-grade identity/policy adapters; governed operations still enforce the approved capabilities and separation of duty independently of the UI.
- Employee assignment, employee-specific KPI tracking, management notifications, or manager reporting.
- Nested KPI references in formulas.
- Importing values from spreadsheets, productivity suites, ERP systems, data warehouses, or external services.
- Email, chat, or other notification delivery.
- Dashboards, reports, charts, or management analytics.
- Drag-and-drop formula construction.
- Fiscal calendars other than the Gregorian calendar.
- Multi-company administration in the user interface.
- Production deployment or cloud-provider configuration.
- Arbitrary code, loops, recursion, or formula access to files, processes, networks, or stored data.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Author and Test a KPI Version (Priority: P1)

A KPI Creator creates a stable KPI Definition, declares an ordered set of typed Formula Variables, writes a formula, sees immediate syntax and type feedback, reviews its structured meaning, and runs temporary examples before asking for approval.

**Why this priority**: A trustworthy definition and formula are the foundation for every later governance and evaluation journey.

**Independent Test**: Create a Draft with Decimal and Boolean variables, validate a formula using arithmetic and conditional logic, run it with manual inputs, and verify the result is shown while no official Evaluation history is created.

**Acceptance Scenarios**:

1. **Given** a new KPI Definition and valid ordered variables, **When** the Creator enters a valid formula, **Then** the formula is accepted, its variables and declared result type agree, and a structured read-only representation is shown.
2. **Given** a valid Draft formula, **When** the Creator performs a Formula Test Run with manual inputs, **Then** the outcome is displayed and discarded without creating an official KPI Evaluation or Audit Record for an evaluation.
3. **Given** a formula with an invalid token, missing variable, type mismatch, invalid rounding scale, or division by zero, **When** it is validated or tested, **Then** the Creator receives a stable failure reason and the relevant source location where applicable.
4. **Given** a Draft saved with deliberate spacing and variable order, **When** it is reopened, **Then** the source text and ordered variable definitions appear exactly as saved.

---

### User Story 2 - Review, Publish, and Trace KPI Versions (Priority: P2)

A KPI Creator submits a completed version for policy review. A KPI Policy Approver approves or rejects it with a comment but cannot edit its content. An approved version can be published from a stated effective date, while every earlier version and change explanation remains traceable.

**Why this priority**: Organizational use requires a non-bypassable approval decision and immutable published meaning.

**Independent Test**: Submit a Draft, reject it once, revise and resubmit it, approve it as a separate persona, publish it with a future effective date, and inspect the complete version timeline and diff.

**Acceptance Scenarios**:

1. **Given** a valid Draft, **When** the Creator submits it, **Then** its content becomes read-only and awaits a policy decision.
2. **Given** a submitted version, **When** the Policy Approver rejects it with a comment, **Then** the rejection and comment are preserved and the Creator can return the content to Draft for revision.
3. **Given** an approved version, **When** it is published with an effective date, **Then** it becomes eligible only from that date and cannot be edited.
4. **Given** a currently effective version and an approved successor, **When** the successor reaches its effective date, **Then** the predecessor is retired, the successor becomes current, and their effective ranges do not overlap.

---

### User Story 3 - Plan and Govern a KPI Period (Priority: P3)

A KPI Period Planner defines a company-calendar interval and selects one eligible exact KPI Version for each included KPI Definition. A separate KPI Period Approver approves or rejects the plan without editing it. An approved plan waits until its start time, becomes active, and closes at its end time.

**Why this priority**: A published KPI does not become an official measurement until an authorized period selects its exact version.

**Independent Test**: Plan a period, select eligible versions, submit it as the Planner, approve it as a different persona, reconcile the start and end times, and verify the frozen selections and complete state history.

**Acceptance Scenarios**:

1. **Given** eligible published versions, **When** the Planner builds a Draft KPI Period Plan, **Then** each KPI Definition can appear at most once and only with a matching cadence.
2. **Given** a submitted plan, **When** the same person attempts to approve it, **Then** approval is rejected without changing the plan.
3. **Given** a submitted plan, **When** a separate KPI Period Approver rejects it with a comment, **Then** the period becomes Rejected without changing its content; only its Planner may return it to Draft for revision and resubmission.
4. **Given** a separate approver and valid non-overlapping dates, **When** the plan is approved, **Then** dates and selections are frozen and the period becomes Scheduled.
5. **Given** a Scheduled or Active period and time has advanced past a boundary during downtime, **When** time reconciliation next runs, **Then** each due transition occurs once and the same reconciliation can be repeated without duplicate transitions.
6. **Given** a Scheduled period that needs change before activation, **When** the Planner proposes an Amendment and a separate KPI Period Approver approves it, **Then** an immutable effective revision is created, the original approved plan remains unchanged, and activation uses the latest approved revision.

---

### User Story 4 - Record and Correct Official Evaluations (Priority: P4)

A KPI Evaluator supplies manual inputs for an Active KPI Period Activation and records an immutable KPI Evaluation. If an input was wrong, the Evaluator creates a Superseding Evaluation with a reason; both attempts and their differences remain visible.

**Why this priority**: Management must be able to trust the current result without losing the evidence behind an earlier or failed attempt.

**Independent Test**: Evaluate an active KPI to obtain 25, correct one input to obtain 30, trigger a later failed attempt, and verify that 30 remains Current while every attempt and correction difference is preserved.

**Acceptance Scenarios**:

1. **Given** an Active activation and all required inputs or defaults, **When** the Evaluator runs an official evaluation, **Then** the exact version, formula meaning, ordered inputs, outcome, actor, and time are preserved as one immutable attempt.
2. **Given** a successful evaluation, **When** the Evaluator corrects it using the same version and provides a reason, **Then** a new immutable attempt is created with old/new input differences and old/new result differences.
3. **Given** a Current successful evaluation, **When** a later official attempt fails, **Then** the failure remains visible but does not replace the Current successful result.
4. **Given** a Scheduled, Closed, Cancelled, or otherwise inactive activation, **When** an ordinary official evaluation is attempted, **Then** it is rejected with an explanatory reason.

---

### User Story 5 - Audit and Manage KPI History (Priority: P5)

A KPI Administrator or responsible user inspects who changed or governed a KPI, archives historical content rather than destroying it, restores a Definition when appropriate, and transfers ownership with authorization and a reason.

**Why this priority**: Governance is useful only when decisions and corrective actions can be explained without giving administrators silent editing power.

**Independent Test**: Inspect a timeline across definition, version, period, evaluation correction, archive, restore, and ownership transfer; verify actor, time, reason, and change context remain visible and immutable.

**Acceptance Scenarios**:

1. **Given** governed actions have occurred, **When** the Administrator filters the Audit history, **Then** records are ordered and show actor, action, affected entity, time, reason when required, and a concise change explanation.
2. **Given** a Definition whose only version is an unused never-submitted Draft, **When** an authorized user hard-deletes it with a reason, **Then** its mutable content is removed and an immutable audit tombstone remains.
3. **Given** any Definition or version with governed history, **When** deletion is attempted, **Then** hard deletion is rejected and archive is the available reversible action.
4. **Given** an Archived KPI Definition, **When** it is restored, **Then** its history returns to view without automatically publishing a version or activating it in a period.
5. **Given** a Creator has left the company, **When** a KPI Policy Approver transfers ownership to another KPI Creator with a reason, **Then** accountability changes and the transfer is audited without an administrator editing KPI content.

### Edge Cases

- KPI Codes are unique within the company regardless of letter case and remain immutable after creation.
- Formula Variable codes are case-insensitive canonical `snake_case`; duplicate or case-conflicting codes are rejected.
- A required Formula Variable without an explicit Evaluation Input or compatible default prevents evaluation. An optional Formula Variable may be omitted only when the formula does not reference it; if the formula references it without an explicit input or compatible default, evaluation returns a stable missing-input Failure. Null is never accepted as an input or successful result.
- A default value with the wrong declared type is rejected before a formula can be used.
- Percentage means division by 100, while `MOD` means remainder; they are never treated as synonyms.
- `IF`, `AND`, and `OR` evaluate only required branches, so an error in an unselected branch does not fail the outcome.
- Division by zero, numeric overflow, invalid scale, syntax errors, type errors, missing inputs, and formula limit violations produce Failure rather than a fabricated value.
- Formulas exceeding 100 variables, 10,000 source characters, expression depth 32, 10,000 evaluated expression elements, or 500 milliseconds are rejected or stopped with an explanatory Failure.
- Concurrent edits using stale state never overwrite a newer Draft or plan; the stale action is rejected and the user must reload.
- A KPI Version cannot be published before approval, and no role can bypass review.
- UI persona selection never grants authority by itself; every governed action rejects missing or conflicting capabilities before changing business state or Audit history.
- An approver cannot edit submitted content while deciding it.
- Published or Retired content remains immutable; reuse starts by cloning it into a new Draft with a Change Summary.
- Effective ranges for versions of the same KPI Definition cannot overlap.
- Ineligible, Retired, cadence-mismatched, or insufficiently effective versions whose effective range does not cover the planned activation cannot be selected for a KPI Period; a future version may be planned in advance when it will be effective by the period start.
- Same-cadence KPI Periods cannot overlap, and the same KPI Definition cannot be active in overlapping periods.
- Exact start and end boundaries are applied once: start makes a Scheduled period Active; end makes an Active period Closed.
- Repeated or delayed time reconciliation cannot duplicate state transitions or Audit Records.
- Only a Scheduled KPI Period may receive an Amendment in the MVP; Draft or In Review content is edited through normal planning, while Active, Closed, or Cancelled periods reject Amendment proposals without changing plan or Audit state.
- An approved Amendment creates a new immutable effective revision for later activation; it never overwrites the original approved plan or any earlier Amendment revision.
- A correction must use the same KPI Version as the corrected evaluation and must include a reason.
- An unsuccessful correction attempt remains history and cannot displace the last Current successful evaluation.
- Closing a KPI Period blocks new ordinary evaluations but does not erase or prevent a governed correction of an existing successful evaluation.
- Cancelling a period preserves its plan, decisions, selections, and audit history.
- A Rejected KPI Period remains read-only until its KPI Period Planner explicitly returns it to Draft; rejection comment, actor, time, and Audit Record remain historical after revision and resubmission.

## Requirements *(mandatory)*

### Domain Terminology

- **KPI Definition** is the stable identity of a measure across revisions and is identified by an immutable company-scoped **KPI Code**.
- **KPI Version** is a traceable revision containing its name, description, Change Summary, Formula Variables, KPI Formula, result type, cadence, and lifecycle history.
- **KPI Formula** is a deterministic expression over declared **Formula Variables**; it is not executable user code.
- **Formula Test Run** is a transient Draft evaluation and is not a **KPI Evaluation**.
- **KPI Evaluation** is an immutable official attempt for one **KPI Period Activation** with exact Evaluation Inputs and an Evaluation Outcome.
- **Current KPI Evaluation** is the latest successful attempt in a correction chain, not merely the latest attempt.
- **Superseding Evaluation** is a corrective immutable evaluation that preserves the earlier attempt and explains the differences.
- **KPI Period Plan** is the proposed interval and exact version selection prepared by a **KPI Period Planner** and decided by a separate **KPI Period Approver**.
- **Published KPI Version** is approved and eligible from its effective date; a **Retired KPI Version** remains historical but is not eligible for new use.
- **Audit Record** is an immutable explanation of a governed action and is distinct from ordinary operational logging.

### Functional Requirements

- **FR-001**: The system MUST scope every KPI Definition, KPI Period, KPI Evaluation, actor, and Audit Record to a company while initially presenting one seeded company.
- **FR-002**: A KPI Creator MUST be able to create a KPI Definition with a unique immutable KPI Code, understandable name, description, and accountable owner.
- **FR-003**: The system MUST preserve one stable KPI Definition identity across all of its KPI Versions.
- **FR-004**: A KPI Version MUST preserve its sequential version number, name, description, Change Summary, optional predecessor, ordered Formula Variables, KPI Formula, declared result type, cadence, effective range, and lifecycle history.
- **FR-005**: Only Draft KPI Version content MAY be edited; changing content that has been submitted or used MUST create a new traceable version.
- **FR-006**: A new version after the first MUST include a Change Summary and MUST support a human-readable comparison with its predecessor.
- **FR-007**: KPI Version lifecycle MUST follow Draft → In Review → Approved → Published → Retired, with rejection returning content to a revisable Draft through an audited decision.
- **FR-008**: A KPI Policy Approver MUST be able to approve or reject a submitted KPI Version with a comment but MUST NOT edit its content.
- **FR-009**: A KPI Version MUST NOT be published unless approved, and no actor or role MAY bypass review.
- **FR-010**: Publication MUST assign an effective-from date and MUST prevent overlapping effective ranges for versions of the same KPI Definition.
- **FR-011**: At most one Published KPI Version per KPI Definition MAY be currently effective; when a successor becomes effective, its predecessor MUST become Retired exactly once.
- **FR-012**: A Retired KPI Version MUST remain visible and immutable but MUST NOT be eligible for a new KPI Period; reusing its behavior MUST create a new Draft clone.
- **FR-013**: A KPI Definition whose only version is an unused, never-submitted Draft, or an unused never-submitted Draft Version, MAY be hard-deleted only with a reason and an Audit tombstone.
- **FR-014**: A KPI Definition with governed history MUST only be archived and restored; restoration MUST NOT publish or reactivate content automatically.
- **FR-015**: A KPI Policy Approver MUST be able to transfer KPI ownership to another KPI Creator with a reason and Audit Record; KPI Administrators MUST NOT silently edit creator-owned content.
- **FR-016**: A Formula Variable MUST declare a canonical code, display name, description, Decimal or Boolean type, required flag, compatible optional non-null default, and display order.
- **FR-017**: A KPI Version MUST support between 0 and 100 dynamically added Formula Variables and MUST preserve their display order after saving and reloading.
- **FR-018**: Evaluation MUST begin only when every required Formula Variable has an explicit non-null input or a compatible non-null default, and every optional Formula Variable referenced by the formula has an explicit non-null input or a compatible non-null default; an unreferenced optional variable MAY be omitted without creating Null.
- **FR-019**: A KPI Formula MUST declare a Decimal or Boolean result type and MUST be rejected when its inferred result type does not match.
- **FR-020**: The formula language MUST support Decimal and Boolean literals, variables, parentheses, comparisons (`=`, `!=`, `>`, `>=`, `<`, `<=`), `AND`, `OR`, `NOT`, `IF`, `+`, `-`, `*`, `/`, unary minus, postfix `%`, `ROUND`, `ABS`, and `MOD`.
- **FR-021**: Formula keywords and function names MUST be case-insensitive English terms in every supported display language.
- **FR-022**: Formula precedence MUST be, from highest to lowest: grouping/values, postfix percentage, unary operations, multiplication/division, addition/subtraction, comparison, `AND`, then `OR`.
- **FR-023**: `IF`, `AND`, and `OR` MUST short-circuit so that an unselected expression cannot create an Evaluation Failure.
- **FR-024**: Percentage MUST mean division by 100; `MOD` MUST mean remainder; `ROUND` MUST round midpoint values away from zero.
- **FR-025**: Decimal calculations MUST preserve up to 28 significant digits and 10 fractional digits without binary floating-point approximation.
- **FR-026**: Formula validation and evaluation MUST return stable explanatory Failures for invalid syntax, missing variables, incompatible defaults, type mismatches, division by zero, numeric overflow, invalid scale, and exceeded limits.
- **FR-027**: Formula execution MUST NOT allow arbitrary code, loops, recursion, or access to files, processes, networks, or stored data.
- **FR-028**: The system MUST enforce formula limits of 100 variables, 10,000 source characters, expression depth 32, 10,000 evaluated expression elements, and 500 milliseconds per run.
- **FR-029**: A Creator MUST be able to see formula syntax guidance, insert declared variables or supported functions, receive immediate validation feedback with source location, and inspect a generated structured representation before submission.
- **FR-030**: A Formula Test Run MUST use the same formula meaning and calculation rules as an official evaluation but MUST NOT persist an Evaluation, change Current results, or create official evaluation history.
- **FR-031**: Saving and reloading a KPI Version MUST preserve formula source text exactly, preserve ordered Formula Variables, and reproduce the same formula meaning and version metadata.
- **FR-032**: A KPI Period Planner MUST be able to create a KPI Period Plan with code, name, description, cadence, start, end, and exact selected KPI Versions.
- **FR-033**: KPI Period lifecycle MUST support Draft → In Review → Scheduled → Active → Closed, In Review → Rejected → Draft, and cancellation from Draft, In Review, or Scheduled; rejection MUST preserve the Approver's comment and Audit Record, and only the KPI Period Planner MAY return a Rejected period to Draft for revision and resubmission.
- **FR-034**: The person who submits a KPI Period Plan MUST NOT approve that same plan; the approver may approve or reject with a comment but MUST NOT edit it.
- **FR-035**: Each KPI Definition MUST appear at most once in a KPI Period, and each selected KPI Version cadence MUST match the period cadence.
- **FR-036**: Eligible versions MUST be presented newest to oldest, with ineligible choices unavailable and accompanied by a reason.
- **FR-037**: Same-cadence KPI Periods MUST NOT overlap, and the same KPI Definition MUST NOT be active in overlapping periods.
- **FR-038**: Approval MUST freeze a KPI Period's dates and selected versions; only a Scheduled period MAY be changed through a separately reviewed KPI Period Amendment, whose approval MUST create a new immutable effective revision used by activation without overwriting the original approved plan or earlier revisions.
- **FR-039**: Reaching a Scheduled period's start MUST activate all selections atomically; reaching an Active period's end MUST close the period.
- **FR-040**: Time reconciliation MUST be idempotent, MUST catch up after downtime, and MUST create exactly one transition and Audit Record for each actual due change.
- **FR-041**: An ordinary official KPI Evaluation MUST be allowed only for a KPI Period Activation in an Active KPI Period.
- **FR-042**: An official KPI Evaluation MUST preserve the exact KPI Version, exact formula source and structured meaning, ordered Evaluation Input snapshot after defaults, Evaluation Outcome, actor, and evaluation time.
- **FR-043**: Evaluation Outcome MUST be either a successful Decimal or Boolean value or a structured Failure; Null MUST NOT be a successful result.
- **FR-044**: Every official evaluation attempt MUST be immutable and remain visible in chronological history.
- **FR-045**: Only a successful attempt MAY become Current; a later Failure MUST NOT replace the latest Current successful evaluation.
- **FR-046**: A correction MUST create a Superseding Evaluation using the same KPI Version and a complete new input snapshot, and MUST require a correction reason.
- **FR-047**: A correction MUST show literal old/new values for each changed input and old/new result values while preserving all prior attempts.
- **FR-048**: Audit Records MUST be append-only and MUST cover definition creation and Draft edits, review and publication decisions, retirement, archive/restore, ownership transfer, period planning and transitions, amendments, and evaluation correction relationships.
- **FR-049**: Audit history MUST support filtering by affected entity, actor, event type, and date and MUST expose actor, time, reason when required, correlation, and concise change context.
- **FR-050**: The first release MUST provide demonstrable personas for KPI Creator, KPI Policy Approver, KPI Period Planner, KPI Period Approver, KPI Evaluator, and KPI Administrator without representing them as production authentication; every governed operation MUST independently enforce the applicable capability and separation-of-duty rule before changing business state or Audit history.
- **FR-051**: KPI Administrators MUST be able to monitor Definitions, Versions, Periods, Evaluations, and Audit history but MUST NOT edit creator-owned KPI content.
- **FR-052**: Concurrent changes based on stale state MUST be rejected without overwriting a newer Draft or KPI Period Plan.
- **FR-053**: User-facing behavior and failure explanations MUST be available primarily in Vietnamese, with core English text available; canonical codes and formula terms MUST remain English.
- **FR-054**: The complete included workflow MUST be operable through a local interactive user interface with accessible labels, lifecycle status, confirmations, formula guidance, history, and failure states.
- **FR-055**: The system MUST provide a machine-readable read representation that includes exact formula source, generated structured meaning, formula-language version, structured-representation version, and exact Decimal text; submitted structured meaning MUST NOT be trusted over source.
- **FR-056**: A Vietnamese operating guide MUST enable a human or another coding agent to run, understand, verify, and later extract the feature without relying on chat history.
- **FR-057**: Stable company, Definition, Version, Period Activation, and Evaluation identities and immutable histories MUST remain suitable for future employee assignment, nested KPI references, external inputs, provider-neutral notifications, and management reporting without implementing those features now.
- **FR-058**: A KPI Version already selected by a Scheduled or Active KPI Period MUST NOT be invalidated in a way that breaks that period's frozen plan or evaluation history.
- **FR-059**: Closing a KPI Period MUST block new ordinary evaluations but MUST still permit a governed correction of an existing successful evaluation using the same KPI Version, complete new inputs, and a mandatory reason.

### Key Entities

- **Company**: The initial organizational scope for actors, KPI Definitions, KPI Periods, KPI Evaluations, and Audit Records.
- **Actor**: A demonstrable user identity with one of the agreed KPI capabilities; production identity integration is outside this release.
- **KPI Definition**: Stable company-scoped KPI identity with immutable KPI Code, current owner, and archive state.
- **KPI Version**: A traceable content revision belonging to one KPI Definition, optionally succeeding another version.
- **Formula Variable**: An ordered, named, typed input definition owned by one KPI Version.
- **KPI Formula**: Exact authored source plus generated versioned structured meaning used to calculate the declared outcome.
- **KPI Period**: A governed Gregorian interval with cadence, planner, separate approver, and lifecycle state.
- **KPI Period Activation**: The exact selection of one KPI Version of a KPI Definition within one KPI Period.
- **KPI Evaluation**: An immutable official attempt for one activation, optionally superseding an earlier attempt.
- **Evaluation Outcome**: Either a successful Decimal/Boolean value or a structured Failure.
- **KPI Period Amendment**: A separately reviewed proposal for a Scheduled period whose approval creates a new immutable effective revision for activation without overwriting the original approved plan or earlier revisions.
- **Audit Record**: An immutable governed-action record connected to its company, actor, entity, time, reason, and change context.

### Acceptance Criteria

- **AC-001**: A user can complete Definition creation, Draft formula authoring/testing, version review/publication, period planning/approval, activation, official evaluation, correction, audit inspection, archive, and restore through the included interactive experience.
- **AC-002**: Dynamically added Formula Variables retain exact identity, type, defaults, descriptions, and order after saving, closing, and reopening a KPI Version.
- **AC-003**: Every approved formula construct validates and evaluates deterministically, and no formula can execute arbitrary behavior outside the approved language.
- **AC-004**: After durable save and reload, formula source, generated structured meaning, version metadata, ordered inputs, successful result or Failure, and governed history are semantically unchanged.
- **AC-005**: Repeated Formula Test Runs create zero official Evaluation attempts and never change Current results.
- **AC-006**: KPI Versions, KPI Periods, and KPI Period Amendments accept every allowed lifecycle or revision transition and reject every forbidden, state-conflicting, or role-conflicting transition without partial change.
- **AC-007**: A due successor version retires its predecessor exactly once, and due KPI Period start/end transitions occur exactly once even after downtime.
- **AC-008**: Correcting an Evaluation preserves the old attempt, new attempt, changed inputs, result difference, reason, exact version, and chronological relationship.
- **AC-009**: A failed attempt after a successful Current KPI Evaluation remains visible and does not replace the Current result.
- **AC-010**: The machine-readable formula view returns exact source, generated structured meaning with version metadata, and exact Decimal text while rejecting client-provided structured meaning as authoritative.
- **AC-011**: All core user journeys and failures can be completed in Vietnamese, and the same core text has an English representation without translating formula keywords or canonical codes.
- **AC-012**: Demonstration personas cannot be mistaken for or enabled as production authentication behavior, and changing UI persona state cannot bypass capability or separation-of-duty enforcement on a governed operation.
- **AC-013**: Eligible unused Draft content can be hard-deleted only with an Audit tombstone; content with governed history can only be archived and restored.
- **AC-014**: A stale concurrent change is rejected and leaves the newer saved state unchanged.
- **AC-015**: Every governed state change can be traced to an immutable Audit Record containing the responsible actor, time, affected entity, and required reason or change context.
- **AC-016**: The operating guide lets a new human or coding agent reproduce the principal workflow and understand the feature boundaries without additional chat context.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A reviewer following the operating guide can complete the full governed demonstration journey in 15 minutes or less without undocumented assistance.
- **SC-002**: For 100 repeated runs of the same valid formula version and identical inputs, 100% produce the same typed outcome.
- **SC-003**: Across at least 50 representative save/reload cases, 100% preserve exact formula source, Formula Variable order, exact Decimal text, structured formula meaning, Evaluation Inputs, and Evaluation Outcome.
- **SC-004**: Across at least 100 Formula Test Runs, zero official KPI Evaluation or Current-result records are created or changed.
- **SC-005**: For formulas within the declared limits, at least 95% of validation attempts provide visible feedback within one second from the user's action.
- **SC-006**: All supported and forbidden KPI Version, KPI Period, archive, restore, ownership-transfer, and correction transitions pass their stated acceptance scenarios with no partial state changes.
- **SC-007**: Replaying reconciliation after every tested boundary or simulated downtime produces zero duplicate state transitions and zero duplicate transition Audit Records.
- **SC-008**: A reviewer can trace any Current KPI Evaluation to its exact KPI Version, ordered inputs, outcome, predecessor/correction relationship, actor, and time in under two minutes.
- **SC-009**: All tested calculation failures return a stable reason and never return Null or a fabricated successful value.
- **SC-010**: At least 90% of representative first-time users can complete Draft variable creation, formula validation, and Formula Test Run on their first attempt using only the on-screen syntax guidance.
- **SC-011**: Core journeys have 100% Vietnamese and English text coverage, with zero translated formula keywords or canonical identifier codes.
- **SC-012**: The accepted formula limits are demonstrably enforced at 100 variables, 10,000 source characters, expression depth 32, 10,000 evaluated elements, and 500 milliseconds per run.

## Assumptions

- The first release serves one seeded company; company-scoped identities are retained for later multi-company administration.
- Manual input is the only source for Formula Test Runs and official KPI Evaluations in this release.
- An optional Formula Variable that is not referenced by the KPI Formula does not need an Evaluation Input; a referenced optional variable without an input or compatible default produces a stable missing-input Failure rather than Null.
- Company periods use the Gregorian calendar and `Asia/Ho_Chi_Minh` time interpretation with monthly, quarterly, or annual cadence.
- Formula results and Formula Variables use Decimal or Boolean values only; Null represents absence/failure context and is never a valid input or successful result.
- The role personas supply demonstrable actor identities, while governed operations enforce agreed capabilities and separation of duty; production authentication, sessions, identity-provider integration, and deployment policy adapters remain outside this release.
- A KPI Policy Approver and a KPI Period Approver decide submitted content without editing it; a period submitter cannot approve the same plan.
- A rejected KPI Period remains Rejected and read-only until its Planner explicitly returns it to Draft; rejection evidence remains immutable.
- KPI Period Amendments apply only while a period is Scheduled; an approved Amendment becomes a new immutable effective revision for activation and never mutates the original approved plan.
- Historical records are retained indefinitely for the feasibility release unless they meet the explicitly narrow unused-Draft deletion rule.
- External identity, employee assignment, notifications, external data sources, nested KPIs, dashboards, and reporting are future features and do not need placeholder implementations now.
- Repository quality policy and the approved domain glossary remain authoritative throughout later planning and implementation.
