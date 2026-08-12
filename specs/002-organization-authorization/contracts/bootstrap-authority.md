# Bootstrap Authority Contract

This contract crosses the otherwise circular first-baseline boundary without
creating a permanent hard-coded administrator. Platform authorization is a host
boundary; Organization authorization remains capability plus KPI Data Scope.

## Application interfaces

```csharp
public interface IOrganizationBootstrapProvisioner
{
    Task<BootstrapProvisioningResult> ProvisionAsync(
        ProvisionOrganization command,
        PlatformActor actor,
        CancellationToken cancellationToken);
}

public interface IBootstrapRecoveryGovernance
{
    Task<BootstrapRecoveryRequest> RequestAsync(
        RequestBootstrapRecovery command,
        PlatformActor actor,
        CancellationToken cancellationToken);

    Task<BootstrapRecoveryDecisionResult> DecideAsync(
        DecideBootstrapRecovery command,
        PlatformActor actor,
        CancellationToken cancellationToken);
}

public interface IBootstrapHandoffEvaluator
{
    Task<BootstrapHandoffResult> TryCompleteAsync(
        Guid organizationId,
        DateTimeOffset effectiveAt,
        CancellationToken cancellationToken);
}
```

`ProvisionAsync` atomically creates the Organization and exactly two active,
distinct Bootstrap Principals: `Setup` and `IndependentApproval`. Their opaque
subject IDs are supplied explicitly. The fixed product grant-profile version is
stored; no request may supply capability IDs. Retrying the same idempotency key
and identical payload returns the same result; a different payload conflicts.

`IBootstrapHandoffEvaluator` is an internal consequence of Role Assignment
approval/effectiveness, never a public manual-expiry command. It reads current
approved effective assignments. If both replacement duties are not covered, it
returns `Pending` without changing principal state. If both are covered, one
transaction inserts the immutable handoff, references the exact two assignments,
expires all active Bootstrap Principals, and writes audit evidence.

## Platform authorization boundary

- Provisioning and recovery require host-provided platform capabilities,
  `platform.organization.provision` and `platform.bootstrap.recover`.
- Platform Security Administrators are external subjects, not Organization
  Employees, Bootstrap Principals, Custom KPI Roles, or Role Assignments.
- Development/test uses an explicit deterministic platform-identity adapter.
  It is selected only by the development/test profile and is never a runtime
  fallback for a missing production identity integration.
- A Bootstrap Principal cannot provision an Organization, create/decide a
  recovery request, or act as a platform approver.

## Recovery state and decision rules

```text
Pending --reject--> Rejected
Pending --expires--> Expired
Pending --two distinct valid approvals--> Applied
```

A request identifies exactly one active unavailable principal, a replacement
subject, reason, and expiry. The replacement differs from both the unavailable
and remaining active Bootstrap Principal. Each decision is immutable and
records actor, outcome, reason, time, and correlation ID. Two approvals must be
made by distinct Platform Security Administrators; neither may be either
Bootstrap Principal. One approval, duplicate approval, any rejection, expiry,
or a stale unavailable-principal reference changes no authority. The second
valid approval atomically replaces only the unavailable principal with the same
duty and grant-profile version.

## Transport and stable outcomes

The versioned OpenAPI exposes provisioning/status and recovery request/decision
operations. Protected unknown Organization/request/principal references return
404. Stable domain outcomes include:

| HTTP | Code | Meaning |
|---|---|---|
| 403 | `platform_authority_missing` | Actor lacks the required host capability. |
| 403 | `bootstrap_principal_forbidden` | A Bootstrap Principal attempted platform recovery/provisioning. |
| 409 | `organization.provision.idempotency_conflict` | Same idempotency key, different payload. |
| 409 | `bootstrap.recovery.already_terminal` | Request is rejected, expired, or applied. |
| 409 | `bootstrap.recovery.principal_changed` | The unavailable principal is no longer the active duty holder. |
| 422 | `bootstrap.principals_must_be_distinct` | Provision/replacement identities violate separation. |
| 422 | `bootstrap.recovery.two_person_required` | A duplicate administrator cannot provide the second approval. |
| 422 | `bootstrap.recovery.approver_ineligible` | Approver is a Bootstrap Principal or lacks platform authority. |
| 422 | `bootstrap.recovery.expiry_invalid` | Expiry is absent, elapsed, or beyond platform policy. |

No response returns credentials, tokens, directory attributes, or hidden facts
about another Organization.

## Required contract evidence

Tests must prove:

1. Provisioning is atomic, idempotent, and creates two distinct active duties.
2. Fixed bootstrap grants allow only their documented tasks, are non-delegable,
   and obey maker/approver separation.
3. The first approved baseline contains structure/workforce facts and zero Role
   Assignments.
4. One replacement assignment leaves both principals active; the second causes
   one atomic immutable handoff and expires both.
5. One/duplicate/ineligible approval, rejection, expiry, wrong replacement, and
   stale principal change no authority; two valid approvals replace exactly one.
6. PostgreSQL restart preserves provisioning, recovery decisions, replacements,
   handoff, expiry, and audit history.
