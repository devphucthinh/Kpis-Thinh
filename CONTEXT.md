# KPI Management

This context defines the shared language for authoring, governing, and evaluating organizational KPIs while preserving their history.

## Language

**KPI Definition**:
The stable identity of an organizational measure across all revisions of its meaning and formula, identified by an immutable company-scoped KPI Code.
_Avoid_: KPI record, KPI formula

**KPI Version**:
A traceable revision of a KPI Definition whose human-readable name, description, formula, variables, and explanatory change note are preserved after use or publication.
_Avoid_: Edited KPI, overwritten KPI

**Strategic Objective**:
A specific strategic outcome an Organization intends to achieve and for which one or more KPIs provide evidence of progress.
_Avoid_: KPI Target, KPI result, free-form KPI purpose

**Strategy Map**:
A governed directed acyclic graph whose nodes are Strategic Objectives and whose edges express intended cause-and-effect relationships.
_Avoid_: KPI cascade tree, free-form drawing, dashboard chart

**KPI Plan Item**:
A period- and scope-specific use of one exact KPI Version to measure one Strategic Objective, carrying the applicable target and scoring policy without changing the KPI Version.
_Avoid_: KPI Version, employee KPI copy

**KPI Target Set**:
The Baseline, Target, and Stretch Target values expected from a KPI Plan Item for its defined period and scope.
_Avoid_: Strategic Objective, KPI actual, formula variable

**Target Allocation Policy**:
The governed method that translates an annual KPI Target Set into reporting-period targets using an equal, custom, or formula-based allocation appropriate to the KPI's aggregation behavior.
_Avoid_: Scoring policy, KPI cadence, copied annual target

**KPI Assignment**:
A governed responsibility relationship between a KPI Plan Item and a person who is accountable for or contributes to that plan item.
_Avoid_: KPI ownership, KPI Version assignee list

**Position KPI Template**:
A reusable responsibility template that associates a Position with KPI Definitions or planning rules and is snapshotted into Employee KPI Assignments when an Annual KPI Plan is prepared.
_Avoid_: Employee KPI Assignment, job-title string, KPI Version assignee list

**Position Assignment**:
An effective-dated relationship recording that an Employee holds a Position within an Organization Unit.
_Avoid_: KPI Assignment, user role, department string

**Business Responsibility**:
A domain accountability such as Strategic Objective Owner, Data Owner, or KPI Plan Item Accountable that explains who is responsible without itself granting authorization.
_Avoid_: KPI Capability, user role, approval delegation

**KPI Cascade Edge**:
A traceable directed relationship between a parent and child KPI Plan Item, classified as Reference, Decompose, or Aggregate and carrying contribution metadata when aggregation is required.
_Avoid_: Copied KPI, Strategy Map edge, organization membership

**Composite KPI**:
A KPI Plan Item at a higher organizational scope whose Target and Actual Evaluations depend on exact child KPI Plan Items at descendant scopes through governed bindings.
_Avoid_: Arbitrary latest-KPI reference, Strategy Map objective, copied KPI

**Child KPI Binding**:
A period-aligned dependency from one parent Formula Variable to an exact child KPI Plan Item and KPI Version, carrying its governed contribution weight and separate Target and Actual channels.
_Avoid_: KPI code lookup, live latest-version reference, unweighted cascade edge

**Cascade Contribution Weight**:
The share of one child KPI Plan Item in the governed Target and Actual calculation of its direct Composite KPI parent; the active child bindings of each parent total 100 percent.
_Avoid_: Objective KPI Weight, Scorecard KPI Weight, formula literal

**Objective KPI Weight**:
The share of one KPI Plan Item in the derived progress of its Strategic Objective; the active KPI contributions of each objective total 100 percent.
_Avoid_: Cascade Contribution Weight, Scorecard KPI Weight

**Scorecard KPI Weight**:
The share of one KPI Score in the Official Aggregate Score of an Employee or Organization Unit scorecard; the required KPI weights of each scorecard total 100 percent.
_Avoid_: Cascade Contribution Weight, Objective KPI Weight, reward percentage

**KPI Code**:
An immutable, human-readable identifier that distinguishes a KPI Definition within a company.
_Avoid_: KPI name, database ID

**KPI Formula**:
The deterministic expression belonging to a KPI Version that calculates a result from declared Formula Variables.
_Avoid_: Script, executable code

**Formula Variable**:
A named and typed input declared by a KPI Version, including whether it is required and any default value.
_Avoid_: Placeholder, free-form parameter

