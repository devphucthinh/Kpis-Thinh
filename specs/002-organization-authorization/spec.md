# Feature Specification: Organization and Authorization Foundation

**Feature Branch**: `feature/bsc-kpi-reference-implementation`

**Created**: 2026-08-11

**Status**: Draft

**Input**: User description: "Establish the governed organization structure, workforce positions, approved structure baseline, capability-based authorization, scoped custom roles, independent privilege approval, delegation, and audit foundation required before BSC and KPI operations can begin."

## Clarifications

### Session 2026-08-11

- Q: How must the system determine whether a Role Assignment is privileged and requires independent approval? → A: Use task-oriented atomic business capabilities, comparable to Microsoft 365 Admin Center administration; require independent approval when capability risk is high or the KPI Data Scope exceeds the configured safe threshold.
- Q: Who may configure the capability-risk and Data Scope thresholds that require independent approval? → A: The system defines a mandatory minimum security policy; each Organization may make its policy stricter but cannot weaken the system minimum.
- Q: How should the effective baseline and downstream KPI responsibility weights change when the organization structure changes during a KPI period? → A: Keep exactly one effective baseline at each instant; an approved mid-period baseline supersedes the prior baseline from its effective time, triggers a governed re-cascade amendment, and proportionally rescales existing assignee weights to make room for fixed new-assignee weights while preserving the existing relative order and a total of exactly 100 percent.
- Q: How must an official KPI result combine facts from before and after a mid-period baseline or weight change? → A: Split the period into immutable effective segments, evaluate each segment with its applicable baseline and weights, and combine segment outcomes using the KPI's approved Aggregation Policy; time-based proration is allowed only when that KPI policy explicitly permits it.
- Q: How must rounding residuals be allocated when proportional weight rescaling exceeds the allowed precision? → A: Use the largest-remainder method at the configured precision, distribute residual units by descending fractional remainder, break ties by prior relative order and stable assignment identity, and never reverse the prior assignee weight order.
- Q: Does this foundation feature itself apply KPI Plan amendments and calculate official cross-segment KPI results? → A: No; this feature owns the enforceable approved-baseline gate, immutable change impact, deterministic allocation preview, and effective-segment integration contract, while later KPI Planning and Evaluation features apply governed plan amendments and calculate official results against those contracts.
- Q: Does “exactly one effective baseline at each instant” require continuity? → A: Yes; before the first baseline starts, baseline-dependent operations remain blocked, and from that first effective instant onward approved baselines form a gapless, non-overlapping chain in which successor approval atomically ends predecessor applicability at the successor start.
- Q: How must a new or changed Approval Route version be activated? → A: Every Approval Route version requires independent approval by an eligible actor with the route-approval capability and applicable scope; its creator or editor cannot approve or activate that version, and the system preserves the decision, reason, capability, scope, and timeline evidence.
- Q: How must the Organization Unit Head selector identify the approver? → A: Each Approval Route explicitly selects an Employee as the Organization Unit Head for the relevant unit instead of deriving the head from a Position or reporting-tree rank; route resolution must still verify that the configured Employee is active, belongs to the applicable unit context, and is eligible within the required scope under the applicable approved baseline.
- Q: Which Position must a Direct Manager selector use when an Employee holds multiple Positions? → A: Resolve from the Position attached to the governed artifact's business responsibility or subject; only when the artifact has no Position context may the selector fall back to the Employee's applicable primary Position, and the chosen Position plus any fallback must be preserved in the route snapshot.
- Q: Where must a Named Group selector obtain its group and membership? → A: Use an Organization-scoped internal Approval Group whose Employee memberships are effective-dated; at submission, resolve eligible members and preserve that immutable member set in the Approval Route Snapshot so later membership changes do not rewrite the submitted route.
- Q: What must happen when an administrator retires the only active Approval Route for a governed artifact type? → A: Block retirement until an independently approved replacement version is ready, then atomically activate the replacement and retire the prior route without an unroutable gap; already-submitted artifacts continue using their immutable prior route snapshots.

### Session 2026-08-12

