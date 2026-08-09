# Formula Language Catalog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Expose the supported KPI formula operations through the API and provide an Excel-like autocomplete/syntax helper below the editor.

**Architecture:** Add one immutable `FormulaLanguageCatalog` in the Web/Application delivery boundary containing the public operation metadata. The Formula API serializes that catalog for discovery and includes it in validation/Test Run responses. The existing server Formula Engine remains authoritative; browser JavaScript only filters and inserts snippets.

**Tech Stack:** .NET 10 ASP.NET Core MVC, C# records, System.Text.Json, Razor `.cshtml`, vanilla browser JavaScript, xUnit integration tests, Playwright smoke tests, repository `harness.cmd`.

## Global Constraints

- Do not use `eval`, Roslyn scripting, or a second browser-side formula evaluator.
- Preserve Decimal-as-invariant-string JSON behavior and existing AST/version fields.
- Keep the supported language list synchronized through one catalog source.
- The suggestion list is advisory; server validation remains authoritative.
- Work directly on `main`; do not create a branch containing `codex`.
- Every vertical slice starts with a failing public-seam test and ends with `.\harness.cmd test`.

---

### Task 1: Add the formula language catalog and API discovery contract

**Files:**
- Create: `src/Kpi.Web/Formula/FormulaLanguageCatalog.cs`
- Modify: `src/Kpi.Web/Api/V1/FormulaController.cs`
- Test: `tests/Kpi.IntegrationTests/Web/FormulaLanguageCatalogApiTests.cs`
- Modify: `tests/Kpi.IntegrationTests/Web/KpiApiSmokeTests.cs`

**Interfaces:**
- `FormulaLanguageCatalog.All` returns immutable operation descriptors with `Name`, `Kind`, `Signature`, `Parameters`, `Description`, and `Example`.
- `FormulaLanguageCatalog.ToContract()` returns an object containing `formulaLanguageVersion`, `astSchemaVersion`, `operators`, `functions`, and `examples`.
- `GET /api/v1/formulas/capabilities` returns the catalog contract.
- `POST /api/v1/formulas/validate` and `/test-run` add `supportedOperations` containing the same contract.

- [ ] **Step 1: Write the failing API tests.** Assert the discovery endpoint contains `+`, `MOD`, `AND`, `OR`, `NOT`, `IF`, `ROUND`, and `ABS`, signatures, examples, and version metadata. Extend existing validate/Test Run tests to require `supportedOperations`.
- [ ] **Step 2: Run `.\harness.cmd test` and verify only the new catalog assertions fail.**
- [ ] **Step 3: Implement the immutable catalog and controller endpoint.** Use explicit arrays/records; do not derive descriptions from parser internals. Pass the catalog contract into both existing response anonymous objects.
- [ ] **Step 4: Run `.\harness.cmd test` and verify the API tests pass.**
- [ ] **Step 5: Commit:** `git add src/Kpi.Web/Formula src/Kpi.Web/Api/V1/FormulaController.cs tests/Kpi.IntegrationTests/Web && git commit -m "feat: expose formula language catalog"`.

### Task 2: Render the catalog and syntax helper in the Workbench

**Files:**
- Modify: `src/Kpi.Web/Views/Kpis/Edit.cshtml`
- Modify: `src/Kpi.Web/wwwroot/css/site.css`
- Modify: `src/Kpi.Web/wwwroot/js/formula-editor.js`
- Test: `tests/Kpi.IntegrationTests/Web/KpiWorkbenchPageTests.cs`

**Interfaces:**
- The editor receives a catalog URL through `data-formula-capabilities-url="/api/v1/formulas/capabilities"`.
- The page contains `#formula-suggestions-panel` with `role="listbox"` and `#formula-syntax-helper` with `role="status"` below `#formula-source`.
- JavaScript exports `attachFormulaEditor` unchanged and adds catalog loading with local fallback.

