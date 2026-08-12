# Authorization Decision Interface

This is the authoritative internal interface used by every governed Application
command. HTTP attributes, role names, menu visibility, and Razor conditionals
cannot replace it.

## Interface

```csharp
public interface IAuthorizationDecision
{
    Task<AuthorizationDecision> DecideAsync(
        ActorIdentity actor,
        KpiCapabilityId capability,
        AuthorizationResource resource,
        DateTimeOffset effectiveAt,
        RepresentedAuthority? representedAuthority,
        CancellationToken cancellationToken);
}
```

Callers must load the resource identity and safe scope facts before invoking
the interface. The module hides active bootstrap/handoff lookup, catalog lookup,
account/employment eligibility, effective role version/assignment lookup, scope
containment, delegation intersection, system/Organization policy, and
separation of duty. One Application-command context may memoize an identical
check, but an `AuthorizationDecision` is never cached or reused across governed
actions.

## Required evaluation order

The order is externally observable through stable denial codes and must remain
deterministic:

1. Reject an Organization mismatch without disclosing the foreign resource.
2. Resolve whether the actor is one of the two active Organization Bootstrap
   Principals. If so, require the capability in that duty's product-fixed
   allowlist, reject delegation, and continue to baseline/resource and
   separation-of-duty checks. An expired/replaced principal has no authority.
3. Otherwise require an enabled linked account and active employment when the
   capability is workforce-based.
4. Resolve effective approved Role Assignments for the requested instant.
5. Require the atomic capability in at least one exact assigned role version.
6. Require at least one matching KPI Data Scope for the resource facts. A
   `UnitSubtree` always identifies an applicable approved baseline, never an
   editable structure revision.
7. If represented authority is used, intersect original authority, delegation,
   effective interval, responsibility, and scope; never union them.
8. Apply same-artifact separation of duty against submitter, requester,
   beneficiary, prior maker/editor, and represented authority. Route-version
   review and activation both reject every actor who created or edited that
   version, even when one Custom Role contains maker, approver, and activator
   capabilities.
9. Return Allow with only the minimum assignment/scope evidence needed for
   audit; otherwise return one stable Deny reason.

Every invocation reloads the current committed bootstrap/handoff, account,
employment, role/assignment, baseline, delegation, and resource facts. The next
governed action must observe any committed revoke, expiry, scope/baseline
change, recovery replacement, or bootstrap handoff.

If several failures apply, the first safe reason in this order is returned. A
cross-Organization request maps to not-found/denied without confirming the
resource exists.

## Stable decision result

```text
AuthorizationDecision
  Outcome: Allow | Deny
  ReasonCode:
    allowed
    organization_mismatch
    account_disabled
    account_unlinked
    employment_inactive
    missing_capability
    scope_mismatch
    authority_not_effective
    bootstrap_capability_not_granted
    bootstrap_expired
    bootstrap_delegation_forbidden
    delegation_not_effective
    delegation_scope_mismatch
    separation_of_duty
    baseline_missing
    approver_unresolved
  CapabilityId
  OrganizationId
  ResourceType / ResourceId / ResourceRevision
  EffectiveAt
  AssignmentIds[]
  BootstrapPrincipalId?
  ScopeEvidence[]
  RepresentedAuthorityActorId?
  DelegationId?
```

The Web adapter maps:

- organization mismatch or protected unknown resource to HTTP 404;
- disabled/unlinked account to HTTP 403;
- missing capability, scope mismatch, expired authority, delegation failure,
  and separation of duty to HTTP 403;
- missing approved baseline or unresolved approver during a state transition to
  HTTP 422;
- stale resource revision to HTTP 409 before or during command commit.

All responses use stable Problem Details `type`/`code` values and a safe
`detail`. Diagnostic IDs and paths may be included only when the caller has
scope to view them.

## Capability catalog rules

- IDs are stable dotted business-task codes, not controller/action names.
- Catalog metadata is fixed by the product and versioned with the application.
- Organization administrators may bundle any codes into a Custom KPI Role.
- Conflict metadata creates warnings; it never disables runtime separation of
  duty.
- A role version is immutable after creation. A changed bundle is a new version.
- Creating or managing a role never grants its capabilities.

Initial business-area groups:

| Business area | Representative capabilities |
|---|---|
| Organization | `organization.structure.view`, `organization.structure.edit`, `organization.baseline.submit`, `organization.baseline.approve` |
| Workforce | `workforce.employee.view`, `workforce.employee.manage`, `workforce.position.manage` |
| Security | `security.custom-role.view`, `security.custom-role.manage`, `security.role-assignment.request`, `security.role-assignment.approve` |
| Approval | `approval.group.manage`, `approval.route.manage`, `approval.route.submit`, `approval.route.approve`, `approval.route.activate`, `approval.delegation.request`, `approval.delegation.approve`, `approval.decision.make` |
| Audit | `audit.timeline.view`, `audit.organization.view` |

The implementation task must publish the complete initial catalog and tests;
these representative IDs establish naming and grouping, not an exhaustive list.

## Bootstrap authority profile

Organization provisioning creates exactly two distinct subject bindings. The
fixed profile is versioned with the product and cannot be edited or delegated:

| Duty | Fixed initial capabilities |
|---|---|
| Setup | `organization.structure.view`, `organization.structure.edit`, `organization.baseline.submit`, `security.custom-role.view`, `security.custom-role.manage`, `security.role-assignment.request`, `audit.timeline.view` |
| Independent approval | `organization.structure.view`, `organization.baseline.approve`, `security.custom-role.view`, `security.role-assignment.approve`, `audit.timeline.view` |