- Q: How does the foundation prove that a Baseline Change Impact was resolved by later KPI Planning? → A: Keep the impact immutable and derive `Resolved` only from a separate immutable Baseline Impact Resolution created as a consequence of the governed KPI Plan Amendment approval; validate the exact Organization, baseline, amendment revision, approval decision, and content hash, commit both outcomes atomically, treat the same retry as idempotent, and reject missing, unapproved, cross-Organization, or conflicting evidence.
- Q: How are the 90-percent first-attempt outcomes in SC-002 and SC-008 measured? → A: Record a standardized human evidence protocol: SC-002 uses at least 10 representative Organization Administrators and passes at 9/10 or better; SC-008 uses at least 20 participants with at least five in each named persona group and passes at 18/20 or better. A first attempt begins after the same orientation but before task-specific assistance, and Playwright evidence cannot substitute for a human attempt.
- Q: When no baseline or Role Assignment exists yet, how does the system authorize the first actors to establish the Organization and approve it independently? → A: Provision two distinct, Organization-scoped Bootstrap Principals: one setup actor and one independent approver. Their product-owned bootstrap grants are non-delegable and fully audited, cannot be held by the same sign-in identity, and expire automatically only after independently approved replacement Role Assignments for both duties become effective.
- Q: May the system reuse an Authorization Decision from an earlier governed action? → A: No. Each governed action reloads and evaluates the current committed authorization facts; only identical evaluations inside that one action execution may be memoized. The next action after an account, employment, Role Assignment, policy, baseline, or delegation change must observe that change, while authorization evaluation after resource facts are loaded must complete within 50 milliseconds p95 under the accepted local load.
- Q: Must replacement Role Assignments exist before the first Organization Structure Baseline is approved? → A: No. The first baseline freezes only structure and workforce facts and contains no Role Assignment. The two Bootstrap Principals submit and independently approve that baseline, then establish governed Role Assignments against the approved baseline; bootstrap authority expires only after effective replacements cover both duties.
- Q: How is bootstrap authority recovered if one Bootstrap Principal loses access before governed replacement assignments are effective? → A: Use a platform-level break-glass replacement requiring approval by two distinct Platform Security Administrators. It may replace only the unavailable principal, requires reason and expiry evidence plus a complete audit trail, and the replacement identity must remain distinct from the other Bootstrap Principal.
- Q: What scale must the first feature pass as a release-blocking performance gate? → A: Use an acceptance envelope of 1,000 Employees and 200 Organization Units. Structure validation must complete within 2 seconds, paged administration and authorized tree reads returning at most 200 nodes must complete within 500 milliseconds p95, and current-fact authorization evaluation must retain its 50-millisecond p95 gate after resource facts are loaded.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Approve an Organization Structure Baseline (Priority: P1)

An Organization Administrator defines the company, its effective-dated
Organization Unit hierarchy, Positions, reporting relationships, Employees,
and Position Assignments. An independent approver reviews the complete
structure and approves an Organization Structure Baseline that downstream
authorization and planning can trust. Role Assignments are governed separately
after the first baseline exists and are not baseline snapshot members.

**Why this priority**: Annual BSC planning, KPI assignment, approval routing,
and cascade depend on an approved and explainable organization snapshot.

**Independent Test**: Create a representative organization with multiple unit
levels, positions, employees, and reporting lines; submit it for independent
review; approve it without a Role Assignment in the snapshot; then verify that
the approved snapshot is immutable, traceable, and recognized as the effective
authorization and planning baseline.

**Acceptance Scenarios**:

1. **Given** a complete valid organization draft, **When** an authorized actor
   submits it and a different eligible actor approves it, **Then** an effective
   Organization Structure Baseline is created with the reviewed revision and
   approval evidence.
2. **Given** an organization tree containing a cycle, **When** an administrator
   validates or submits it, **Then** submission is blocked and the exact unit
   path causing the cycle is explained.
3. **Given** a baseline missing a required primary Position Assignment or
   reporting relationship, **When** it is submitted, **Then** submission is
   blocked and each incomplete item is identified.
4. **Given** an approved baseline, **When** an administrator changes an
   organization fact, **Then** the approved snapshot remains unchanged and the
   change is prepared as a traceable later revision or effective amendment.
5. **Given** a new baseline approved to take effect during an open KPI period,
   **When** its effective time arrives, **Then** it becomes the only applicable
   baseline from that time, the prior baseline remains applicable to earlier
   facts, and affected downstream KPI responsibilities are identified for a
   governed re-cascade amendment.
6. **Given** an open KPI period spans a baseline effective-time boundary,
   **When** downstream results are prepared, **Then** facts before and after the
   boundary remain in separate immutable effective segments and no segment is
   silently recalculated with the other segment's structure or weights.
7. **Given** the first baseline has become effective, **When** a successor is
   submitted with a start that would leave a gap, overlap, or insert before the
   last approved successor, **Then** approval is blocked and the required
   contiguous boundary is explained.
8. **Given** an immutable Baseline Change Impact, **When** downstream Planning
   supplies missing, unapproved, cross-Organization, wrong-baseline, or
   conflicting amendment evidence, **Then** no resolution is created; **When**
   the exact independently approved KPI Plan Amendment revision is registered,
   **Then** one immutable Baseline Impact Resolution is created atomically with
   audit evidence, and retrying that exact evidence returns the same resolution
   without another write.
9. **Given** a newly provisioned Organization with no baseline or Role
   Assignment, **When** its setup Bootstrap Principal submits the complete
   structure and its distinct approver Bootstrap Principal approves the first
   baseline, **Then** that baseline contains no Role Assignment; **When** they
   later establish independently approved Role Assignments against that
   baseline, **Then** every bootstrap action is audited, neither principal may
   approve its own governed submission or delegate bootstrap authority, and
   both bootstrap grants expire only after the two replacement duties are
   effective.
10. **Given** one Bootstrap Principal is unavailable before governed replacement
    assignments cover both duties, **When** two distinct Platform Security
    Administrators approve a reasoned, time-bounded break-glass replacement,
    **Then** only that unavailable principal is replaced, the replacement
    remains distinct from the other principal, and the request, both decisions,
    expiry, and resulting grant are preserved as immutable audit evidence.

---

### User Story 2 - Govern Employees, Positions, and Effective Assignments (Priority: P1)

An Organization Administrator maintains Employees independently from their
sign-in account status and records the Positions they hold over time. One
Employee may hold multiple Positions, while the applicable active assignments
identify exactly one primary Position.

**Why this priority**: Responsibility, manager resolution, data scope, and later
KPI assignment cannot be explained from job-title strings or current-only
department values.

