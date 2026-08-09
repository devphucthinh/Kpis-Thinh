# KPI Management Full-Flow UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the existing Vietnamese-first MVC pages into a coherent KPI Workbench that supports the complete visible journey from KPI draft through governance, Period activation, official Evaluation/history/correction, and Audit in professional Light/Dark themes.

**Architecture:** Keep Razor MVC as the primary rendering boundary and reuse the existing Application operations and `/api/v1` contracts. Add focused Web read-model/projection services and view models so controllers do not traverse Domain graphs for display, while small JavaScript modules provide formula validation, AST/Test Run, theme switching, and progressive enhancement. Add only the persistence/read seams required to display durable Period, Evaluation, and Audit facts when the Postgres profile is selected.

**Tech Stack:** .NET 10 ASP.NET Core MVC, Razor `.cshtml`, existing Application/Domain operations, PostgreSQL adapter, vanilla browser JavaScript, CSS design tokens with Bootstrap 5-compatible markup, xUnit integration tests, Playwright end-to-end tests, and the repository `harness.cmd`.

## Global Constraints

- Preserve `main`; do not create a branch whose name contains `codex`.
- Preserve the terminology in `CONTEXT.md`: KPI Definition, KPI Version, KPI Formula, Formula Variable, KPI Evaluation, Superseding Evaluation, Current KPI Evaluation, KPI Period Plan, KPI Period Activation, KPI Policy Approver, KPI Period Approver, and Audit Record.
- Vietnamese is the default UI language; assign stable resource keys so English can be added later. Function names in formulas remain English.
- Do not introduce a SPA, microservices, production identity integration, external connectors, employee assignment, nested KPI references, drag-and-drop authoring, or analytics dashboards.
- Formula source is authoritative on writes; the server produces the AST. Never use `eval` or binary floating point for KPI values.
- Test Runs are transient; official Evaluations and Audit Records are immutable facts and must remain durable under the Postgres runtime profile.
- Every vertical slice uses RED → GREEN tests and ends with `./harness.cmd test`; the complete change ends with `./harness.cmd check`.
- `./harness.cmd bootstrap` and `./harness.cmd check` must never write PostgreSQL schema; schema changes use the explicit migration action only.

---

## File map and shared seams

The following files are the planned ownership boundaries. A task may add a
small supporting file, but it must keep the same responsibility.

- `src/Kpi.Web/ViewModels/KpiWorkbenchViewModels.cs` — list/editor/version
  display models and typed formula-variable form rows.
- `src/Kpi.Web/ViewModels/KpiPeriodViewModels.cs` — Period Plan, selection,
  amendment, activation, and next-action models.
- `src/Kpi.Web/ViewModels/KpiEvaluationViewModels.cs` — typed input rows,
  current/history/correction display models.
- `src/Kpi.Web/ViewModels/OverviewViewModels.cs` and
  `src/Kpi.Web/ViewModels/AuditViewModels.cs` — operational cards and filtered
  Audit Record rows.
- `src/Kpi.Web/Queries/KpiWebReadModelService.cs` — converts Application
  operation results into the view models above and owns display projection;
  controllers do not walk nested Domain collections.
- `src/Kpi.Web/Queries/FormulaInputParser.cs` — converts typed form values to
  `FormulaValue` using the declared `FormulaValueType` without binary floats.
- `src/Kpi.Web/Controllers/*.cs` — HTTP orchestration, ModelState, persona
  checks, and Post/Redirect/Get only.
- `src/Kpi.Web/Views/Shared/*.cshtml` — layout, flash messages, status badge,
  action panel, and version stepper partials.
- `src/Kpi.Web/Views/Kpis/*`, `KpiPeriods/*`, `KpiEvaluations/*`, `Audit/*`,
  `Home/*` — pages for each vertical slice.
- `src/Kpi.Web/wwwroot/css/site.css` — semantic design tokens, layout, focus,
  responsive behavior, and both themes.
- `src/Kpi.Web/wwwroot/js/theme.js` — `data-theme` selection and local browser
  preference; it does not persist product data.
- `src/Kpi.Web/wwwroot/js/formula-editor.js` — formula editor, variable cards,
  autocomplete, validation, AST preview, and transient Test Run.
- `src/Kpi.Application/Persistence/IKpiGovernedPersistence.cs` and
  `src/Kpi.Infrastructure.Postgres/Stores/PostgresGovernedStore.cs` — durable
  read seams for Period, Evaluation, and Audit facts where the current UI
  would otherwise read only the process-local store.
- `tests/Kpi.IntegrationTests/Web/*PageTests.cs` — HTTP/view contracts.
- `tests/Kpi.Web.EndToEndTests/*` — one browser journey per slice and theme
  checks.
- `HUONG_DAN_TICH_HOP_KPI.txt` — update integration and local UI instructions
  after routes, assets, or test commands change.

## Shared view-model interfaces

Task 1 defines these exact types so later tasks can consume stable seams:

