# Minimal HTTP Delivery Contract

**Purpose**: Describe the smallest versioned HTTP surface supporting the interactive UI and machine-readable feature requirements. Core rules remain in Application operations.

## Conventions

- Prefix: `/api/v1`.
- Requests write Formula source, variables and declared result type; they never write trusted AST.
- Decimal Formula Values use invariant strings; Boolean values use JSON booleans with explicit Formula Value type where polymorphic.
- Every mutable request carries an opaque concurrency token.
- Expected failures use RFC-compatible Problem Details with stable English code, localized message, correlation id and formula source span when applicable.

## Resources

| Resource | Minimum actions |
|---|---|
| Formula | validate source; Test Run without persistence. |
| KPI Definitions | list/get/create/update Draft metadata; archive/restore; transfer ownership; delete only eligible unused Draft content. |
| KPI Versions | list/get/create Draft/update Draft/clone/submit/approve/reject/publish/retire/diff. |
| KPI Periods | list/get/create/update Draft; manage selections; submit/approve/reject/cancel/amend; read resolved activations/state. |
| KPI Evaluations | create official attempt; list history; read Current; create correction. |
| Audit Records | read ordered records filtered by entity, actor, type and date. |

## Status mapping

| Result | HTTP outcome |
|---|---|
| Malformed transport | 400 |
| Missing resource | 404 |
| Stale or lifecycle/governance/range conflict | 409 |
| Formula/business validation failure | 422 |
| Unexpected infrastructure failure | 500 with safe generic detail |

## Public read guarantees

Formula reads expose exact source, generated AST, formula-language version and AST-schema version. Evaluation/history reads expose the exact Version identity, ordered input snapshot, outcome, Current/superseded relationship and correction diff. Audit reads never expose credentials or sensitive configuration.