**Independent Test**: Create one Employee with two non-overlapping effective
Position Assignments, select the applicable primary Position, change the
Employee's employment status without changing account status, and verify the
correct historical and current relationships.

**Acceptance Scenarios**:

1. **Given** an active Employee, **When** the administrator records multiple
   valid Position Assignments with exactly one applicable primary Position,
   **Then** all assignments and their effective ranges remain visible and the
   primary Position is unambiguous.
2. **Given** overlapping assignments that would create two primary Positions
   for the same applicable time, **When** the administrator submits the change,
   **Then** the change is rejected with the conflicting assignments identified.
3. **Given** an Employee whose employment ends while the account remains
   enabled, **When** authorization is evaluated, **Then** employment-based KPI
   eligibility ends without silently changing sign-in account status.
4. **Given** an account is disabled while employment remains active, **When**
   the Employee record is reviewed, **Then** employment history remains intact
   and the account cannot perform interactive actions.

---

### User Story 3 - Define Custom Roles from Atomic Capabilities (Priority: P2)

A Security Administrator creates Organization-specific Custom KPI Roles by
selecting from the system's fixed KPI Capability catalog. The administrator can
choose any capability combination, sees warnings for risky combinations, and
understands that changing a used bundle creates a new role version. The
administration experience groups permissions by recognizable business task,
comparable to task-oriented role management in Microsoft 365 Admin Center.

**Why this priority**: The product must support dynamic business roles without
turning role names into hard-coded authorization rules.

**Independent Test**: Create a Custom KPI Role with a risky maker-and-approver
combination, confirm the warning is visible without blocking creation, assign
the role in a limited scope, and verify that runtime separation of duty still
blocks self-approval.

**Acceptance Scenarios**:

1. **Given** the fixed KPI Capability catalog, **When** a Security Administrator
   selects any combination and acknowledges displayed warnings, **Then** a
   Custom KPI Role is created without creating new capability names.
2. **Given** a Custom KPI Role already used by an assignment, **When** its
   capability bundle must change, **Then** a new role version is created and
   existing assignments continue to reference the original bundle.
3. **Given** an actor who can create roles, **When** the actor saves a role,
   **Then** the actor gains no capability unless a separate governed assignment
   is approved and becomes effective.
4. **Given** two roles with the same display name in different Organizations,
   **When** they are viewed, **Then** their Organization ownership, capability
   bundles, and assignments remain distinct.
5. **Given** a Security Administrator is composing a role, **When** the
   capability catalog is displayed, **Then** permissions are presented as
   business tasks with their risk and scope impact rather than as pages, menu
   items, or role-name checks.

---

### User Story 4 - Assign Privilege within an Explicit Data Scope (Priority: P2)

A Security Administrator proposes an effective-dated Role Assignment for an
Employee, role version, Organization, and KPI Data Scope. Privileged assignment
or elevation becomes effective only after an independent eligible approver
accepts it with a reason.

**Why this priority**: Capability without a data boundary creates excessive
access, while self-approved elevation would defeat the governance model.

**Independent Test**: Propose one UnitSubtree assignment, attempt self-approval,
observe rejection, obtain independent approval, and verify that the assignee can
act inside the subtree but is denied outside it.

**Acceptance Scenarios**:

1. **Given** a proposed privileged Role Assignment, **When** its requester or
   beneficiary attempts to approve it, **Then** approval is rejected and the
   separation-of-duty reason is recorded.
2. **Given** an independently approved UnitSubtree assignment, **When** the
   assignee views or acts on an in-scope resource with the required capability,
   **Then** the action is allowed.
3. **Given** that same assignment, **When** the assignee attempts the action on
   a resource outside the subtree, **Then** the action is denied with an
   understandable scope explanation and an Audit Record.
4. **Given** an expired assignment, **When** the former assignee attempts a
   governed action, **Then** the expired assignment grants no authority even
   though its historical approval remains visible.
5. **Given** an assignment whose business-task capabilities are low risk but
   whose requested KPI Data Scope exceeds the configured safe threshold,
   **When** the assignment is submitted, **Then** independent approval is
   required before it becomes effective.
6. **Given** an Organization Security Administrator changes its approval policy,
   **When** the proposed policy is stricter than the system minimum, **Then** it
   may govern later assignments; **When** it would weaken the system minimum,
   **Then** the change is rejected with the protected rule identified.

---

### User Story 5 - Resolve Approvers, Delegation, and Audit Visibility (Priority: P3)

A process owner configures approval selectors based on the approved organization
structure. A different eligible actor independently approves each route version
before activation. At submission, the system resolves and snapshots the
eligible route. An effective delegate may act within the original authority's
capability, scope, and period, while authorized participants and auditors can
understand the complete decision timeline.

**Why this priority**: Governed KPI operations require explainable approval
routes that survive later organization changes without enabling silent skips or
expanded delegated power.

**Independent Test**: Resolve a direct manager and fallback from an approved
baseline, submit an item, change the live manager, delegate the original
approval within a limited period, and verify the stored route, delegated
identity, decision reason, and scoped timeline visibility.

**Acceptance Scenarios**:

1. **Given** an approved baseline and an approval selector, **When** an artifact
   is submitted, **Then** the resolved approvers, selector evidence, fallback,
   and applicable scope are preserved as the submission's route snapshot.