**Formula Variable Target-Actual Pair**:
The required planned and observed value channels for one Formula Variable across reporting periods, preserved together so their absolute and relative variance remains traceable.
_Avoid_: Two unrelated variables, mutable dashboard values, KPI Target Set

**Variable Tracking Policy**:
The governed rules that determine how a Formula Variable's Target and Actual channels are sourced, aggregated over time, compared, and classified for attention.
_Avoid_: Formula Variable type, KPI Scoring Policy, UI chart setting

**Evaluation Input**:
The concrete, non-null value supplied for a Formula Variable when a KPI Evaluation is run, after any declared default has been applied.
_Avoid_: Variable definition, formula parameter

**KPI Evaluation**:
An immutable record of evaluating a KPI Version activated for a KPI Period with a specific set of Formula Variable values, preserving the result or error and the time of evaluation.
_Avoid_: Current KPI result, recalculated history

**Formula Test Run**:
A transient evaluation of a draft KPI Formula used for author feedback and discarded without becoming KPI history.
_Avoid_: KPI Evaluation, saved result

**Evaluation Outcome**:
The result of a KPI Evaluation, represented as either a successful declared value or a failure with a code, message, and explanatory details.
_Avoid_: Nullable result

**Superseding Evaluation**:
An immutable corrective KPI Evaluation that replaces the current interpretation of an earlier evaluation while retaining the old inputs, result, changed fields, and correction reason.
_Avoid_: Updated evaluation, overwritten result

**Current KPI Evaluation**:
The latest successful KPI Evaluation in an activation's correction chain; a later failed attempt remains visible but does not replace it.
_Avoid_: Latest attempt, mutable result

**KPI Cadence**:
The recurrence pattern of a KPI Version, supporting daily, monthly, quarterly, or annually governed reporting points.
_Avoid_: KPI Period, schedule

**Period Alignment Policy**:
The governed rule that aligns child KPI Evaluation Pairs to a parent Reporting Period, including same-period use, higher-frequency aggregation, and explicitly approved lower-frequency carry-forward or interpolation.
_Avoid_: Latest available result, calendar overlap alone, silent resampling

**KPI Aggregation Policy**:
The governed method such as Sum, Weighted Average, Last, Minimum, Maximum, or constrained Formula that rolls higher-frequency Evaluation Pairs into a lower-frequency Reporting Period.
_Avoid_: Calendar overlap, universal summation, dashboard-only calculation

**Organization Business Calendar**:
An Organization-scoped timezone calendar defining working days, non-working days, holidays, and exceptional workdays for Daily KPI scheduling.
_Avoid_: Server timezone, fixed Monday-to-Friday assumption

**KPI Period**:
A specific company-calendar interval, such as a month, quarter, or year, in which KPI Versions may be activated and evaluated.
_Avoid_: KPI Cadence, version lifetime

**Organization**:
The company-level governance and data-isolation boundary that owns its strategies, workforce scope, KPI plans, periods, evaluations, and audit history.
_Avoid_: Organization Unit, department, tenant database

**Organization Unit**:
An effective-dated node in an Organization's hierarchy, classified by a business type such as division, department, or section and linked to an optional parent unit.
_Avoid_: Organization, fixed department column, free-form unit name

**Organization Structure Baseline**:
An approved immutable snapshot of Organization Units, Positions, Position Assignments, reporting lines, and scoped role assignments used to plan and cascade organizational KPIs within its Baseline Applicability Segment.
_Avoid_: Live directory view, unapproved organization draft, Annual BSC Plan

**Baseline Applicability Segment**:
The effective interval during which one approved Organization Structure Baseline governs an Organization; after the first segment begins, successor segments form a gapless, non-overlapping chain without changing previously approved baseline content.
_Avoid_: Mutable baseline snapshot, optional calendar gap, KPI Effective Segment

**Organization Structure Revision**:
An immutable candidate snapshot frozen from the editable organization workspace when submitted for review; approval may make that exact revision an effective Organization Structure Baseline.
_Avoid_: Live organization tree, mutable baseline, database row version

**Baseline Change Impact**:
An immutable explanation of the Organization Units, Positions, Employees, assignments, and downstream responsibilities affected when one approved Organization Structure Baseline supersedes another from a defined effective instant.
_Avoid_: KPI Plan Amendment, recalculated KPI result, mutable diff

