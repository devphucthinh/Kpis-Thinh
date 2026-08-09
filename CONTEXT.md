# KPI Management

This context defines the shared language for authoring, governing, and evaluating organizational KPIs while preserving their history.

## Language

**KPI Definition**:
The stable identity of an organizational measure across all revisions of its meaning and formula, identified by an immutable company-scoped KPI Code.
_Avoid_: KPI record, KPI formula

**KPI Version**:
A traceable revision of a KPI Definition whose human-readable name, description, formula, variables, and explanatory change note are preserved after use or publication.
_Avoid_: Edited KPI, overwritten KPI

**KPI Code**:
An immutable, human-readable identifier that distinguishes a KPI Definition within a company.
_Avoid_: KPI name, database ID

**KPI Formula**:
The deterministic expression belonging to a KPI Version that calculates a result from declared Formula Variables.
_Avoid_: Script, executable code

**Formula Variable**:
A named and typed input declared by a KPI Version, including whether it is required and any default value.
_Avoid_: Placeholder, free-form parameter

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
The recurrence pattern of a KPI Version, initially monthly, quarterly, or annually.
_Avoid_: KPI Period, schedule

**KPI Period**:
A specific company-calendar interval, such as a month, quarter, or year, in which KPI Versions may be activated and evaluated.
_Avoid_: KPI Cadence, version lifetime

**KPI Period Plan**:
A proposal defining a KPI Period's start, end, and selected KPI Versions before the period is authorized to become active.
_Avoid_: KPI Period Activation, published KPI list

**KPI Period Planner**:
The person who prepares and submits a KPI Period Plan but cannot approve that same plan.
_Avoid_: KPI Period Approver, KPI Creator

**KPI Period Approver**:
The authority who approves or rejects a submitted KPI Period Plan without editing it or having submitted it.
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
An audited, separately approved change proposed for a Scheduled, Active, or Closed KPI Period instead of editing the approved plan in place.
_Avoid_: Period edit, silent correction

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