2. **Given** a later manager or Position change, **When** the submitted route is
   reviewed, **Then** the stored route is unchanged and the later organization
   revision remains separately traceable.
3. **Given** a valid time- and scope-limited delegation, **When** the delegate
   decides an eligible stage, **Then** both the original approver and delegate
   identities, the delegation, reason, and time are recorded.
4. **Given** an expired delegation or a delegate lacking the required scope,
   **When** the delegate attempts approval, **Then** the action is rejected and
   no stage is silently skipped.
5. **Given** an actor outside the involved organization scope and without
   applicable audit authority, **When** the actor requests the timeline,
   **Then** protected decision details are not visible.
6. **Given** a process owner creates or changes an Approval Route version,
   **When** that creator or editor attempts to approve or activate the same
   version, **Then** the action is rejected; **When** a different actor with the
   route-approval capability and applicable scope approves it with a reason,
   **Then** the version may be activated and the complete decision evidence is
   preserved in the timeline.
7. **Given** an Organization Unit Head stage configured with a specific
   Employee for the relevant unit, **When** a governed artifact is submitted,
   **Then** the system resolves that Employee without inferring the head from a
   Position or reporting-tree rank and verifies active employment, applicable
   unit context, and required scope against the approved baseline; an
   ineligible configured Employee causes primary or fallback resolution to fail
   explicitly.
8. **Given** an Employee holds multiple Positions and the submitted artifact
   identifies the Position responsible for that business context, **When** a
   Direct Manager stage is resolved, **Then** the system follows the Direct
   reporting relationship from that Position; **Given** no Position context is
   present, **Then** it may fall back to the Employee's applicable primary
   Position, and the selected Position and fallback evidence are preserved in
   the immutable route snapshot.
9. **Given** a Named Group stage references an internal Approval Group, **When**
   an artifact is submitted, **Then** the system resolves the effective and
   eligible Employee memberships for that Organization and preserves them in
   the immutable route snapshot; later group membership changes affect only
   later submissions.
10. **Given** an Approval Route is the only active route for a governed artifact
    type, **When** an administrator attempts to retire it without an
    independently approved replacement ready for activation, **Then** the
    action is blocked; **When** a replacement is ready, **Then** activation and
    retirement occur atomically with no unroutable interval, while previously
    submitted artifacts keep their original route snapshots.

### Edge Cases

- Moving an Organization Unit beneath one of its descendants is rejected with
  the full proposed cycle path.
- A unit code that collides within the same Organization is rejected while the
  same human-readable name may be allowed when its stable code is distinct.
- Overlapping effective assignments are permitted only when they do not violate
  the single applicable primary-Position rule or allocation constraints.
- An Employee transfer that takes effect after an approved baseline does not
  rewrite that baseline; later planning uses the applicable approved revision.
- A baseline change during an open KPI period preserves the prior baseline and
  weights for earlier effective facts, applies the new baseline from its
  effective time, and marks affected KPI responsibilities for re-cascade rather
  than silently rewriting either segment.
- A time-proportional combination is rejected for a KPI whose approved
  Aggregation Policy does not permit time-based proration; the user is shown the
  applicable aggregation rule instead.
- When proportional rescaling produces more decimal places than permitted,
  residual units are allocated deterministically by largest remainder; equal
  remainders use prior relative order and stable assignment identity, and the
  final allocation cannot reverse the prior weight order.
- A baseline submitted from a stale revision is rejected without overwriting
  the newer draft, and the user is shown which revision changed.
- After the first baseline effective instant, a successor that would leave a
  gap, overlap, or out-of-order chain is rejected; concurrent successor
  approvals serialize on one tail and cannot create branches.
- A risky Custom KPI Role combination produces a warning but remains creatable;
  self-approval and other prohibited same-artifact actions remain blocked at
  runtime.
- Concurrent Custom Role or Approval Route version requests from the same stale
  head reject the later request with the current head and preserve one monotonic
  version lineage.
- Removing a capability from a new role version does not mutate an older role
  version or the history of assignments that used it.
- A scoped assignment whose Organization Unit is later deactivated remains
  historical but grants no authority outside its valid effective context.
- When no eligible approver can be resolved, submission or stage progression is
  blocked with the failed selector and configured fallback explained.
- Delegation cannot expand the original authority's capabilities or KPI Data
  Scope and cannot make a delegate eligible to approve their own artifact.
- The two Bootstrap Principals cannot share a sign-in identity, exchange or
  delegate bootstrap duties, approve their own governed submissions, or expire
  one-sidedly before effective Role Assignments replace both duties.
- A single Platform Security Administrator, either Bootstrap Principal, or a
  replacement identity matching the remaining principal cannot complete a
  bootstrap recovery; a partially approved or expired break-glass request
  changes no bootstrap authority.
- A governed action evaluated immediately after an authorization fact changes
  cannot reuse the prior action's decision; revocation, expiry, account or
  employment changes, policy, baseline, and delegation changes apply from their
  effective instant on the next action.
- Timeline visibility changes with effective capability and scope, while the
  underlying Audit Record remains immutable.
- An enabled sign-in account does not itself prove active employment, a Position
  Assignment, a KPI business responsibility, or authorization.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST keep every organization, workforce, authorization,
  approval, delegation, and audit fact within one explicit Organization
  boundary.
- **FR-002**: The product model MUST support multiple Organizations while the
  first operational release exposes one Organization for administration and
  operation.