```csharp
public sealed record FormulaVariableInputVm(
    string Code,
    string DisplayName,
    FormulaValueType Type,
    bool Required,
    string? DefaultValue,
    string? Description,
    int DisplayOrder);

public sealed record KpiVersionListItemVm(
    Guid Id,
    int VersionNumber,
    string Name,
    KpiVersionStatus Status,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    bool IsCurrent,
    bool CanEdit,
    bool CanSubmit,
    bool CanReview,
    bool CanPublish);

public sealed record KpiWorkbenchVm(
    Guid DefinitionId,
    string Code,
    string Name,
    string Description,
    Guid OwnerId,
    IReadOnlyList<KpiVersionListItemVm> Versions,
    KpiVersionEditorVm? Draft,
    string? Notice,
    IReadOnlyList<string> Diagnostics);

public sealed record KpiVersionEditorVm(
    Guid VersionId,
    int VersionNumber,
    string Name,
    string Description,
    string Source,
    IReadOnlyList<FormulaVariableInputVm> Variables,
    string AstJson,
    string? ChangeSummary,
    KpiVersionStatus Status,
    long Revision,
    string ConcurrencyToken,
    IReadOnlyList<string> Diagnostics,
    bool CanSave,
    bool CanSubmit,
    bool CanReview,
    bool CanPublish,
    bool CanClone,
    bool CanArchive,
    bool CanRestore);
```

The Period, Evaluation, Overview, and Audit model contracts are defined in
their respective tasks before their views are changed. No view receives a
Domain aggregate directly once the corresponding task is complete.

---

### Task 1: Establish Web read models and typed formula input seams

**Files:**
- Create: `src/Kpi.Web/ViewModels/KpiWorkbenchViewModels.cs`
- Create: `src/Kpi.Web/Queries/KpiWebReadModelService.cs`
- Create: `src/Kpi.Web/Queries/FormulaInputParser.cs`
- Modify: `src/Kpi.Web/Kpi.Web.csproj` only if a new source folder requires no project change (SDK default globs should keep it unchanged)
- Test: `tests/Kpi.IntegrationTests/Web/KpiReadModelContractTests.cs`

**Interfaces:**
- Consumes: `KpiOperations.List(Guid organizationId)`, `EvaluationOperations.Current/History`, `PeriodOperations.List`, `ActorContext`, `FormulaDocumentSerializer.Serialize`, and `FormulaVariableDefinition`.
- Produces: `KpiWebReadModelService.GetKpiIndex`, `GetWorkbench`, and `GetVersionEditor`; `FormulaInputParser.Parse(IReadOnlyList<FormulaVariableInputVm>, IReadOnlyDictionary<string,string>)` returning `ApplicationResult<IReadOnlyDictionary<string,FormulaValue>>`.

- [ ] **Step 1: Write the failing projection tests.** Assert that a persisted-style `KpiDefinition` produces a `KpiWorkbenchVm` containing ordered variables, source text, `AstJson`, version status, concurrency token, and action flags; assert that invalid Boolean/Decimal/Null input returns a stable validation error instead of throwing.

```csharp
[Fact]
public void Workbench_projection_preserves_source_ordered_variables_and_ast()
{
    var page = service.GetWorkbench(definition.Id, actor.Current);
    Assert.Equal("gross_revenue - discount", page.Draft!.Source);
    Assert.Equal(["gross_revenue", "discount"], page.Draft.Variables.Select(x => x.Code));
    Assert.Contains("ast", page.Draft.AstJson, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public void Typed_input_parser_returns_stable_error_for_invalid_decimal()
{
    var result = FormulaInputParser.Parse(
        [new("amount", "Amount", FormulaValueType.Decimal, true, null, null, 0)],
        new Dictionary<string, string> { ["amount"] = "not-a-decimal" });
    Assert.False(result.IsSuccess);
    Assert.Equal("FORMULA_INPUT_INVALID", result.Error!.Code);
}
```

- [ ] **Step 2: Run the focused tests to verify RED.**

Run: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~KpiReadModelContractTests"`

Expected: FAIL because the read-model service and typed parser do not exist.

- [ ] **Step 3: Implement the minimal read-model and parser seams.** Project `FormulaDocumentSerializer.Serialize(version.Formula)` into `AstJson`, order variables by `DisplayOrder`, derive action flags from the existing `ActorContext.Can(...)` and lifecycle status, and parse Decimal with `CultureInfo.InvariantCulture`, Boolean with `bool.TryParse`, and explicit empty/`null` values as the singleton `FormulaValue.Null` only when the variable definition permits it.

- [ ] **Step 4: Run the focused tests to verify GREEN.**

Run: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~KpiReadModelContractTests"`

Expected: PASS with no unhandled parser exception and exact source/variable/AST round-trip assertions.

- [ ] **Step 5: Commit the read-model seam.**

```powershell
git add src/Kpi.Web/ViewModels src/Kpi.Web/Queries tests/Kpi.IntegrationTests/Web/KpiReadModelContractTests.cs
git commit -m "feat: add KPI UI read models"
```

---

