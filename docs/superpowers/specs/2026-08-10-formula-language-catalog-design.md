# Formula Language Catalog and Editor Assistance

## Goal

Make the supported formula language discoverable and consistent across the
API, the formula editor, and the integration guide. A user must be able to see
the available operations, their signatures, examples, and a contextual hint
below the editor while writing a formula.

## Approved design

The Domain formula language remains the source of truth for parsing and
evaluation. A small application-facing language catalog describes the public
syntax without evaluating user input. The catalog contains:

- arithmetic operators: `+`, `-`, `*`, `/`, postfix `%`, and infix `MOD`;
- comparison operators: `=`, `!=`, `<`, `<=`, `>`, `>=`;
- logical operators: infix `AND`, infix `OR`, and unary `NOT`;
- functions: `IF`, `ROUND`, `ABS`, and call-form `MOD`;
- each item's kind, signature, parameter summary, description, and example;
- formula language and AST schema versions.

`GET /api/v1/formulas/capabilities` exposes this catalog as a stable discovery
resource. Existing `validate` and `test-run` responses include the same
`supportedOperations` payload so a client does not need a second request to
render the language help after a formula request.

The editor renders the catalog as a contextual suggestion list below the text
area. Suggestions are filtered by the current token and include declared
variable codes. Keyboard navigation uses Up/Down, Enter selects a suggestion,
and Escape closes the list. Selecting a function inserts its signature/snippet
at the caret; selecting an operator inserts the operator with safe spacing.
The source text remains unchanged until an explicit selection is made.

Below the suggestion list, an Excel-like syntax helper shows the selected
operation's signature, parameter description, and example. It is informational
only and never becomes part of the formula source. Existing server validation,
AST preview, Test Run, Decimal behavior, and the no-`eval` rule are unchanged.

## Data flow

```text
FormulaLanguageCatalog
        |-- GET /api/v1/formulas/capabilities
        |-- validate/test-run response supportedOperations
        |-- formula-editor.js suggestion list + syntax helper
        `-- HUONG_DAN_TICH_HOP_KPI.txt examples
```

## Failure behavior

- A catalog request is deterministic and does not require a formula or
  database access.
- Unknown or malformed formula text continues to use the existing diagnostics
  and source spans.
- A suggestion is never treated as validation; the server remains authoritative.
- If the catalog fetch fails, the editor keeps the local built-in catalog and
  still permits normal editing and server validation.

## Verification

- API integration tests assert the operation list, signatures, examples, and
  version metadata.
- API contract tests assert `supportedOperations` is present on validate and
  test-run responses.
- Editor contract tests assert listbox, keyboard hooks, syntax helper, and
  variable/function suggestions.
- Browser smoke asserts a suggestion can be selected and its syntax helper is
  visible below the editor.
- The guide contract checks the supported-operation table and discovery route.