- **FR-003**: Authorized administrators MUST be able to maintain a generic,
  effective-dated Organization Unit tree with stable unit codes, business unit
  types, status, and an optional parent.
- **FR-004**: The system MUST reject self-parenting, descendant-parenting, and
  any other organization cycle and MUST identify the complete failing path.
- **FR-005**: Authorized administrators MUST be able to define effective-dated
  Positions and reporting relationships without relying on free-form job-title
  or department strings as governed identity.
- **FR-006**: The system MUST govern Employee employment status independently
  from sign-in account status and preserve both histories.
- **FR-007**: An Employee MUST be able to hold multiple effective Position
  Assignments, with exactly one applicable primary Position whenever the active
  assignment set requires a primary Position.
- **FR-008**: Position Assignment validation MUST identify conflicting
  effective ranges, primary-Position conflicts, and incomplete relationships
  before baseline submission.
- **FR-009**: The system MUST evaluate employment eligibility, account access,
  Position responsibility, capability, and KPI Data Scope as distinct facts.
- **FR-010**: Authorized actors MUST be able to prepare and submit an
  Organization Structure Baseline containing the applicable units, Positions,
  Position Assignments, and reporting lines. Role Assignments MUST remain
  separately governed facts and MUST NOT be members of the first or any later
  baseline snapshot.
- **FR-011**: Baseline submission MUST be blocked until required structure,
  primary Position, and reporting relationships are complete and valid; absence
  of Role Assignments MUST NOT block the first baseline while valid Bootstrap
  Principals provide setup and independent-approval authority.
- **FR-012**: An Organization Structure Baseline MUST require approval by an
  eligible actor who did not submit the same revision.
- **FR-013**: An approved Organization Structure Baseline MUST be an immutable
  effective snapshot with submitter, approver, reason, revision, and effective
  period evidence.
- **FR-014**: Changes after baseline approval MUST create a traceable later
  revision or effective amendment and MUST NOT overwrite the approved snapshot.
- **FR-015**: The foundation MUST provide one authoritative approved-baseline
  gate that returns an allow or stable deny decision for a requested
  Organization and effective instant; later Annual BSC planning, KPI plan
  submission, Position KPI templating, KPI Assignment, approval-route
  resolution, and organization cascade operations MUST consume this gate before
  becoming operational.
- **FR-016**: The approved-baseline gate MUST classify KPI Dictionary authoring
  as baseline-independent while classifying Annual BSC/KPI planning,
  assignment, routing, cascade, and operation as baseline-dependent; this
  feature MUST prove that decision matrix without implementing those later
  business modules.
- **FR-017**: The system MUST publish a fixed catalog of atomic KPI Capabilities
  representing recognizable business tasks, comparable to Microsoft 365 Admin
  Center task-oriented administration, and MUST use capability identifiers,
  rather than role names, pages, or menu items, as authorization units.
- **FR-018**: Authorized Security Administrators MUST be able to create
  Organization-scoped Custom KPI Roles by selecting any combination from the
  fixed KPI Capability catalog grouped by business area and showing each
  capability's risk and scope impact.
- **FR-019**: The role-authoring experience MUST display explicit warnings for
  risky or conflicting capability combinations while allowing the warned bundle
  to be created after acknowledgement.
- **FR-020**: A used Custom KPI Role's capability bundle MUST remain immutable;
  a changed bundle MUST create a separately identifiable role version from the
  current optimistic role head, and stale concurrent version creation MUST be
  rejected without creating an implicit branch.
- **FR-021**: Creating, editing, or viewing a Custom KPI Role MUST NOT itself
  grant any capability represented by that role.
- **FR-022**: Every Role Assignment MUST identify the Employee, exact role
  version, Organization, KPI Data Scope, effective period, requester, approval
  state, and reason.
- **FR-023**: KPI Data Scope MUST support Organization, Organization Unit
  subtree, assigned business responsibility, and self boundaries. UnitSubtree
  scope MUST reference an approved applicable baseline and therefore cannot be
  created against an editable structure draft.
- **FR-024**: A governed action MUST be allowed only when the actor has both the
  required effective KPI Capability and an applicable KPI Data Scope for the
  resource facts.
- **FR-025**: A Role Assignment or role elevation MUST require independent
  approval before becoming effective when any selected business-task capability
  is classified as high risk or the requested KPI Data Scope exceeds the
  configured safe threshold; role display names MUST NOT affect this decision.
- **FR-026**: Runtime separation of duty MUST reject self-approval of a baseline,
  assignment, elevation, delegation-dependent decision, exception, or other
  governed submission even when the actor's role bundles contain both maker and
  approver capabilities.
- **FR-027**: Denied actions MUST provide an understandable reason distinguishing
  missing capability, out-of-scope data, expired authority, separation of duty,
  and missing eligible approver without exposing protected data.