**Baseline Impact Resolution**:
An immutable link proving that one Baseline Change Impact was addressed by one exact independently approved KPI Plan Amendment revision; its presence, rather than a mutable status flag, makes the impact resolved.
_Avoid_: Acknowledgement checkbox, editable impact status, unapproved amendment reference

**Effective Segment**:
A non-overlapping portion of a KPI Period that preserves the exact Organization Structure Baseline, plan revision, responsibility weights, and aggregation-policy version applicable to facts in that portion.
_Avoid_: Recalculated whole period, dashboard date filter, latest organization view

**Employee**:
A person eligible to participate in organizational KPI responsibilities whose employment eligibility is governed independently from whether their system account can currently sign in.
_Avoid_: Login status, KPI Assignment, actor role

**Strategic Plan**:
An Organization's governed three-to-five-year statement of strategic direction and long-term outcomes.
_Avoid_: Annual BSC Plan, KPI Period

**Annual BSC Plan**:
A governed yearly translation of one Strategic Plan into BSC perspectives, Strategic Objectives, KPI Plan Items, targets, and causal relationships.
_Avoid_: Strategic Plan, KPI Period, KPI Dictionary

**BSC Perspective**:
An Organization-governed strategic viewpoint used to group Strategic Objectives; every Organization starts with the four standard BSC perspectives and may define additional perspectives.
_Avoid_: Strategic Objective, dashboard category, free-form tag

**KPI Measurement Scope**:
The declared level at which a KPI Plan Item produces its result, distinguishing a shared Organization or Organization Unit result from an individual Employee result.
_Avoid_: Data authorization scope, KPI Assignment role

**Actual Submission**:
An auditable proposal of observed KPI input values, including its source, effective time, submitter, and review evidence before it may affect an official result.
_Avoid_: Formula Test Run, KPI Target, mutable actual value

**Approval Workflow**:
A configurable ordered set of approval stages for a governed KPI artifact, resolved and preserved as an immutable workflow snapshot when that artifact is submitted.
_Avoid_: Hard-coded job-title chain, current organization hierarchy

**Approval Group**:
An Organization-scoped internal approver group whose Employee memberships are effective-dated and whose eligible member set is frozen when a Named Group selector resolves a submitted artifact.
_Avoid_: Live identity-provider group, mutable Employee list inside a route, Custom KPI Role

**Approval Route Definition**:
The stable Organization- and artifact-type-scoped identity that owns immutable Approval Route Versions and an optimistic configuration head.
_Avoid_: Approval Route Snapshot, active approver list, hard-coded workflow

**Approval Route Version**:
An immutable ordered selector/fallback configuration that must be submitted, independently approved outside its maker/editor set, and activated through the artifact-type route slot before it can resolve new submissions.
_Avoid_: Editable active route, Approval Route Snapshot, validation-only approval

**Approval Route Activation Slot**:
The single concurrency-controlled active Approval Route Definition and Version for one Organization and governed artifact type, switched atomically so replacement never creates an unroutable gap.
_Avoid_: Latest route version, standalone route retirement, frontend-selected route

**Approval Route Snapshot**:
The immutable selector, candidate, resolved approver, Position/group resolution, fallback, scope, and applicable Organization Structure Baseline evidence frozen for every stage when a governed artifact is submitted.
_Avoid_: Current manager lookup, editable approval route, approver display name

**Approval Delegation**:
An effective-dated and scope-limited authority for one actor to perform specified approval stages on behalf of another while preserving both identities and separation-of-duty rules.
_Avoid_: Role reassignment, administrator override, shared account

**Evidence Policy**:
The rules of a KPI Plan Item that determine which supporting files, links, or explanations an Actual Submission must provide and when reviewer comments are mandatory.
_Avoid_: Uploaded file, approval workflow, audit log

**KPI Actual Result**:
The official metric value produced by evaluating one exact KPI Version from an approved Actual Submission in its applicable KPI Plan Item and period.
_Avoid_: KPI Score, KPI Target, unapproved input

**KPI Target Evaluation**:
The official planned metric value produced by evaluating one exact KPI Version with the Target channels of its Formula Variables for a KPI Plan Item and Reporting Period.
_Avoid_: Manually competing KPI target, KPI Actual Result, stretch threshold

**KPI Evaluation Pair**:
The exact Target Evaluation and Actual Evaluation for the same KPI Version, KPI Plan Item, and Reporting Period from which KPI and variable variances are derived.
_Avoid_: Two unrelated formula runs, latest-value comparison