### Task 2: Build the shared shell, visual tokens, and theme switching

**Files:**
- Modify: `src/Kpi.Web/Views/Shared/_Layout.cshtml`
- Create: `src/Kpi.Web/Views/Shared/_FlashMessage.cshtml`
- Create: `src/Kpi.Web/Views/Shared/_StatusBadge.cshtml`
- Create: `src/Kpi.Web/Views/Shared/_Sidebar.cshtml`
- Modify: `src/Kpi.Web/wwwroot/css/site.css`
- Create: `src/Kpi.Web/wwwroot/js/theme.js`
- Modify: `tests/Kpi.IntegrationTests/Web/DraftAuthoringPageTests.cs`
- Create: `tests/Kpi.IntegrationTests/Web/SharedShellPageTests.cs`

**Interfaces:**
- Consumes: `KpiWebReadModelService` output, `ViewData["Notice"]`, current persona display data, and `data-theme` attributes.
- Produces: navigation links for `/`, `/Kpis`, `/KpiPeriods`, `/KpiEvaluations/History`, and `/Audit`; theme toggle button with `data-theme-toggle`; reusable status/flash partials; responsive shell classes.

- [ ] **Step 1: Add failing HTTP contract tests.** Assert that `/Kpis` returns navigation labels and links, `data-theme-toggle`, `theme.js`, a status badge text, and a non-color-only focusable action. Assert `theme.js` contains both `prefers-color-scheme` handling and local preference storage.

```csharp
[Fact]
public async Task Shared_shell_exposes_navigation_and_theme_controls()
{
    var html = await client.GetStringAsync("/Kpis", TestContext.Current.CancellationToken);
    Assert.Contains("/KpiPeriods", html, StringComparison.Ordinal);
    Assert.Contains("/Audit", html, StringComparison.Ordinal);
    Assert.Contains("data-theme-toggle", html, StringComparison.Ordinal);
    Assert.Contains("theme.js", html, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run the shell tests to verify RED.**

Run: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~SharedShellPageTests"`

Expected: FAIL because the current layout has only two links and no theme asset.

- [ ] **Step 3: Implement the shared shell.** Add a Vietnamese-first top bar, navigation/sidebar partial, `aria-current` on the active item, flash message regions with `role="status"`/`role="alert"`, semantic badge text, keyboard-visible focus styles, and a toggle that sets `document.documentElement.dataset.theme` to `light` or `dark`.

- [ ] **Step 4: Run the shell tests to verify GREEN.**

Run: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~SharedShellPageTests|FullyQualifiedName~DraftAuthoringPageTests"`

Expected: PASS; existing antiforgery and formula-editor contracts remain green.

- [ ] **Step 5: Commit the shared shell.**

```powershell
git add src/Kpi.Web/Views/Shared src/Kpi.Web/wwwroot/css/site.css src/Kpi.Web/wwwroot/js/theme.js tests/Kpi.IntegrationTests/Web
git commit -m "feat: add KPI UI shell and themes"
```

---

### Task 3: Complete the KPI Workbench editor and Test Run experience

**Files:**
- Modify: `src/Kpi.Web/Controllers/KpisController.cs`
- Modify: `src/Kpi.Web/Views/Kpis/Index.cshtml`
- Modify: `src/Kpi.Web/Views/Kpis/Create.cshtml`
- Modify: `src/Kpi.Web/Views/Kpis/Edit.cshtml`
- Create: `src/Kpi.Web/Views/Shared/_VersionStepper.cshtml`
- Create: `src/Kpi.Web/Views/Shared/_KpiActionPanel.cshtml`
- Modify: `src/Kpi.Web/wwwroot/js/formula-editor.js`
- Modify: `tests/Kpi.IntegrationTests/Web/DraftAuthoringPageTests.cs`
- Create: `tests/Kpi.IntegrationTests/Web/KpiWorkbenchPageTests.cs`

**Interfaces:**
- Consumes: `KpiWebReadModelService.GetKpiIndex/GetWorkbench/GetVersionEditor`, `FormulaInputParser`, existing `KpiOperations.CreateDefinition/CreateVersion/UpdateDraft`, and formula API endpoints.
- Produces: `GET /Kpis?query=&status=`, `GET /Kpis/Edit/{id}` with the `KpiWorkbenchVm`, a typed variable-card form posted as `VariablesJson`, and client functions `parseVariableRows`, `scheduleValidation`, and `runTest` that preserve the declared variables.

- [ ] **Step 1: Write failing Workbench tests.** Cover list filtering, editor rehydration of source/ordered variables/AST, typed variable fields, visible version stepper, Test Run input generation for Decimal/Boolean/Null, and preservation of ModelState after invalid formula input.