- **FR-028**: Authorized process owners MUST be able to create, read, validate,
  propose, version, and retire Approval Route Definitions through the versioned
  transport interface; route stages MUST support direct manager, Organization
  Unit head, Position holder, named user or group, and required capability plus
  KPI Data Scope selectors. An Organization Unit head selector MUST explicitly
  reference an Employee for the relevant unit and MUST NOT infer the head from
  a Position or reporting-tree rank; resolution MUST verify that Employee's
  active employment, applicable unit context, and required scope against the
  applicable approved baseline. A named group selector MUST reference an
  Organization-scoped internal Approval Group with effective-dated Employee
  memberships. Every new or changed route version MUST require
  approval by a different eligible actor with the route-approval capability and
  applicable scope before activation; the creator or editor MUST NOT approve or
  activate that version. The approval decision, reason, evaluated capability,
  scope, and timeline MUST be preserved. Used route versions MUST remain
  immutable and stale route-head changes MUST return a stable conflict. The
  only active route for a governed artifact type MUST NOT be retired until an
  independently approved replacement version is ready; replacement activation
  and prior-route retirement MUST occur atomically without an unroutable gap,
  and existing submission snapshots MUST remain unchanged.
- **FR-029**: Approver resolution MUST use the applicable approved Organization
  Structure Baseline and MUST preserve the resolved selector, actors, scope,
  fallback, Position context, and route as an immutable submission snapshot. A
  Direct Manager selector MUST use the Position attached to the governed
  artifact's business responsibility or subject when present and MAY fall back
  to the Employee's applicable primary Position only when the artifact has no
  Position context. Named Group resolution MUST snapshot the effective eligible
  Employee member set so later membership changes do not alter a submitted
  route.
- **FR-030**: A configured approval stage MUST NOT be silently skipped; failure
  to resolve an eligible primary or fallback approver MUST block progression and
  explain the failed resolution.
- **FR-031**: Approval Delegation MUST be effective-dated, scope-limited, tied to
  specified approval responsibilities, and accepted only when it does not expand
  the original authority or permit self-approval.
- **FR-032**: A delegated decision MUST preserve both the original authority and
  delegate identities together with the delegation, decision, reason, and time.
- **FR-033**: Every accepted or rejected governed action MUST create an immutable
  Audit Record identifying the actor, represented authority when applicable,
  Organization, resource, revision, action, decision, reason, scope, and time.
- **FR-034**: Approval and exception timelines MUST be visible to involved actors
  and actors with applicable audit authority only within their permitted
  organization-tree scope.
- **FR-035**: Timelines MUST explain the selector, resolved approver, fallback,
  delegation, decision, reason, affected revision, and authorization impact in a
  form understandable without inspecting system internals.
- **FR-036**: Concurrent changes based on a stale draft or submitted revision
  MUST be rejected without overwriting newer facts and MUST identify the current
  revision to the user.
- **FR-037**: Historical baselines, role versions, assignments, delegations,
  approval routes, and Audit Records MUST remain discoverable after they cease
  to be current.
- **FR-038**: Effective-date changes MUST alter current authorization and routing
  only when their effective time is reached and MUST NOT rewrite prior decisions.
- **FR-039**: User-facing navigation and action availability MAY reflect
  effective capabilities for usability, but every governed action MUST be
  authorized independently from its visibility.
- **FR-040**: All validation and authorization rejections MUST identify the
  affected organization fact or governed artifact closely enough for an
  authorized user to correct the configuration without disclosing unrelated
  protected information.
- **FR-041**: The system MUST define a mandatory minimum policy for business-task
  capability risk and KPI Data Scope approval thresholds; an Organization MAY
  configure stricter thresholds but MUST NOT weaken or bypass that minimum.
- **FR-042**: Before an Organization's first approved baseline effective instant,
  baseline-dependent operations MUST remain blocked; from that instant onward,
  approved baselines MUST form one gapless and non-overlapping chain. Successor
  approval MUST atomically close predecessor applicability at the exact
  successor start, MUST NOT expose a standalone operation that creates a gap,
  and MUST be serialized safely against concurrent approvals.
- **FR-043**: When a new baseline becomes effective during an open KPI period,
  the foundation MUST preserve the prior effective facts, identify changed
  organization responsibility inputs, create an immutable impact requiring
  downstream resolution, and prevent that impact from being marked resolved
  until a governed KPI Plan amendment reference is registered. The later KPI
  Planning feature MUST identify affected KPI responsibilities and apply the
  actual re-cascade before results under the new structure become official.
- **FR-044**: When fixed new-assignee weights totaling `N` percent are introduced
  by a re-cascade, the remaining `100 - N` percent MUST be distributed across
  existing assignees in proportion to their prior weights using
  `new existing weight = prior weight × (100 - N) / sum of prior weights`; the
  result MUST preserve the prior relative ordering and total exactly 100 percent
  with the new-assignee weights.
- **FR-045**: A baseline or approved weight change during an open KPI period MUST
  create an immutable effective boundary so that facts before and after the
  change remain attributable to their applicable baseline, plan revision,
  assignments, and weights.
- **FR-046**: The foundation MUST publish an immutable Effective Segment
  integration contract identifying the applicable baseline, downstream plan
  revision, assignment-weight snapshot, and Aggregation Policy version, and
  MUST NOT calculate or claim an official KPI result. The later Evaluation
  feature MUST evaluate and combine official segment outcomes through that
  contract, using time-based proration only when the approved KPI Aggregation
  Policy explicitly permits it.
- **FR-047**: When proportional re-cascade weights exceed the configured
  precision, the system MUST round down at that precision and distribute the
  residual units by descending fractional remainder; ties MUST use prior
  relative order and stable assignment identity, the prior assignee order MUST
  NOT be reversed, and the final weights MUST total exactly 100 percent.