**KPI Variance**:
The direction-aware absolute and relative difference between an applicable KPI Target and KPI Actual Result for one reporting point or governed cumulative range.
_Avoid_: KPI Score, unversioned dashboard calculation, formula error

**KPI Time Series**:
The ordered, revision-aware Target, Actual, and Variance points of a KPI or Formula Variable across governed Reporting Periods, available as period and cumulative views.
_Avoid_: Latest value cache, mutable chart series, audit timeline

**KPI Change Comparison**:
A reconstructed comparison of governed KPI configuration, Target, Actual, Variance, Score, and revisions between two selected timestamps without creating a separate page or full configuration snapshot for every day.
_Avoid_: Daily duplicated page, latest-only view, audit log dump

**KPI Scoring Policy**:
The governed rules that map a KPI Actual Result against its KPI Target Set into a KPI Score, including direction, thresholds, caps, weighting, and rounding.
_Avoid_: KPI Formula, bonus rule, manually entered score

**Qualitative KPI**:
A KPI evaluated through a governed rubric or milestone set with explicit criteria and approved evidence rather than an unrestricted manually entered score.
_Avoid_: Free-form manager score, quantitative KPI formula

**KPI Score**:
The governed performance score derived from an official KPI result and its applicable target and scoring policy, suitable for review, filtering, highlighting, and export without representing a reward or payment amount.
_Avoid_: KPI actual, bonus amount, payroll instruction

**Official Aggregate Score**:
A governed roll-up of complete required KPI Scores for an Employee, Organization Unit, BSC Perspective, Strategic Objective, or Organization; missing required results prevent publication unless an audited exception policy applies.
_Avoid_: Partial dashboard average, unofficial preview, reward amount

**Score Completeness Exception**:
An approved, time-bounded decision that permits a clearly marked provisional aggregate despite missing required KPI Scores and records whether missing items are excluded, reweighted, or treated as zero.
_Avoid_: Administrator bypass, silent reweighting, ordinary correction

**Strategic Objective Progress**:
The derived progress of a Strategic Objective, calculated from weighted quantitative KPI Scores and explicitly evidenced qualitative milestones rather than silently edited dashboard data.
_Avoid_: Manually typed percentage, KPI Actual Result

**Pilot Mode**:
A designation that runs a KPI plan or period through the same governed workflow and durable persistence as production while excluding its results from official reporting and exports by default.
_Avoid_: Demo data, frontend simulation, separate ungoverned database

**Pilot Exit Gate**:
An evidence-backed checklist that must be visibly satisfied before an approved Pilot configuration can be promoted into a separate production plan or revision.
_Avoid_: Automatic timer, hidden release condition, informal sign-off

**KPI Issue**:
A governed finding from pilot or production operation linked to the exact affected KPI artifact, with severity, owner, root cause, corrective action, and resolution lifecycle.
_Avoid_: Free-form note, application error log, unlinked external ticket

**Annual Plan Carry-forward**:
The creation of a new Annual BSC Plan Draft from a prior plan's governed configuration with provenance and a reviewable difference report, never carrying forward actual results or scores.
_Avoid_: Extending the old plan, copying production results, silent rollover

**KPI Governance Status**:
The lifecycle position of a governed artifact such as a KPI Version or plan, kept separate from data-entry progress and performance outcome.
_Avoid_: KPI Data Status, Performance Band, Attention Flag

**KPI Data Status**:
The progress of collecting and approving KPI actual data, kept separate from governance lifecycle and measured performance.
_Avoid_: KPI Governance Status, Performance Band

**Performance Band**:
A scoring-policy classification such as No Data, Below Target, At Target, Above Target, or Stretch used for filtering and visual emphasis.
_Avoid_: KPI status, manually assigned color, reward grade

**Attention Flag**:
A derived operational warning such as Overdue, Missing Evidence, or Needs Correction that may coexist with any lifecycle or performance state.
_Avoid_: KPI Data Status, Performance Band, free-form status

**KPI Data Scope**:
The Organization, Organization Unit subtree, assigned responsibility, or self boundary within which an actor may exercise a granted KPI Capability.
_Avoid_: KPI Measurement Scope, role name, frontend visibility rule

**KPI Period Plan**:
A proposal defining a KPI Period's start, end, and selected KPI Versions before the period is authorized to become active.
_Avoid_: KPI Period Activation, published KPI list