```csharp
[Fact]
public async Task Workbench_renders_typed_variables_version_stepper_and_ast()
{
    var id = await CreateDefinitionAndVersion();
    var html = await client.GetStringAsync($"/Kpis/Edit/{id}", TestContext.Current.CancellationToken);
    Assert.Contains("Gross revenue", html, StringComparison.Ordinal);
    Assert.Contains("data-variable-type=\"Decimal\"", html, StringComparison.Ordinal);
    Assert.Contains("Draft", html, StringComparison.Ordinal);
    Assert.Contains("formula-ast", html, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run the Workbench tests to verify RED.**

Run: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~KpiWorkbenchPageTests|FullyQualifiedName~DraftAuthoringPageTests"`

Expected: FAIL for the missing typed variable cards/filter/stepper assertions while the old basic editor tests remain the baseline.

- [ ] **Step 3: Implement the Workbench.** Replace direct Domain traversal in `KpisController` with the read model, bind `VariablesJson` to `FormulaVariableInputVm` rows, keep a legacy newline parser only for existing clients, render the editor as two professional cards, add autocomplete suggestions for supported functions/operators and declared variables, render diagnostics as a list, and display server-produced AST JSON in a read-only `<pre>`.

- [ ] **Step 4: Implement client behavior and run tests.** `formula-editor.js` must regenerate Test Run inputs from the variable rows, submit the same ordered variable metadata to `/api/v1/formulas/validate` and `/api/v1/formulas/test-run`, display Decimal without binary conversion, show stable error code/message, and never persist Test Run history.

Run: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~KpiWorkbenchPageTests|FullyQualifiedName~DraftAuthoringPageTests"`

Expected: PASS with source, variables, AST, diagnostics, and transient Test Run contracts intact.

- [ ] **Step 5: Commit the Workbench slice.**

```powershell
git add src/Kpi.Web/Controllers/KpisController.cs src/Kpi.Web/Views/Kpis src/Kpi.Web/Views/Shared/_VersionStepper.cshtml src/Kpi.Web/Views/Shared/_KpiActionPanel.cshtml src/Kpi.Web/wwwroot/js/formula-editor.js tests/Kpi.IntegrationTests/Web
git commit -m "feat: complete KPI workbench editor"
```

---

### Task 4: Expose the governed KPI Version lifecycle

**Files:**
- Modify: `src/Kpi.Web/Controllers/KpisController.cs`
- Modify: `src/Kpi.Web/Queries/KpiWebReadModelService.cs`
- Modify: `src/Kpi.Web/Views/Kpis/Edit.cshtml`
- Modify: `src/Kpi.Web/Views/Shared/_KpiActionPanel.cshtml`
- Create: `src/Kpi.Web/Views/Kpis/_ReviewPanel.cshtml`
- Create: `src/Kpi.Web/Views/Kpis/_VersionTimeline.cshtml`
- Create: `tests/Kpi.IntegrationTests/Web/KpiGovernancePageTests.cs`

**Interfaces:**
- Consumes: existing `KpiOperations.SubmitVersion`, `ReviewVersion`, `PublishVersion`, `ReturnVersionToDraft`, `CloneVersion`, `Archive`, and `Restore`; `ActorContext` capability checks; `KpiWorkbenchVm` action flags.
- Produces: Post/Redirect/Get forms for Submit, Review, Publish, Return to Draft, Clone, Archive, and Restore; required reject/publish/clone reason fields; conflict messages with reload action.

- [ ] **Step 1: Write failing governance page tests.** Assert that Draft shows Submit/Clone, In Review shows read-only Review controls, rejection requires a comment, Approved shows effective-date Publish, Published is read-only with successor/Archive controls, and a self-review action is not rendered for the Creator persona.

```csharp
[Fact]
public async Task In_review_page_shows_read_only_review_and_requires_comment()
{
    var id = await CreateSubmittedVersion();
    var html = await client.GetStringAsync($"/Kpis/Edit/{id}", TestContext.Current.CancellationToken);
    Assert.Contains("KPI Policy Approver", html, StringComparison.Ordinal);
    Assert.Contains("name=\"comment\"", html, StringComparison.Ordinal);
    Assert.DoesNotContain("name=\"Source\"", html, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run governance tests to verify RED.**

Run: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~KpiGovernancePageTests"`

Expected: FAIL because current Edit only has the draft form and redirects errors into a query-string notice.

- [ ] **Step 3: Implement state-aware action panels and timeline.** Use the Application result status/error code to return the Workbench with `ModelState` and a flash message instead of silently redirecting on failure; render only actions allowed by the current persona/state, include effective-from and change-summary fields, and preserve the original version snapshot in the timeline.

- [ ] **Step 4: Run focused governance tests to verify GREEN.**

Run: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~KpiGovernancePageTests"`

Expected: PASS for state/action visibility, required comments, no self-approval, and concurrency conflict messaging.

- [ ] **Step 5: Commit the governance slice.**

```powershell
git add src/Kpi.Web/Controllers/KpisController.cs src/Kpi.Web/Queries/KpiWebReadModelService.cs src/Kpi.Web/Views/Kpis src/Kpi.Web/Views/Shared/_KpiActionPanel.cshtml tests/Kpi.IntegrationTests/Web/KpiGovernancePageTests.cs
git commit -m "feat: expose governed KPI version flow"
```

---