- [ ] **Step 1: Write failing page/JavaScript contract assertions** for the listbox, syntax helper, `ArrowDown`, `ArrowUp`, `Enter`, `Escape`, `supportedOperations`, and variable suggestions.
- [ ] **Step 2: Run `.\harness.cmd test` and verify the new assertions fail.**
- [ ] **Step 3: Implement catalog loading, token filtering, keyboard navigation, snippet insertion, and helper rendering.** Functions insert a signature such as `ROUND(value, decimals)`; operators insert safe spacing. Escape hides the list. Use textContent/DOM APIs for all catalog text.
- [ ] **Step 4: Add responsive/focus styles for the suggestion panel and syntax helper.** Keep the panel usable at 390px and in dark mode.
- [ ] **Step 5: Run `.\harness.cmd test` and verify Workbench contracts pass.**
- [ ] **Step 6: Commit:** `git add src/Kpi.Web/Views/Kpis/Edit.cshtml src/Kpi.Web/wwwroot/css/site.css src/Kpi.Web/wwwroot/js/formula-editor.js tests/Kpi.IntegrationTests/Web/KpiWorkbenchPageTests.cs && git commit -m "feat: add formula editor operation suggestions"`.

### Task 3: Add browser evidence for contextual syntax help

**Files:**
- Modify: `tests/Kpi.Web.EndToEndTests/KpiFullFlowTests.cs`
- Modify: `tests/Kpi.Web.EndToEndTests/Harness/EndToEndHarnessWitnessTests.cs` only if a test-discovery contract needs updating.

**Interfaces:**
- Browser test obtains a Workbench page, focuses `#formula-source`, types a function prefix, verifies a visible listbox item and syntax helper, selects with keyboard, and verifies the inserted source.
- Browser test verifies the helper remains visible at a 390px viewport and the theme toggle remains usable.

- [ ] **Step 1: Add a failing Playwright test for function suggestion and syntax helper selection.**
- [ ] **Step 2: Run `.\harness.cmd test` and verify the browser assertion fails before the implementation exists.**
- [ ] **Step 3: Fix only browser/ARIA behavior revealed by the test; do not move formula evaluation into JavaScript.**
- [ ] **Step 4: Run `.\harness.cmd test` and verify all browser tests pass.**
- [ ] **Step 5: Commit:** `git add tests/Kpi.Web.EndToEndTests && git commit -m "test: verify formula syntax assistance"`.

### Task 4: Update the integration guide and final verification

**Files:**
- Modify: `HUONG_DAN_TICH_HOP_KPI.txt`
- Modify: `README.md` only if the API route/test command needs a quick-start note.
- Modify: `docs/quality.md` only if the new catalog/syntax smoke layer changes verification policy.
- Modify: `tests/harness/integration-guide.tests.ps1`

**Interfaces:**
- The guide documents the supported operation table, signatures, examples, `GET /api/v1/formulas/capabilities`, the editor listbox, keyboard controls, and the syntax helper below the editor.

- [ ] **Step 1: Extend the guide contract with the discovery route, operation names, `formula-suggestions-panel`, and `formula-syntax-helper`.**
- [ ] **Step 2: Update the guide with copy/paste API examples and the manual editor workflow.**
- [ ] **Step 3: Run `.\harness.cmd check` and confirm locked restore, build, lint, all tests, E2E, and guide contract pass.**
- [ ] **Step 4: Review `git diff`, verify no generated output/secrets, then commit:** `git add HUONG_DAN_TICH_HOP_KPI.txt README.md docs/quality.md tests/harness/integration-guide.tests.ps1 && git commit -m "docs: explain formula operations and editor assistance"`.
- [ ] **Step 5: Push `main` to `origin/main` after final verification.**

## Self-review

- API and editor both consume the same catalog contract; no duplicate operation list is introduced.
- Existing formula parsing, AST serialization, Test Run persistence rules, and Decimal handling remain unchanged.
- The browser helper is presentation-only and cannot bypass server diagnostics.
- Failure behavior has explicit local fallback and existing validation diagnostics.
- Each implementation task has a RED → GREEN test cycle and a harness checkpoint.