**KPI Period Planner**:
The person who prepares and submits a KPI Period Plan but cannot approve that same plan.
_Avoid_: KPI Period Approver, KPI Creator

**KPI Period Approver**:
The authority who approves or rejects a submitted KPI Period Plan or Scheduled KPI Period Amendment without editing it or having submitted/proposed it.
_Avoid_: KPI Period Planner, KPI Policy Approver

**Scheduled KPI Period**:
An approved KPI Period whose selected versions are frozen while it waits for its start time.
_Avoid_: Draft period, Active KPI Period

**Active KPI Period**:
A KPI Period that has reached its start time and currently accepts official KPI Evaluations for its activated versions.
_Avoid_: Scheduled KPI Period, open draft

**Closed KPI Period**:
A KPI Period that has reached its end and no longer accepts ordinary KPI Evaluations while preserving its history.
_Avoid_: Cancelled period, deleted period

**KPI Period Amendment**:
An audited, separately reviewed proposal to change a Scheduled KPI Period by creating a new immutable effective revision instead of editing an approved plan in place.
_Avoid_: Period edit, silent correction

**KPI Period Effective Revision**:
An immutable complete interval-and-selection snapshot used for activation; revision zero is the original approved plan and each later revision comes from an approved Scheduled KPI Period Amendment.
_Avoid_: Mutable period plan, amendment delta

**KPI Period Activation**:
The governed selection of one KPI Version of a KPI Definition for use in a specific KPI Period.
_Avoid_: KPI publication, employee assignment

**KPI Evaluator**:
The person who supplies Evaluation Inputs for an activated KPI Version without changing its formula.
_Avoid_: KPI Creator, formula tester

**KPI Creator**:
The person accountable for authoring a KPI Definition and proposing its versions.
_Avoid_: KPI Administrator

**KPI Administrator**:
A governance role that monitors KPI Definitions, published KPI Versions, and KPI Evaluations without editing creator-owned content.
_Avoid_: KPI Creator, KPI Owner

**KPI Capability**:
An atomic business authority to perform one governed KPI action; capabilities are assigned independently of role names and remain the authorization unit.
_Avoid_: Hard-coded role check, menu permission

**Authorization Decision**:
The explainable allow or deny result for one actor, atomic KPI Capability, governed resource, effective time, KPI Data Scope, and any represented authority or separation-of-duty facts.
_Avoid_: Menu visibility, role-name check, authentication result

**Organization Security Policy**:
An Organization-scoped policy that may make the system's mandatory capability-risk and KPI Data Scope approval thresholds stricter but can never weaken them.
_Avoid_: Custom KPI Role, sign-in policy, optional security bypass

**KPI Role Template**:
A named organizational bundle of KPI Capabilities used to provision common responsibilities without making the role name itself an authorization rule.
_Avoid_: Capability, hard-coded authorization role

**Custom KPI Role**:
A named Organization-defined role whose immutable versions select bundles from the system's fixed KPI Capability catalog; changing a used capability bundle creates a new role version rather than mutating the existing version.
_Avoid_: Custom capability, hard-coded job title, business responsibility

**KPI Policy Approver**:
The executive or delegated policy authority who decides whether a proposed KPI Version is acceptable for organizational use.
_Avoid_: KPI Administrator, reviewer

**KPI Version Review**:
The decision process in which a KPI Policy Approver approves or rejects a proposed KPI Version with a recorded comment but cannot edit its content.
_Avoid_: KPI editing, publication

**Published KPI Version**:
An approved KPI Version made eligible for activation in a KPI Period from its effective date.
_Avoid_: Approved KPI Version, latest draft

**Retired KPI Version**:
A previously published KPI Version that is no longer available for new evaluations but remains available for history and audit.
_Avoid_: Deleted KPI

**KPI Ownership Transfer**:
An audited reassignment of accountability for a KPI Definition from one KPI Creator to another, authorized by a KPI Policy Approver and accompanied by a reason.
_Avoid_: Administrator edit, silent reassignment

**Change Summary**:
The required human explanation of how and why a KPI Version differs from its predecessor.
_Avoid_: Commit message, audit log

**Archived KPI Definition**:
A recoverable KPI Definition removed from normal use while its versions, evaluations, and audit history remain preserved.
_Avoid_: Hard-deleted KPI, retired version

**Audit Record**:
An immutable account of a governed KPI action, including who performed it, when it occurred, and enough context to explain the change.
_Avoid_: Application log