### Task 5: Build the KPI Period Plan, selection, approval, and activation UI

**Files:**
- Create: `src/Kpi.Web/ViewModels/KpiPeriodViewModels.cs`
- Modify: `src/Kpi.Application/PeriodOperations.cs` to add an atomic `SelectMany(ActorContext actor, Guid periodId, IReadOnlyDictionary<Guid,Guid> selections, ConcurrencyToken? token = null)` command that validates every selection before mutating the Draft Period.
- Modify: `src/Kpi.Web/Queries/KpiWebReadModelService.cs`
- Modify: `src/Kpi.Web/Controllers/KpiPeriodsController.cs`
- Modify: `src/Kpi.Web/Views/KpiPeriods/Index.cshtml`
- Modify: `src/Kpi.Web/Views/KpiPeriods/Create.cshtml`
- Create: `src/Kpi.Web/Views/KpiPeriods/Details.cshtml`
- Create: `src/Kpi.Web/Views/KpiPeriods/_SelectionTable.cshtml`
- Create: `tests/Kpi.Application.Tests/Periods/PeriodSelectionCommandTests.cs`
- Create: `tests/Kpi.IntegrationTests/Web/KpiPeriodPageTests.cs`

**Interfaces:**
- Consumes: `PeriodOperations.Create/Select/Submit/Approve/Reject/ReturnToDraft/ProposeAmendment/ReviewAmendment/Activate/Close`, `KpiOperations.List`, and `KpiPeriod` selection/revision/activation data.
- Produces: `KpiPeriodDetailsVm`, `KpiPeriodSelectionVm`, and the atomic `SelectMany` command; routes `GET /KpiPeriods/Details/{id}` and POST forms for selection, submit, approval, rejection, amendment, activation, and close.

- [ ] **Step 1: Write failing Application and page tests.** Assert that `SelectMany` rejects any non-Published/wrong-cadence/out-of-effective-range/other-organization version without partially changing selections; assert that the Period Details page shows newest eligible version first, frozen selection summary, lifecycle stepper, approver comment, and activation records.

```csharp
[Fact]
public void SelectMany_is_atomic_when_one_selected_version_is_ineligible()
{
    var result = operations.SelectMany(actor, period.Id,
        new Dictionary<Guid, Guid> { [eligibleDefinition.Id] = eligibleVersion.Id, [definition.Id] = draftVersion.Id });
    Assert.False(result.IsSuccess);
    Assert.Empty(period.SelectedVersions);
}
```

- [ ] **Step 2: Run the focused tests to verify RED.**

Run: `dotnet test tests/Kpi.Application.Tests/Kpi.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~PeriodSelectionCommandTests"; dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~KpiPeriodPageTests"`

Expected: FAIL because `SelectMany`, the Details route, and the guided selection view do not exist.

- [ ] **Step 3: Implement atomic selection and read models.** Validate all selections with the existing period eligibility rule before applying any; commit one period/audit mutation with the supplied concurrency token. Project each candidate with status, cadence, effective range, and a human-readable ineligibility reason.

- [ ] **Step 4: Implement the Period pages and governance forms.** Add a guided Create/Details flow, searchable KPI list, version dropdown ordered newest-to-oldest, frozen summary after Submit/Approve, Amendment review cards, scheduled/active/closed state badges, and explicit confirmation for Activate/Close. Return conflict/validation errors to the same Details page.

- [ ] **Step 5: Run the Period tests to verify GREEN.**

Run: `dotnet test tests/Kpi.Application.Tests/Kpi.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~PeriodSelectionCommandTests"; dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~KpiPeriodPageTests"`

Expected: PASS with no partial selections and visible lifecycle/eligibility behavior.

- [ ] **Step 6: Commit the Period slice.**

```powershell
git add src/Kpi.Application/PeriodOperations.cs src/Kpi.Web/ViewModels/KpiPeriodViewModels.cs src/Kpi.Web/Queries/KpiWebReadModelService.cs src/Kpi.Web/Controllers/KpiPeriodsController.cs src/Kpi.Web/Views/KpiPeriods tests/Kpi.Application.Tests/Periods/PeriodSelectionCommandTests.cs tests/Kpi.IntegrationTests/Web/KpiPeriodPageTests.cs
git commit -m "feat: complete KPI period planning UI"
```

---

### Task 6: Complete official Evaluation, history, and correction UI

**Files:**
- Create: `src/Kpi.Web/ViewModels/KpiEvaluationViewModels.cs`
- Modify: `src/Kpi.Web/Queries/KpiWebReadModelService.cs`
- Modify: `src/Kpi.Web/Queries/FormulaInputParser.cs`
- Modify: `src/Kpi.Web/Controllers/KpiEvaluationsController.cs`
- Modify: `src/Kpi.Web/Views/KpiEvaluations/Create.cshtml`
- Modify: `src/Kpi.Web/Views/KpiEvaluations/History.cshtml`
- Modify: `src/Kpi.Web/Views/KpiEvaluations/Correct.cshtml`
- Create: `src/Kpi.Web/Views/KpiEvaluations/_EvaluationOutcome.cshtml`
- Create: `tests/Kpi.IntegrationTests/Web/KpiEvaluationPageTests.cs`
- Modify: `tests/Kpi.Application.Tests/Evaluations/OfficialEvaluationGovernanceTests.cs` only for missing read/history assertions discovered by the RED test.