- **FR-048**: Explicit Organization provisioning MUST create two distinct
  Organization-scoped Bootstrap Principals for initial setup and independent
  approval. Bootstrap grants MUST be product-owned, non-delegable, fully
  audited, unusable outside their Organization, and forbidden from sharing one
  sign-in identity or bypassing same-artifact separation of duty. They MUST
  expire automatically only after independently approved, effective Role
  Assignments provide both replacement duties; failure to establish both
  replacements MUST leave the bootstrap grants visible and effective rather
  than silently locking the Organization out.
- **FR-049**: Every governed action MUST evaluate authorization from the current
  committed account, employment, Role Assignment, policy, baseline, delegation,
  resource-revision, and effective-time facts. An Authorization Decision MUST
  NOT be reused across separate governed actions. Repeated identical decisions
  MAY be memoized only within one action execution and only while the actor,
  Organization, capability, resource revision, effective instant, represented
  authority, and loaded authorization facts remain identical.
- **FR-050**: Before bootstrap handoff completes, replacement of an unavailable
  Bootstrap Principal MUST use a platform-level, time-bounded break-glass
  request approved by two distinct Platform Security Administrators. The
  request MUST identify only the principal being replaced, require a reason and
  expiry, reject either Bootstrap Principal as a platform approver, reject a
  replacement identity equal to the remaining principal, change no authority
  until both decisions exist, and preserve the request, decisions, replacement,
  resulting grant, and expiry as immutable audit evidence.

### Key Entities

- **Organization**: Company-level governance and data-isolation boundary that
  owns structure, workforce, roles, approvals, delegations, and audit history.
- **Organization Unit**: Effective-dated node with a stable code, type, status,
  and optional parent in the Organization hierarchy.
- **Position**: Stable governed responsibility location within an Organization
  Unit, used for reporting and approver resolution.
- **Employee**: Person eligible for organizational responsibilities, with
  employment status governed independently from sign-in account status.
- **Position Assignment**: Effective-dated relationship between an Employee and
  Position, including primary-Position and applicable allocation facts.
- **Reporting Relationship**: Effective relationship used to explain management
  and direct-manager resolution.
- **Organization Structure Baseline**: Approved immutable snapshot of the
  structure and workforce responsibility facts used by downstream
  authorization and planning; it never contains a Role Assignment.
- **Baseline Applicability Segment**: Effective interval selecting one approved
  baseline; after the first segment starts, segments form a gapless,
  non-overlapping chain and successor approval closes only predecessor
  applicability, never its reviewed content.
- **Baseline Change Impact**: Immutable explanation of the responsibility inputs
  changed by a successor baseline; it contains no mutable resolution status.
- **Baseline Impact Resolution**: Immutable one-per-impact link to one exact
  independently approved KPI Plan Amendment revision and its decision evidence;
  its existence is the only resolved-state proof.
- **KPI Capability**: Atomic fixed business-task authority used for
  authorization, carrying governance metadata that explains its business area,
  risk classification, and applicable scope impact.
- **Custom KPI Role**: Organization-defined immutable bundle of KPI Capabilities.
- **KPI Data Scope**: Organization, UnitSubtree, Assigned, or Self boundary in
  which a capability may be exercised.
- **Role Assignment**: Effective and approved relationship granting one exact
  role version to an Employee within a KPI Data Scope.
- **Bootstrap Principal**: One of two explicitly provisioned, distinct sign-in
  identities holding a temporary product-owned Organization-scoped setup or
  independent-approval grant until governed Role Assignments replace both
  duties; the grant is non-delegable, audited, and not a Custom KPI Role.
- **Bootstrap Recovery Request**: Time-bounded platform-level break-glass fact
  that replaces exactly one unavailable Bootstrap Principal only after two
  distinct Platform Security Administrator decisions and preserves immutable
  reason, expiry, decision, replacement, and grant evidence.
- **Approval Group**: Organization-scoped internal group with effective-dated
  Employee memberships used by Named Group approval selectors.
- **Approval Route Definition**: Organization-scoped route identity with
  optimistic head and immutable versions of ordered selector/fallback stages.
- **Approval Route Snapshot**: Resolved ordered approver route and fallback
  evidence, including any resolved Approval Group member set, preserved when a
  governed artifact is submitted.
- **Approval Delegation**: Effective and scope-limited authority to perform
  specified approval duties on behalf of another actor.
- **Audit Record**: Immutable explanation of a governed action and its context.

### Scope Boundaries

This feature ends when an Organization has an approved structure baseline and
runtime authorization can enforce capabilities, scopes, independent approval,
delegation, and audit visibility. Strategic Plans, Annual BSC content,
perspectives, Strategy Maps, KPI formulas and versions, KPI Plan Items, targets,
cascade amendment persistence, Actual Submissions, official segment evaluation
and aggregation, scoring, dashboards, Pilot operation, exports, production
porting, and reward calculation are outside this feature. This feature still
owns and behaviorally tests the approved-baseline eligibility gate, immutable
baseline-change impact, immutable approved-amendment resolution seam,
deterministic re-cascade preview, and Effective Segment contract that those
later features MUST consume.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In acceptance testing, 100% of organization-cycle, missing-primary,
  incomplete-baseline, stale structure revision, stale Custom Role head, and
  stale Approval Route head cases are blocked before approval/commit and
  identify the current conflicting fact or head.
