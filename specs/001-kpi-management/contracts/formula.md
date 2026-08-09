# Formula Contract

**Purpose**: Define the public Formula behavior shared by Draft validation, Formula Test Run, and official KPI Evaluation.

## Input authority

The caller supplies:

- Formula source text;
- ordered Formula Variable definitions;
- declared result type (`Decimal` or `Boolean`);
- optional non-null test/evaluation inputs.

The caller never supplies a trusted AST. The service generates the typed AST from source and exposes it only as read data.

## Language

| Construct | Supported behavior |
|---|---|
| Literals | invariant Decimal and Boolean values |
| Variables | declared, case-insensitive canonical `snake_case` names |
| Grouping | parentheses |
| Comparison | `=`, `!=`, `>`, `>=`, `<`, `<=` |
| Logic | `AND`, `OR`, `NOT` |
| Conditional | `IF(condition, when_true, when_false)` |
| Arithmetic | `+`, `-`, `*`, `/`, unary `-` |
| Percentage | postfix `%`, meaning divide by 100 |
| Functions | `ROUND(value, scale)`, `ABS(value)`, `MOD(value, divisor)` |

Precedence is grouping/value, percentage, unary, multiplication/division, addition/subtraction, comparison, `AND`, then `OR`.

## Compile response

```json
{
  "formula": {
    "source": "ROUND(revenue / target * 100, 2)",
    "ast": {
      "nodeType": "Call",
      "resultType": "Decimal",
      "span": { "start": 0, "length": 32 }
    }
  },
  "formulaLanguageVersion": 1,
  "astSchemaVersion": 1
}
```

- Source is preserved exactly as authored.
- Every AST node has stable `nodeType`, `resultType` and `span` data.
- Decimal literals are invariant strings; Boolean literals are booleans.
- Unknown schema version or node type is a safe failure, never an implicit fallback.

## Evaluate response

```json
{
  "outcome": {
    "kind": "Success",
    "value": { "type": "Decimal", "value": "30.0000000000" }
  }
}
```

or

```json
{
  "outcome": {
    "kind": "Failure",
    "code": "FORMULA_DIVISION_BY_ZERO",
    "message": "Localized user message",
    "span": { "start": 18, "length": 1 }
  }
}
```

## Safety and diagnostics

- Maximum 100 variables, 10,000 source characters, AST depth 32, 10,000 evaluated nodes and 500 ms evaluation duration.
- `IF`, `AND`, and `OR` type-check their content but evaluate only selected/required branches.
- Required variables must resolve through an explicit or compatible default value; null is rejected.
- Failures use stable codes across source/parse/type/input/arithmetic/limit/schema phases.
- Formula execution has no code compilation, reflection, callbacks, file, process, network, database, loop, recursion or external-data capability.

## Persistence contract

Formula Test Run returns this contract but persists nothing. Official KPI Evaluation stores the exact generated Formula Document and its language/schema versions with the input snapshot and outcome so history can be reproduced after later parser changes.