**Interfaces:**
- Consumes: `EvaluationOperations.Evaluate/Current/History/Correct`, active `KpiPeriodActivation`, version formula snapshot/variables, and `FormulaInputParser`.
- Produces: `KpiEvaluationPageVm` with 25-record default history, typed input rows, current result/failure, immutable formula snapshot, correction diff, and explicit “Tính evaluation mới”/“Tạo Superseding Evaluation” actions.

- [ ] **Step 1: Write failing evaluation page tests.** Assert that Create is available only for an Active Activation, the form renders the exact ordered variable types/defaults, history limits to 25, a failed result leaves Current unchanged, and Correct displays predecessor/superseding diff with a required reason.

```csharp
[Fact]
public async Task Evaluation_page_renders_typed_inputs_current_result_and_history_limit()
{
    var html = await client.GetStringAsync($"/KpiEvaluations/Create?definitionId={definitionId}&activationId={activationId}", TestContext.Current.CancellationToken);
    Assert.Contains("Current KPI Evaluation", html, StringComparison.Ordinal);
    Assert.Contains("data-input-type=\"Decimal\"", html, StringComparison.Ordinal);
    Assert.Contains("Tính evaluation mới", html, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run evaluation tests to verify RED.**

Run: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~KpiEvaluationPageTests"`

Expected: FAIL because current Create/Correct views are explanatory text and MVC input parsing assumes every value is Decimal.

- [ ] **Step 3: Implement typed input and read models.** Load the matching Active Activation and exact Published Version through the read service; render Decimal/Boolean/Null inputs; use invariant Decimal parsing and explicit Boolean/null handling; return stable errors in the same form without replacing Current.

- [ ] **Step 4: Implement History and Correction views.** Show Current first, then latest 25 immutable attempts, formula/version identifiers, inputs, outcome, evaluator, timestamp, and failure reason. Show correction diff (changed inputs, predecessor result, replacement result, reason) and keep the predecessor read-only.

- [ ] **Step 5: Run the focused tests to verify GREEN.**

Run: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~KpiEvaluationPageTests|FullyQualifiedName~OfficialEvaluationGovernanceTests"`

Expected: PASS for activation guard, typed inputs, 25-record history, failure/current semantics, and correction diff.

- [ ] **Step 6: Commit the Evaluation slice.**

```powershell
git add src/Kpi.Web/ViewModels/KpiEvaluationViewModels.cs src/Kpi.Web/Queries src/Kpi.Web/Controllers/KpiEvaluationsController.cs src/Kpi.Web/Views/KpiEvaluations tests/Kpi.IntegrationTests/Web/KpiEvaluationPageTests.cs tests/Kpi.Application.Tests/Evaluations/OfficialEvaluationGovernanceTests.cs
git commit -m "feat: complete KPI evaluation history UI"
```

---

### Task 7: Make durable Period/Evaluation/Audit reads available to the UI

**Files:**
- Modify: `src/Kpi.Application/Persistence/IKpiGovernedPersistence.cs`
- Modify: `src/Kpi.Application/KpiOperations.cs`
- Modify: `src/Kpi.Application/PeriodOperations.cs`
- Modify: `src/Kpi.Application/EvaluationOperations.cs`
- Modify: `src/Kpi.Infrastructure.Postgres/Stores/PostgresGovernedStore.cs`
- Modify: `src/Kpi.Infrastructure.Postgres/Persistence/KpiDbContext.cs` only if query indexes/relationships are required by the existing schema
- Create: `tests/Kpi.IntegrationTests/Persistence/GovernedReadModelTests.cs`

**Interfaces:**
- Consumes: existing relational rows `kpi_periods`, `kpi_period_activations`, `kpi_period_amendments`, `kpi_evaluations`, and `audit_records`.
- Produces: `LoadPeriods(Guid organizationId)`, `LoadEvaluations(Guid organizationId, Guid definitionId, Guid? activationId)`, and `LoadAudit(Guid organizationId, AuditQuery query)` on `IKpiGovernedPersistence`; Postgres implementations return immutable facts ordered newest-first. InMemory adapters return equivalent deterministic data.

- [ ] **Step 1: Write failing durable-read tests.** Create a Period, activation, official Evaluation, and Audit Record through the Postgres adapter, create a new Web scope, and assert that the read seam returns the same selected versions, formula snapshot, current flag, and Audit filters after process state is refreshed.

- [ ] **Step 2: Run the opt-in read tests to verify RED.**

Run: `$env:KPI_POSTGRES_TESTS='1'; dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~GovernedReadModelTests"`

Expected: FAIL or skip with the current adapter because governed read methods are not implemented; when credentials are absent the fixture must report the repository-standard deterministic skip.