- **SC-002**: Authorized administrators complete the representative
  organization-to-approved-baseline journey without direct data repair or
  developer assistance in at least 90% of first attempts.
- **SC-003**: Across the approved capability-and-scope authorization matrix,
  100% of allowed cases succeed and 100% of missing-capability, out-of-scope,
  expired-authority, and disabled-account cases are denied.
- **SC-004**: 100% of tested self-approval and self-elevation attempts are
  rejected, including Approval Route version activation and attempts by actors
  whose Custom KPI Roles contain both maker and approver capabilities.
- **SC-005**: 100% of risky capability combinations used in acceptance tests
  display a warning before role creation while remaining creatable after explicit
  acknowledgement.
- **SC-006**: Every required selector kind can be configured and versioned
  through the transport interface; 100% of tested only-active-route retirement
  attempts are blocked until an independently approved replacement can be
  switched atomically; and for every submitted approval case reviewers can
  identify the selector, resolved approver, fallback, Position context, resolved
  group membership, delegation, decision, reason, scope, route version, and
  affected revision from the timeline without technical assistance.
- **SC-007**: After an application interruption and recovery, 100% of approved
  baselines, historical revisions, effective assignments, role versions,
  approval snapshots, delegations, and Audit Records used in acceptance testing
  remain available and unchanged.
- **SC-008**: At least 90% of representative Organization Administrators,
  Security Administrators, approvers, and auditors complete their primary
  journey successfully on the first guided usability attempt.
- **SC-009**: All primary journeys are operable by keyboard and at a 390-pixel
  viewport without losing required actions, warnings, validation details, or
  approval evidence.
- **SC-010**: In acceptance testing, 100% of rows in the foundation's
  baseline-dependency decision matrix return the expected stable result before
  and after the first approved baseline: Dictionary authoring remains eligible,
  while representative Annual BSC/KPI planning, assignment, routing, cascade,
  and operation requests are denied until the gate finds an applicable baseline.
- **SC-011**: In the approved mid-period re-cascade example, prior assignee
  weights of 50, 20, and 30 percent plus a fixed 20 percent new-assignee weight
  produce revised weights of 40, 16, 24, and 20 percent, preserve the original
  assignee order, and total exactly 100 percent.
- **SC-012**: For every mid-period baseline change in this feature's acceptance
  testing, users can reproduce the gapless before/after effective boundary,
  immutable impact, either its exact approved-amendment resolution evidence or
  its derived unresolved state, allocation preview, and Effective Segment
  contract without retroactively changing earlier facts; official segment
  evaluation and whole-period aggregation are explicit acceptance obligations
  of the later Evaluation feature.
- **SC-013**: For 100% of accepted proportional re-cascade cases, including
  cases requiring rounding, repeated calculation produces the same assignment
  weights, preserves the prior assignee order, and totals exactly 100 percent at
  the configured precision.
- **SC-014**: Under the accepted local authorization load, 100% of next-action
  tests observe any account, employment, Role Assignment, policy, baseline, or
  delegation change from its effective instant without cross-action decision
  reuse, and authorization evaluation completes within 50 milliseconds p95
  after the governed resource facts are loaded.
- **SC-015**: In acceptance testing, 100% of bootstrap recovery attempts with
  one approver, a Bootstrap Principal acting as platform approver, a replacement
  matching the remaining principal, missing reason/expiry, or an expired
  request are rejected without changing authority, while a request approved by
  two distinct eligible Platform Security Administrators replaces only the
  unavailable principal and exposes the complete immutable audit evidence.
- **SC-016**: Under the first-feature acceptance envelope of 1,000 Employees and
  200 Organization Units, structure validation completes within 2 seconds;
  paged administration and authorized Organization-tree reads returning at
  most 200 nodes complete within 500 milliseconds p95. These measurements and
  the SC-014 authorization threshold are release-blocking automated evidence.

## Assumptions

- The domain model remains Organization-scoped for future multi-company use,
  while only one Organization is operationally exposed in the first release.
- The first-feature performance acceptance envelope is 1,000 Employees and 200
  Organization Units; larger production sizing remains a later port/load-test
  obligation and does not weaken Organization isolation or correctness rules.
- A sign-in identity capability already exists outside this feature; this
  feature governs account status, Employee linkage, authorization outcomes, and
  audit evidence without choosing an authentication method.
- KPI Capability identifiers are supplied by the product and are not created by
  Organization administrators; Custom KPI Roles only bundle those capabilities.
- Organization Structure Baselines and other governed approvals use a
  submit-and-independent-review pattern consistent with the approved KPI
  governance model.
- Effective times are interpreted consistently for the Organization; exact
  calendar and timezone implementation choices belong to technical planning.
- KPI Dictionary authoring is permitted before baseline approval, while Annual
  BSC/KPI planning, assignment, routing, cascade, and operation remain gated.
- The Organization and Authorization Foundation behaviorally enforces the
  approved-baseline gate, gapless applicability chain, baseline-change impact,
  approved-amendment resolution-registration contract, deterministic allocation
  preview, and Effective Segment contract. Detailed KPI plan amendment/
  re-cascade persistence and official cross-segment result aggregation belong
  to later KPI Planning and Evaluation features and cannot be claimed complete
  by this feature.
- Bulk organization or workforce import is not required for this feature's
  primary acceptance journey; future import adapters must obey the same
  validation, approval, revision, and audit rules.