The first baseline contains only structure/workforce facts. It is submitted by
the setup principal and approved by the independent-approval principal. Roles
and Role Assignments are then created against that baseline. When effective
approved assignments cover both replacement duties, the handoff fact and expiry
of all active Bootstrap Principals commit atomically. Until then, both duties
remain active. A principal cannot approve its own submission, use the other
duty, approve its own Role Assignment, or delegate bootstrap authority.

Platform break-glass recovery is deliberately outside this interface: it uses
the host's Platform Security Administrator authority, requires two distinct
platform approvals, and cannot be authorized by a Bootstrap Principal or an
Organization KPI Role. See `bootstrap-authority.md`.

## KPI Data Scope containment

`Self` is narrowest, followed by `Assigned`, `UnitSubtree`, and `Organization`
for privilege-threshold comparison. Runtime containment is not solely rank-based:
the exact Employee, responsibility, baseline, unit ancestry, and Organization
must match.

Examples:

- Organization scope contains all resources only in the same Organization.
- UnitSubtree contains a resource when the resource unit path in the applicable
  approved baseline includes the scoped root unit.
- Assigned contains only a resource whose governed responsibility snapshot
  identifies the acting Employee.
- Self contains only the Employee's own resource or explicitly self-scoped input.

## Security floor merge

The effective privilege policy is a monotonic merge:

```text
effective risk threshold = stricter(system threshold, organization threshold)
effective safe scope     = narrower(system safe scope, organization safe scope)
always-approve set       = union(system set, organization set)
```

A policy update that attempts the opposite is rejected with
`security.policy.weakens-system-floor` and the protected field name.

## Test surface

The interface is tested with matrix rows across:

- account enabled/disabled/unlinked;
- employment active/inactive;
- effective/not-yet-effective/expired Role Assignment;
- capability present/missing;
- Organization/UnitSubtree/Assigned/Self match and mismatch;
- direct and represented authority;
- submitter/requester/beneficiary self-approval conflicts;
- current and historical approved baselines;
- Organization isolation;
- both bootstrap duties, fixed-capability allow/deny, non-delegation, self-
  approval rejection, partial/full handoff, expiry, and recovery replacement;
- a committed revoke/scope/baseline/handoff change followed by a new action, to
  prove no cross-action cache is used.

Tests assert outcome, stable reason, evidence minimization, and transactional
Audit Record behavior through the same interface used by controllers.

## Approval Route governance seam

Route configuration is governed independently from runtime artifact approval:

```csharp
public interface IApprovalRouteGovernance
{
    Task<ApprovalRouteVersion> SubmitAsync(...);
    Task<ApprovalRouteVersionReview> DecideAsync(...);
    Task<ApprovalRouteActivationResult> ActivateAsync(...);
    Task<ApprovalRouteDefinition> RetireInactiveAsync(...);
}
```

`SubmitAsync` freezes the version and all maker/editor identities.
`DecideAsync` requires `approval.route.approve`, applicable scope, and a reviewer
outside that maker set. `ActivateAsync` requires an approved version,
`approval.route.activate`, the same maker exclusion, current route-head and
artifact-type activation-slot tokens, and one atomic prior-retire/target-
activate transaction. `RetireInactiveAsync` rejects the active slot target with
`approval.route.replacement-required`.

Selector resolution receives one immutable `ApprovalSubjectContext`. A supplied
artifact Position is authoritative for `DirectManager`; primary Position is
used only when context is absent. `OrganizationUnitHead` uses its configured
Employee plus exact-unit Position Assignment. `NamedGroup` resolves the
effective internal Approval Group membership. Every source and candidate set is
written to the route snapshot.

## Organization tree query authorization

The workspace tree query asks the same decision module for
`organization.structure.view` and applies the actor's effective KPI Data Scope
to the approved baseline projection before returning Unit/Position nodes.
Search, lazy branch expansion, and selected-Position restoration cannot expand
the scope. The reduced action projection remains advisory; direct Position URLs
and every later command are independently authorized.

## Approved baseline gate

Baseline dependency is a separate deep Application interface so later modules
cannot invent their own interpretation:

```csharp
public interface IApprovedBaselineGate
{
    Task<BaselineEligibilityDecision> DecideAsync(
        Guid organizationId,
        BaselineDependentOperation operation,
        DateTimeOffset effectiveAt,
        CancellationToken cancellationToken);
}
```

The fixed decision matrix is:

| Operation | Before first/applicable baseline | With applicable baseline |
|---|---|---|
| KPI Dictionary authoring | Allow: `baseline_not_required` | Allow: `baseline_not_required` |
| Annual BSC planning | Deny: `baseline_missing` | Allow: `baseline_applicable` |
| KPI Plan submission | Deny: `baseline_missing` | Allow: `baseline_applicable` |
| Position KPI templating | Deny: `baseline_missing` | Allow: `baseline_applicable` |
| KPI Assignment | Deny: `baseline_missing` | Allow: `baseline_applicable` |
| Approval-route resolution | Deny: `baseline_missing` | Allow: `baseline_applicable` |
| Organization cascade | Deny: `baseline_missing` | Allow: `baseline_applicable` |
| KPI operation | Deny: `baseline_missing` | Allow: `baseline_applicable` |

Feature 002 executes this matrix through Domain/Application/API tests. Later
Planning and Evaluation commands call the same interface before their own
behavior; this feature does not implement or claim completion of those modules.

Baseline lookup follows the applicability chain: before the first segment
starts, no baseline exists; afterward one and only one contiguous segment must
contain the requested instant. A missing segment after that first start is a
chain-integrity failure, not an ordinary eligibility result.