- [ ] **Step 3: Implement the persistence read ports and adapters.** Deserialize JSONB formula/input/outcome/diff snapshots using the existing serializers, preserve Decimal strings and Boolean/Null types, map Audit summary JSON, and apply organization/entity/date/actor/event filters in the database query rather than in the Razor view.

- [ ] **Step 4: Run the opt-in tests to verify GREEN.**

Run: `$env:KPI_POSTGRES_TESTS='1'; dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~GovernedReadModelTests"`

Expected: PASS when configured, deterministic skip otherwise, with no schema writes during test setup beyond the explicit migration fixture.

- [ ] **Step 5: Commit the durable-read slice.**

```powershell
git add src/Kpi.Application/Persistence src/Kpi.Application/KpiOperations.cs src/Kpi.Application/PeriodOperations.cs src/Kpi.Application/EvaluationOperations.cs src/Kpi.Infrastructure.Postgres/Stores/PostgresGovernedStore.cs src/Kpi.Infrastructure.Postgres/Persistence/KpiDbContext.cs tests/Kpi.IntegrationTests/Persistence/GovernedReadModelTests.cs
git commit -m "feat: add durable KPI governed reads"
```

---

### Task 8: Add operational Overview and filterable Audit timeline

**Files:**
- Create: `src/Kpi.Web/ViewModels/OverviewViewModels.cs`
- Create: `src/Kpi.Web/ViewModels/AuditViewModels.cs`
- Modify: `src/Kpi.Web/Queries/KpiWebReadModelService.cs`
- Modify: `src/Kpi.Web/Controllers/HomeController.cs`
- Modify: `src/Kpi.Web/Controllers/AuditController.cs`
- Modify: `src/Kpi.Web/Views/Home/Index.cshtml`
- Modify: `src/Kpi.Web/Views/Audit/Index.cshtml`
- Create: `tests/Kpi.IntegrationTests/Web/OverviewAndAuditPageTests.cs`

**Interfaces:**
- Consumes: durable/read-model seams from Task 7, current persona capabilities, KPI/Period/Evaluation status counts, and `AuditQuery`.
- Produces: operational cards linking to existing Workbench pages and Audit filters for actor, event, entity type/id, from/to, result, and date; no analytics dashboard entity.

- [ ] **Step 1: Write failing page tests.** Assert that Home renders next-action cards with links, Audit renders filter controls and preserves query values in the form, and an empty filtered result explains the active filters.

- [ ] **Step 2: Run tests to verify RED.**

Run: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~OverviewAndAuditPageTests"`

Expected: FAIL because Home is a placeholder and Audit has no visible filters.

- [ ] **Step 3: Implement the operational pages.** Add cards for Drafts, versions awaiting review, Periods awaiting approval/activation, active periods, failed evaluations, and recent activity. Add an accessible Audit filter form and read-only timeline with actor, action, entity, reason/summary, timestamp, and deep link.

- [ ] **Step 4: Run focused tests to verify GREEN.**

Run: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~OverviewAndAuditPageTests"`

Expected: PASS with no domain-state duplication in views.

- [ ] **Step 5: Commit the overview/audit slice.**

```powershell
git add src/Kpi.Web/ViewModels/OverviewViewModels.cs src/Kpi.Web/ViewModels/AuditViewModels.cs src/Kpi.Web/Queries/KpiWebReadModelService.cs src/Kpi.Web/Controllers/HomeController.cs src/Kpi.Web/Controllers/AuditController.cs src/Kpi.Web/Views/Home src/Kpi.Web/Views/Audit tests/Kpi.IntegrationTests/Web/OverviewAndAuditPageTests.cs
git commit -m "feat: add KPI overview and audit timeline"
```

---

### Task 9: Verify responsive/accessibility behavior and the complete browser journey

**Files:**
- Modify: `src/Kpi.Web/wwwroot/css/site.css`
- Modify: `src/Kpi.Web/wwwroot/js/theme.js`
- Modify: `src/Kpi.Web/Views/Shared/_Layout.cshtml` and shared partials only for final ARIA/focus fixes found by browser tests
- Modify: `tests/Kpi.Web.EndToEndTests/Harness/EndToEndHarnessWitnessTests.cs`
- Create: `tests/Kpi.Web.EndToEndTests/KpiFullFlowTests.cs`

**Interfaces:**
- Consumes: all routes and selectors from Tasks 2–8.
- Produces: browser evidence for Workbench, governance, Period, Evaluation, Audit, Light/Dark themes, narrow layout, keyboard navigation, and no unhandled page errors.

- [ ] **Step 1: Write failing Playwright journeys.** Cover creator creates/edits/validates/test-runs a KPI, submits it, policy persona reviews/publishes it, planner creates/selects/submits a Period, approver approves/activates it, evaluator records an official Evaluation, correction shows a diff after closure, and Audit links back. Add a theme toggle assertion and a 390px viewport assertion.

```csharp
[Fact]
public async Task Full_flow_is_navigable_from_workbench_to_audit()
{
    await page.GotoAsync(baseUrl + "/Kpis");
    await page.GetByRole(AriaRole.Link, new() { Name = "KPIs" }).ClickAsync();
    await page.GetByRole(AriaRole.Button, new() { Name = "Test Run" }).ClickAsync();
    await Expect(page.Locator("[data-theme-toggle]")).ToBeVisibleAsync();
    await page.SetViewportSizeAsync(390, 844);
    await Expect(page.Locator("main")).ToBeVisibleAsync();
}
```

- [ ] **Step 2: Run the browser tests to verify RED.**

Run: `dotnet test tests/Kpi.Web.EndToEndTests/Kpi.Web.EndToEndTests.csproj --no-restore --filter "FullyQualifiedName~KpiFullFlowTests"`

Expected: FAIL while selectors and governed pages are still incomplete.

- [ ] **Step 3: Implement only the CSS/ARIA/browser fixes revealed by the journeys.** Ensure focus rings, labels, error summaries, modal confirmation controls, theme contrast, and stacked narrow layouts work without changing domain behavior.

- [ ] **Step 4: Run the browser tests to verify GREEN.**

Run: `dotnet test tests/Kpi.Web.EndToEndTests/Kpi.Web.EndToEndTests.csproj --no-restore --filter "FullyQualifiedName~KpiFullFlowTests"`

Expected: PASS for the happy path, theme switch, and narrow viewport.

- [ ] **Step 5: Commit the browser verification slice.**

```powershell
git add src/Kpi.Web/wwwroot/css/site.css src/Kpi.Web/wwwroot/js/theme.js src/Kpi.Web/Views/Shared tests/Kpi.Web.EndToEndTests
git commit -m "test: verify KPI full-flow UI journey"
```

---

### Task 10: Update integration guidance and run the complete harness

**Files:**
- Modify: `HUONG_DAN_TICH_HOP_KPI.txt`
- Modify: `README.md` only if route/theme/test instructions changed
- Modify: `docs/quality.md` only if a new verification layer is introduced
- Test: `tests/harness/integration-guide.tests.ps1`

**Interfaces:**
- Consumes: final routes, theme preference behavior, Test Run vs official Evaluation rules, Postgres runtime/migration instructions, and browser test command from Tasks 1–9.
- Produces: copy/pasteable integration steps for another `.cshtml` project and a guide contract that names every required asset, route, DI registration, persona restriction, and verification command.

- [ ] **Step 1: Write/update the guide contract test.** Require the guide to mention Workbench routes, `formula-editor.js`, `theme.js`, Light/Dark preference, Period Details, official Evaluation, correction history, Audit filters, `run-kpi.bat postgres`, and `./harness.cmd check`.

- [ ] **Step 2: Run the guide test to verify RED if documentation is stale.**

Run: `pwsh -NoProfile -File tests/harness/integration-guide.tests.ps1`

Expected: FAIL only for missing final UI route/theme wording.

- [ ] **Step 3: Update the guide and README.** Document file locations, route map, Development persona expectations, theme toggle, formula variable metadata format, Test Run non-persistence, official Evaluation persistence, correction diff, PostgreSQL profile, and manual browser checks.

- [ ] **Step 4: Run the guide test and full harness.**

Run: `pwsh -NoProfile -File tests/harness/integration-guide.tests.ps1`; then `./harness.cmd check`

Expected: guide contract passes; build, lint, all tests, migration contracts, branch policy, and E2E discovery pass. PostgreSQL opt-in tests may report the repository-standard skip when credentials are not enabled.

- [ ] **Step 5: Commit and push the completed UI change on `main`.**

```powershell
git add HUONG_DAN_TICH_HOP_KPI.txt README.md docs/quality.md tests/harness/integration-guide.tests.ps1
git commit -m "docs: document KPI full-flow UI integration"
git push origin main
```

## Plan self-review

- **Spec coverage:** Sections 1–4 are covered by Tasks 1–3 and 9; governance
  lifecycle by Task 4; Period Plan/Activation by Task 5; official Evaluation,
  history, and correction by Tasks 6–7; Overview/Audit by Task 8; localization,
  responsive behavior, themes, safety, tests, and guide handoff by Tasks 2,
  9, and 10.
- **Completeness:** Every task names files, interfaces, tests, commands,
  expected RED/GREEN behavior, and a commit; no step is left as an unspecified
  future action.
- **Type consistency:** Later tasks consume the `KpiWorkbenchVm`,
  `KpiVersionEditorVm`, `FormulaVariableInputVm`, `KpiWebReadModelService`,
  `FormulaInputParser`, and `IKpiGovernedPersistence` seams defined earlier.
- **Scope check:** No task introduces a SPA, production identity, external
  connector, employee assignment, nested KPI, drag-and-drop editor, or future
  analytics dashboard.
- **Operational risk:** Task 7 explicitly addresses the current durable-read
  gap so Postgres reloads can display official Evaluation and Audit facts; no
  Web startup or check path is allowed to mutate schema.
