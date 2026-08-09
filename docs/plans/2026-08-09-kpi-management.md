# KPI Management MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a locally runnable, persistence-backed KPI management application that proves versioned formula authoring, governed periods, immutable evaluations, auditability, REST JSON round-trips, and a Vietnamese Bootstrap 5 UI.

**Architecture:** Implement a modular ASP.NET Core monolith with Domain and Application independent of ASP.NET/EF Core, PostgreSQL behind task-focused persistence interfaces, and MVC/API adapters in Web. The Formula module is a deep module with `Compile` and `Evaluate` as its public interface; all AST construction, typing, limits, and execution stay internal.

**Tech Stack:** .NET SDK 10.0.302, C# 14, ASP.NET Core MVC 10.0, Bootstrap 5.3.8, EF Core 10.0.10, Npgsql EF Core 10.0.3, PostgreSQL 18.4, xUnit 2.9.3, Microsoft.NET.Test.Sdk 18.8.1, xunit.runner.visualstudio 3.1.5, Playwright 1.61.0.

## Global Constraints

- Work and push directly on `main`; the harness rejects other active branches and every branch name containing `codex`.
- Use .NET 10 LTS pinned to SDK `10.0.302` in `global.json`; do not use .NET 11 preview packages.
- Decimal behavior is `System.Decimal`, maximum precision 28, maximum scale 10, invariant string JSON, and midpoint rounding away from zero.
- Formula execution is constrained: no `eval`, Roslyn scripting, reflection, recursion, loops, file, process, database, or network access.
- Formula limits are 100 variables, 10,000 source characters, AST depth 32, 10,000 evaluated nodes, and 500 ms elapsed evaluation.
- UI is server-rendered `.cshtml`, Bootstrap-compatible HTML, and vanilla JavaScript modules; no SPA framework.
- `vi-VN` is default, `en-US` core resources ship from the first release, and formula keywords remain English.
- Persona simulation is Development-only and causes startup failure when enabled outside Development.
- PostgreSQL credentials live in .NET user-secrets or environment variables; no credentials or `.env` files enter Git.
- Every behavior change follows RED → verify failure → minimal GREEN → verify all relevant tests → refactor.
- Every setup, lint/static, unit, integration, and browser command is exposed through `.harness/harness.json`.
- Complete behavior and data contracts are defined in `docs/superpowers/specs/2026-08-09-kpi-management-design.md` and `CONTEXT.md`.

---

## File Map

```text
KpiManagement.slnx                         Solution inventory
global.json                                Exact SDK pin
Directory.Build.props                      Nullable, analyzers, warnings, language
Directory.Packages.props                   Central package versions
src/Kpi.Domain/Formula/                    Tokenizer, AST, compiler, evaluator
src/Kpi.Domain/Kpis/                       Definition and version governance
src/Kpi.Domain/Periods/                    Period planning and lifecycle
src/Kpi.Domain/Evaluations/                Immutable attempts and corrections
src/Kpi.Application/                       Commands, queries, ports, DTOs
src/Kpi.Infrastructure.Postgres/           EF Core mappings, migrations, adapters
src/Kpi.Web/Api/                           Versioned JSON controllers and ProblemDetails
src/Kpi.Web/Controllers/                   MVC page controllers
src/Kpi.Web/Views/                         Razor/Bootstrap pages
src/Kpi.Web/wwwroot/js/                    Formula editor and persona modules
src/Kpi.Web/Resources/                     vi-VN and en-US resources
tests/Kpi.Domain.Tests/                    Formula and aggregate unit tests
tests/Kpi.Application.Tests/               Application operation and clock tests
tests/Kpi.IntegrationTests/                PostgreSQL/API round-trip tests
tests/Kpi.Web.EndToEndTests/               Playwright smoke workflow
docs/decisions/0002-kpi-application-stack.md  Durable stack decision
docs/architecture.md                       Deployed module/data-flow map
HUONG_DAN_TICH_HOP_KPI.txt                  Human-and-agent integration guide
```

## Pre-execution Spec Kit Gate

Before Task 1 changes application files, run the repository skills in this order against feature directory `specs/001-kpi-management/`:

1. `speckit-specify` creates `spec.md` from the approved written design without changing scope.
2. `speckit-plan` creates `plan.md` and its design artifacts using the module/file map below.
3. `speckit-tasks` creates dependency-ordered `tasks.md` whose task boundaries match this plan.
4. `speckit-analyze` must report no critical inconsistency across `spec.md`, `plan.md`, and `tasks.md`.

If analysis finds a conflict, the approved written design is authoritative; update the Spec Kit artifact before writing code.

### Task 1: Install and pin the .NET toolchain, scaffold the solution, and wire the harness

**Files:**
- Create: `global.json`
- Create: `KpiManagement.slnx`
- Create: `Directory.Build.props`
- Create: `Directory.Packages.props`
- Create: `src/Kpi.Domain/Kpi.Domain.csproj`
- Create: `src/Kpi.Domain/AssemblyMarker.cs`
- Create: `src/Kpi.Application/Kpi.Application.csproj`
- Create: `src/Kpi.Infrastructure.Postgres/Kpi.Infrastructure.Postgres.csproj`
- Create: `src/Kpi.Web/Kpi.Web.csproj`
- Create: `tests/Kpi.Domain.Tests/Kpi.Domain.Tests.csproj`
- Create: `tests/Kpi.Application.Tests/Kpi.Application.Tests.csproj`
- Create: `tests/Kpi.Domain.Tests/Architecture/AssemblyBoundaryTests.cs`
- Create: `tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj`
- Create: `tests/Kpi.Web.EndToEndTests/Kpi.Web.EndToEndTests.csproj`
- Create: `docs/decisions/0002-kpi-application-stack.md`
- Modify: `.harness/harness.json`
- Modify: `.gitignore`
- Modify: `docs/architecture.md`
- Create: `src/Kpi.Domain/packages.lock.json`
- Create: `src/Kpi.Application/packages.lock.json`
- Create: `src/Kpi.Infrastructure.Postgres/packages.lock.json`
- Create: `src/Kpi.Web/packages.lock.json`
- Create: `tests/Kpi.Domain.Tests/packages.lock.json`
- Create: `tests/Kpi.Application.Tests/packages.lock.json`
- Create: `tests/Kpi.IntegrationTests/packages.lock.json`
- Create: `tests/Kpi.Web.EndToEndTests/packages.lock.json`
- Test: existing `tests/harness/branch-policy.tests.ps1`

**Interfaces:**
- Consumes: repository harness and approved design spec.
- Produces: compilable `net10.0` solution, central package versions, project references, and canonical restore/format/build/test commands.

- [ ] **Step 1: Verify tool absence and install SDK 10.0.302**

Run:

```powershell
dotnet --version
winget install --id Microsoft.DotNet.SDK.10 --exact --version 10.0.302 --source winget --accept-package-agreements --accept-source-agreements
dotnet --version
```

Expected: the first command is absent in the current baseline; the final command prints `10.0.302`. If Winget does not expose that exact build, use the official .NET 10.0.302 Windows x64 installer and verify the same output before continuing.

- [ ] **Step 2: Pin SDK and central build settings**

Create `global.json`:

```json
{
  "sdk": {
    "version": "10.0.302",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
```

Create `Directory.Build.props` with `TargetFramework=net10.0`, `LangVersion=14.0`, nullable enabled, implicit usings enabled, deterministic builds, and warnings as errors outside generated migration files.

- [ ] **Step 3: Pin packages centrally**

Create `Directory.Packages.props` with central package management and these exact stable versions:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.10" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.10" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.8.1" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageVersion Include="Microsoft.Playwright.Xunit" Version="1.61.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Scaffold the solution and references**

Run these commands, add every generated project to `KpiManagement.slnx`, then add references so dependency direction is exactly:

```powershell
dotnet new sln --name KpiManagement --format slnx
dotnet new classlib --name Kpi.Domain --output src/Kpi.Domain
dotnet new classlib --name Kpi.Application --output src/Kpi.Application
dotnet new classlib --name Kpi.Infrastructure.Postgres --output src/Kpi.Infrastructure.Postgres
dotnet new mvc --name Kpi.Web --output src/Kpi.Web --auth None
dotnet new xunit --name Kpi.Domain.Tests --output tests/Kpi.Domain.Tests
dotnet new xunit --name Kpi.Application.Tests --output tests/Kpi.Application.Tests
dotnet new xunit --name Kpi.IntegrationTests --output tests/Kpi.IntegrationTests
dotnet new xunit --name Kpi.Web.EndToEndTests --output tests/Kpi.Web.EndToEndTests
dotnet sln KpiManagement.slnx add src/Kpi.Domain src/Kpi.Application src/Kpi.Infrastructure.Postgres src/Kpi.Web tests/Kpi.Domain.Tests tests/Kpi.Application.Tests tests/Kpi.IntegrationTests tests/Kpi.Web.EndToEndTests
```

```text
Kpi.Application -> Kpi.Domain
Kpi.Infrastructure.Postgres -> Kpi.Application + Kpi.Domain
Kpi.Web -> Kpi.Application + Kpi.Infrastructure.Postgres
Kpi.Domain.Tests -> Kpi.Domain
Kpi.Application.Tests -> Kpi.Application + Kpi.Domain
Kpi.IntegrationTests -> Kpi.Web + Kpi.Infrastructure.Postgres
Kpi.Web.EndToEndTests -> Kpi.Web
```

- [ ] **Step 5: Write and verify a failing baseline assembly test**

Create `AssemblyBoundaryTests.cs` first. It references the not-yet-created public marker and verifies that Domain has no ASP.NET Core, EF Core, or Npgsql assembly reference:

```csharp
[Fact]
public void Domain_has_no_framework_or_persistence_dependency()
{
    var references = typeof(Kpi.Domain.AssemblyMarker).Assembly
        .GetReferencedAssemblies()
        .Select(reference => reference.Name)
        .ToArray();

    Assert.DoesNotContain(references, name =>
        name is not null &&
        (name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
         name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
         name.StartsWith("Npgsql", StringComparison.Ordinal)));
}
```

Run:

```powershell
dotnet test tests/Kpi.Domain.Tests/Kpi.Domain.Tests.csproj
```

Expected RED: compile fails because `Kpi.Domain.AssemblyMarker` does not exist.

- [ ] **Step 6: Make the scaffold green, lock dependencies, and wire the harness**

Add the minimal marker:

```csharp
namespace Kpi.Domain;

public static class AssemblyMarker;
```

Generate lock files with `dotnet restore KpiManagement.slnx --use-lock-file`. Then configure bootstrap to use `dotnet restore KpiManagement.slnx --locked-mode`, lint to run `dotnet format KpiManagement.slnx --verify-no-changes` plus `dotnet build KpiManagement.slnx --no-restore`, and test to run the focused .NET test projects. Create ADR 0002 explaining alignment with the larger C#/.cshtml/PostgreSQL application and update `docs/architecture.md`. Run:

```powershell
./harness.cmd check
```

Expected GREEN: restore, format, build, branch policy test, and baseline .NET test pass.

- [ ] **Step 7: Commit the toolchain slice**

```powershell
git add global.json KpiManagement.slnx Directory.Build.props Directory.Packages.props src tests .harness .gitignore docs/architecture.md docs/decisions/0002-kpi-application-stack.md
git commit -m "build: add KPI application toolchain"
```

### Task 2: Define Formula contracts and tokenize source with exact spans

**Files:**
- Create: `src/Kpi.Domain/Formula/FormulaValue.cs`
- Create: `src/Kpi.Domain/Formula/FormulaResultType.cs`
- Create: `src/Kpi.Domain/Formula/FormulaVariableDefinition.cs`
- Create: `src/Kpi.Domain/Formula/FormulaDiagnostic.cs`
- Create: `src/Kpi.Domain/Formula/SourceSpan.cs`
- Create: `src/Kpi.Domain/Formula/Token.cs`
- Create: `src/Kpi.Domain/Formula/Tokenizer.cs`
- Test: `tests/Kpi.Domain.Tests/Formula/TokenizerTests.cs`
- Test: `tests/Kpi.Domain.Tests/Formula/FormulaVariableDefinitionTests.cs`

**Interfaces:**
- Consumes: no application or infrastructure types.
- Produces: `FormulaValue`, `FormulaResultType`, `FormulaVariableDefinition`, `SourceSpan`, `TokenKind`, and internal `Tokenizer.Tokenize(string)`.

- [ ] **Step 1: Write RED tests for variables and token spans**

Tests must prove that `revenue >= 25% AND active` yields identifier, comparison, decimal, percent, logical keyword, identifier, and EOF tokens with literal hand-checked spans; duplicate/case-conflicting variable codes and non-snake-case codes are rejected. Required variables without a value/default and every explicit Null input are rejected before evaluation.

```csharp
[Fact]
public void Tokenize_preserves_operator_and_percentage_spans()
{
    var tokens = Tokenizer.Tokenize("revenue >= 25% AND active");
    Assert.Equal(TokenKind.GreaterThanOrEqual, tokens[1].Kind);
    Assert.Equal(new SourceSpan(8, 2), tokens[1].Span);
    Assert.Equal(TokenKind.Percent, tokens[3].Kind);
    Assert.Equal(new SourceSpan(13, 1), tokens[3].Span);
}
```

- [ ] **Step 2: Verify RED**

```powershell
dotnet test tests/Kpi.Domain.Tests/Kpi.Domain.Tests.csproj --filter "FullyQualifiedName~TokenizerTests|FullyQualifiedName~FormulaVariableDefinitionTests"
```

Expected: compile failure because Formula contracts and Tokenizer do not exist.

- [ ] **Step 3: Implement minimal contracts and tokenizer**

Use discriminated records:

```csharp
public abstract record FormulaValue;
public sealed record DecimalFormulaValue(decimal Value) : FormulaValue;
public sealed record BooleanFormulaValue(bool Value) : FormulaValue;
public readonly record struct SourceSpan(int Start, int Length);
```

Tokenizer recognizes invariant Decimal literals, Boolean literals, case-insensitive keywords, identifiers, punctuation, comparison/arithmetic symbols, and EOF. Invalid characters return diagnostics with spans rather than throwing unstructured exceptions.

- [ ] **Step 4: Verify GREEN and refactor tables**

Run the focused tests, then all Domain tests. Keep keyword/operator lookup tables internal and immutable.

- [ ] **Step 5: Commit**

```powershell
git add src/Kpi.Domain/Formula tests/Kpi.Domain.Tests/Formula
git commit -m "feat: tokenize KPI formulas"
```

### Task 3: Parse, type-check, and serialize the versioned AST

**Files:**
- Create: `src/Kpi.Domain/Formula/Ast/FormulaNode.cs`
- Create: `src/Kpi.Domain/Formula/Parser.cs`
- Create: `src/Kpi.Domain/Formula/TypeChecker.cs`
- Create: `src/Kpi.Domain/Formula/CompiledFormula.cs`
- Create: `src/Kpi.Domain/Formula/FormulaCompilation.cs`
- Create: `src/Kpi.Domain/Formula/FormulaEngine.cs`
- Create: `src/Kpi.Domain/Formula/Serialization/FormulaAstJsonContext.cs`
- Test: `tests/Kpi.Domain.Tests/Formula/ParserTests.cs`
- Test: `tests/Kpi.Domain.Tests/Formula/TypeCheckerTests.cs`
- Test: `tests/Kpi.Domain.Tests/Formula/FormulaSerializationTests.cs`

**Interfaces:**
- Consumes: Task 2 Formula contracts and tokens.
- Produces: `FormulaEngine.Compile(source, variables, expectedResultType)`, typed AST nodes, `FormulaCompilation`, and stable AST JSON schema version 1.

- [ ] **Step 1: Write RED precedence and typing tests**

Cover `1 + 2 * 3`, postfix `%`, unary `NOT`, comparison before `AND`, `IF` same-branch types, missing variables, mismatched expected result type, maximum depth, and exact source preservation.

```csharp
[Fact]
public void Compile_applies_multiplication_before_addition()
{
    var result = FormulaEngine.Compile("1 + 2 * 3", [], FormulaResultType.Decimal);
    var add = Assert.IsType<BinaryNode>(result.Formula!.Root);
    Assert.Equal(BinaryOperator.Add, add.Operator);
    Assert.Equal(BinaryOperator.Multiply, Assert.IsType<BinaryNode>(add.Right).Operator);
}
```

- [ ] **Step 2: Verify RED**

Run the three new test classes and observe compile failure for absent parser/AST types.

- [ ] **Step 3: Implement Pratt parser and typed AST**

Create closed node records for decimal/boolean literal, variable, unary, binary, percentage, and call. Every node contains `NodeType`, `ResultType`, and `SourceSpan`. Parser binding powers implement the exact precedence from the spec. TypeChecker resolves variables case-insensitively and produces stable diagnostic codes.

- [ ] **Step 4: Implement public read serialization**

Serialize AST with explicit `nodeType` discriminators and Decimal literals as invariant strings. Preserve source independently:

```csharp
public sealed record FormulaDocument(string Source, FormulaNode Ast);
public sealed record CompiledFormula(
    FormulaDocument Formula,
    int FormulaLanguageVersion,
    int AstSchemaVersion);
```

- [ ] **Step 5: Verify GREEN and golden JSON**

Run Domain tests twice: once normally and once after serializing/deserializing the golden formula. Assert source equality, AST structural equality, variable order, and version fields.

- [ ] **Step 6: Commit**

```powershell
git add src/Kpi.Domain/Formula tests/Kpi.Domain.Tests/Formula
git commit -m "feat: compile typed KPI formula AST"
```

### Task 4: Evaluate formulas deterministically with structured outcomes

**Files:**
- Create: `src/Kpi.Domain/Formula/EvaluationOutcome.cs`
- Create: `src/Kpi.Domain/Formula/FormulaEvaluator.cs`
- Create: `src/Kpi.Domain/Formula/EvaluationBudget.cs`
- Test: `tests/Kpi.Domain.Tests/Formula/EvaluatorArithmeticTests.cs`
- Test: `tests/Kpi.Domain.Tests/Formula/EvaluatorLogicTests.cs`
- Test: `tests/Kpi.Domain.Tests/Formula/EvaluatorFailureTests.cs`

**Interfaces:**
- Consumes: `CompiledFormula` and keyed `FormulaValue` inputs.
- Produces: `FormulaEngine.Evaluate(compiledFormula, inputs)` returning `EvaluationSuccess` or `EvaluationFailure`.

- [ ] **Step 1: Write RED table tests**

Hand-check expected results for `+ - * /`, unary minus, postfix percentage, `MOD`, `ABS`, `ROUND`, comparisons, `AND`, `OR`, `NOT`, and `IF`. Include `IF(false, 1 / 0, 10)` returning 10 and `false AND missing_variable` short-circuiting to false.

- [ ] **Step 2: Write RED failure and budget tests**

Prove stable codes for divide-by-zero, missing input, wrong type, overflow, scale above 10, node budget, and elapsed budget. Prove a Failure never contains a successful value.

- [ ] **Step 3: Verify RED**

Run focused evaluator tests and observe missing evaluator types.

- [ ] **Step 4: Implement minimal recursive evaluator**

Use a budget checked before every node. `IF`, `AND`, and `OR` control which child nodes execute. Map expected conditions to codes such as `FORMULA_DIVISION_BY_ZERO`, `FORMULA_INPUT_MISSING`, `FORMULA_TYPE_MISMATCH`, and `FORMULA_LIMIT_EXCEEDED`.

```csharp
public static EvaluationOutcome Evaluate(
    CompiledFormula formula,
    IReadOnlyDictionary<string, FormulaValue> inputs,
    EvaluationBudget? budget = null);
```

- [ ] **Step 5: Verify GREEN and mutation cases**

Run Domain tests; mentally mutate short-circuit branches, operator mapping, and midpoint rounding and confirm named tests would fail.

- [ ] **Step 6: Commit**

```powershell
git add src/Kpi.Domain/Formula tests/Kpi.Domain.Tests/Formula
git commit -m "feat: evaluate KPI formulas safely"
```

### Task 5: Implement KPI Definition and Version governance

**Files:**
- Create: `src/Kpi.Domain/Common/DomainError.cs`
- Create: `src/Kpi.Domain/Common/Result.cs`
- Create: `src/Kpi.Domain/Kpis/KpiDefinition.cs`
- Create: `src/Kpi.Domain/Kpis/KpiVersion.cs`
- Create: `src/Kpi.Domain/Kpis/KpiVersionStatus.cs`
- Create: `src/Kpi.Domain/Kpis/KpiCadence.cs`
- Create: `src/Kpi.Domain/Kpis/KpiVersionContent.cs`
- Create: `src/Kpi.Domain/Organizations/Organization.cs`
- Create: `src/Kpi.Domain/Organizations/Actor.cs`
- Create: `src/Kpi.Domain/Organizations/ActorCapability.cs`
- Test: `tests/Kpi.Domain.Tests/Kpis/KpiDefinitionTests.cs`
- Test: `tests/Kpi.Domain.Tests/Kpis/KpiVersionLifecycleTests.cs`
- Test: `tests/Kpi.Domain.Tests/Organizations/OrganizationActorTests.cs`

**Interfaces:**
- Consumes: compiled Formula document, variable schema, declared result type.
- Produces: aggregates enforcing immutable code, Draft-only editing, review/publication transitions, retirement, archive/restore, ownership transfer, and Draft deletion eligibility.

- [ ] **Step 1: Write RED aggregate tests**

Test immutable organization-scoped code, actor organization/capabilities, sequential version numbers, required name/description/change summary, Draft edits, submit, approve/reject comment, publish effective date, non-overlapping effective ranges, automatic predecessor retirement when a successor becomes due, clone retired version, archive/restore, ownership transfer reason, and hard-delete eligibility.

- [ ] **Step 2: Verify RED**

Run `dotnet test` filtered to `Kpis` and observe missing aggregate types.

- [ ] **Step 3: Implement aggregate methods**

Expose intent methods rather than public setters:

```csharp
public Result Submit(Guid actorId, DateTimeOffset at);
public Result Approve(Guid approverId, string comment, DateTimeOffset at);
public Result Publish(Guid actorId, DateOnly effectiveFrom, DateTimeOffset at);
public Result Retire(Guid actorId, string reason, DateTimeOffset at);
```

Return stable DomainError codes for forbidden transitions. Store domain events for application-level audit creation.

- [ ] **Step 4: Verify GREEN**

Run all Domain tests and ensure Published content cannot be changed through any public interface.

- [ ] **Step 5: Commit**

```powershell
git add src/Kpi.Domain/Common src/Kpi.Domain/Kpis src/Kpi.Domain/Organizations tests/Kpi.Domain.Tests/Kpis tests/Kpi.Domain.Tests/Organizations
git commit -m "feat: govern KPI definitions and versions"
```

### Task 6: Implement KPI Period planning, approval, and reconciliation

**Files:**
- Create: `src/Kpi.Domain/Periods/KpiPeriod.cs`
- Create: `src/Kpi.Domain/Periods/KpiPeriodStatus.cs`
- Create: `src/Kpi.Domain/Periods/KpiPeriodActivation.cs`
- Create: `src/Kpi.Domain/Periods/KpiPeriodAmendment.cs`
- Create: `src/Kpi.Application/Time/IClock.cs`
- Create: `src/Kpi.Application/Periods/ReconcilePeriods.cs`
- Create: `src/Kpi.Application/Periods/PeriodTransition.cs`
- Create: `src/Kpi.Application/Kpis/ReconcilePublishedVersions.cs`
- Create: `src/Kpi.Application/Kpis/KpiVersionTransition.cs`
- Test: `tests/Kpi.Domain.Tests/Periods/KpiPeriodLifecycleTests.cs`
- Test: `tests/Kpi.Domain.Tests/Periods/KpiPeriodEligibilityTests.cs`
- Test: `tests/Kpi.Domain.Tests/Periods/KpiPeriodOverlapTests.cs`
- Test: `tests/Kpi.Application.Tests/Periods/ReconcilePeriodsTests.cs`
- Test: `tests/Kpi.Application.Tests/Kpis/ReconcilePublishedVersionsTests.cs`

**Interfaces:**
- Consumes: published eligible KPI Versions, cadence, company timezone, injected clock.
- Produces: `KpiPeriod` aggregate, pure idempotent `ReconcilePeriods.Execute(periods, now)`, and `ReconcilePublishedVersions.Execute(definitions, now)` operations. Persistence and transaction boundaries are added in Task 8.

- [ ] **Step 1: Write RED lifecycle and separation tests**

Cover Draft → InReview → Scheduled → Active → Closed, rejection, cancellation, planner/approver separation, frozen approved selections, and audited amendment requirement.

- [ ] **Step 2: Write RED eligibility/overlap tests**

Cover one version per Definition, newest-to-oldest selection eligibility, disabled retired or not-yet-effective versions, matching cadence, same-cadence overlap rejection, and same Definition overlap rejection. Cover a due successor retiring the predecessor exactly once and a future successor remaining non-current.

- [ ] **Step 3: Verify RED**

Run Period test classes and observe missing aggregate/command types.

- [ ] **Step 4: Implement lifecycle and reconciliation**

Use half-open intervals `[start, end)` and convert configured `Asia/Ho_Chi_Minh` local boundaries to UTC once. Both reconciliation operations accept loaded aggregates, return transition events for their caller to persist atomically, and are no-ops when rerun at the same instant.

```csharp
public static IReadOnlyList<PeriodTransition> Execute(
    IReadOnlyCollection<KpiPeriod> periods,
    DateTimeOffset now);

public static IReadOnlyList<KpiVersionTransition> Execute(
    IReadOnlyCollection<KpiDefinition> definitions,
    DateTimeOffset now);
```

- [ ] **Step 5: Verify GREEN**

Run Domain and Application tests with a fake clock; rerun period and version reconciliation and assert no duplicate transitions.

- [ ] **Step 6: Commit**

```powershell
git add src/Kpi.Domain/Periods src/Kpi.Application/Time src/Kpi.Application/Periods src/Kpi.Application/Kpis tests/Kpi.Domain.Tests/Periods tests/Kpi.Application.Tests/Periods tests/Kpi.Application.Tests/Kpis
git commit -m "feat: govern KPI periods"
```

### Task 7: Implement immutable evaluations, corrections, and audit records

**Files:**
- Create: `src/Kpi.Domain/Evaluations/KpiEvaluation.cs`
- Create: `src/Kpi.Domain/Evaluations/EvaluationInputSnapshot.cs`
- Create: `src/Kpi.Domain/Evaluations/EvaluationCorrectionDiff.cs`
- Create: `src/Kpi.Domain/Auditing/AuditRecord.cs`
- Create: `src/Kpi.Application/Evaluations/EvaluateActivatedKpi.cs`
- Create: `src/Kpi.Application/Evaluations/CorrectEvaluation.cs`
- Create: `src/Kpi.Application/Formulas/TestFormula.cs`
- Test: `tests/Kpi.Domain.Tests/Evaluations/KpiEvaluationTests.cs`
- Test: `tests/Kpi.Domain.Tests/Evaluations/EvaluationCorrectionTests.cs`
- Test: `tests/Kpi.Application.Tests/Evaluations/EvaluateActivatedKpiTests.cs`
- Test: `tests/Kpi.Application.Tests/Formulas/TestFormulaTests.cs`

**Interfaces:**
- Consumes: Active `KpiPeriodActivation`, compiled Formula, evaluator inputs, actor, and clock.
- Produces: pure application operations returning immutable attempt/correction results plus a transient `TestFormula.Execute(...)` result. Task 8 persists official returned attempts and never persists Test Run results.

- [ ] **Step 1: Write RED official-attempt tests**

Prove inactive activations reject evaluation, defaults enter the snapshot, source/version/input/outcome are immutable, and a Failure after Success does not replace Current.

- [ ] **Step 2: Write RED correction tests**

Prove same-version requirement, mandatory reason, literal old/new input diff, result delta, supersedes link, and old attempt retention.

- [ ] **Step 3: Verify RED**

Run Evaluation test classes and observe missing types.

- [ ] **Step 4: Implement commands and audit metadata**

`EvaluateActivatedKpi` compiles only the stored trusted Formula document and returns every official attempt for the caller to persist. `CorrectEvaluation` recomputes from a full new input snapshot and derives the diff server-side. `TestFormula` accepts Draft source/variables and returns only a compilation/evaluation response without exposing a persistence port.

```csharp
public Result<KpiEvaluation> Execute(EvaluateActivatedKpiRequest request);
public Result<KpiEvaluation> Execute(CorrectEvaluationRequest request);
public FormulaTestRunResponse Execute(FormulaTestRunRequest request);
```

- [ ] **Step 5: Verify GREEN and transient test separation**

Add an Application test proving `TestFormula` returns the hand-checked result and has no `IKpiEvaluationStore` constructor dependency or persisted identifier. Run all Domain/Application tests.

- [ ] **Step 6: Commit**

```powershell
git add src/Kpi.Domain/Evaluations src/Kpi.Domain/Auditing src/Kpi.Application/Evaluations src/Kpi.Application/Formulas tests/Kpi.Domain.Tests/Evaluations tests/Kpi.Application.Tests/Evaluations tests/Kpi.Application.Tests/Formulas
git commit -m "feat: preserve KPI evaluation history"
```

### Task 8: Persist aggregates and JSONB round-trips in PostgreSQL

**Files:**
- Create: `src/Kpi.Application/Persistence/IKpiDefinitionStore.cs`
- Create: `src/Kpi.Application/Persistence/IKpiPeriodStore.cs`
- Create: `src/Kpi.Application/Persistence/IKpiEvaluationStore.cs`
- Create: `src/Kpi.Application/Persistence/IAuditStore.cs`
- Create: `src/Kpi.Application/Persistence/IUnitOfWork.cs`
- Create: `src/Kpi.Infrastructure.Postgres/KpiDbContext.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Configurations/OrganizationConfiguration.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Configurations/ActorConfiguration.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Configurations/KpiDefinitionConfiguration.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Configurations/KpiVersionConfiguration.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Configurations/KpiPeriodConfiguration.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Configurations/KpiPeriodActivationConfiguration.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Configurations/KpiEvaluationConfiguration.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Configurations/AuditRecordConfiguration.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Stores/PostgresKpiDefinitionStore.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Stores/PostgresKpiPeriodStore.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Stores/PostgresKpiEvaluationStore.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Stores/PostgresAuditStore.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Migrations/202608090001_InitialKpiSchema.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Migrations/202608090001_InitialKpiSchema.Designer.cs`
- Create: `src/Kpi.Infrastructure.Postgres/Migrations/KpiDbContextModelSnapshot.cs`
- Create: `src/Kpi.Infrastructure.Postgres/DependencyInjection.cs`
- Test: `tests/Kpi.IntegrationTests/DatabaseFixture.cs`
- Test: `tests/Kpi.IntegrationTests/FormulaRoundTripTests.cs`
- Test: `tests/Kpi.IntegrationTests/EvaluationRoundTripTests.cs`
- Test: `tests/Kpi.IntegrationTests/AuditPersistenceTests.cs`
- Test: `tests/Kpi.IntegrationTests/ConcurrencyTests.cs`

**Interfaces:**
- Consumes: Domain aggregates and application persistence ports.
- Produces: EF/Npgsql adapters, migrations, relational constraints, JSONB formula/variables/input/error/diff mappings.

- [ ] **Step 1: Configure a dedicated test connection without committing secrets**

Use `KPI_TEST_DB_CONNECTION` or user-secrets. Create `kpi_lab_test` through the existing PostgreSQL 18.4 service and verify `SELECT version();` reports PostgreSQL 18.x.

- [ ] **Step 2: Write RED PostgreSQL round-trip tests**

Persist a formula with nontrivial spacing, ordered variables, typed AST, Success, Failure, and correction chain; clear the DbContext; reload and assert literal equality for source/order/value strings and structural equality for AST/diffs. Prove supported store interfaces can append and read audit rows but expose no update/delete operation. Execute raw `UPDATE` and `DELETE` statements against `audit_records` and assert the migration-installed append-only trigger rejects both.

- [ ] **Step 3: Verify RED**

Run Integration tests and observe missing DbContext/migration/store behavior.

- [ ] **Step 4: Implement mappings and migration**

Keep identities/status/ownership/cadence/dates relational. Map Formula document, ordered variable array, input snapshot, failure details, correction diff, and audit summary to `jsonb`. Add unique constraints and concurrency tokens from the spec. The initial migration creates a trigger function that raises SQLSTATE `55000` for every `UPDATE` or `DELETE` on `audit_records`.

```csharp
builder.Property(version => version.Formula)
    .HasColumnType("jsonb");
builder.Property(evaluation => evaluation.InputSnapshot)
    .HasColumnType("jsonb");
```

- [ ] **Step 5: Verify GREEN from an empty database**

Resolve the configured database name and stop if it is not exactly `kpi_lab_test`. Then drop only that dedicated test database, recreate it, apply migrations, run Integration tests twice, and confirm the second run cleans only its own test data.

- [ ] **Step 6: Commit**

```powershell
git add src/Kpi.Application/Persistence src/Kpi.Infrastructure.Postgres tests/Kpi.IntegrationTests
git commit -m "feat: persist KPI history in PostgreSQL"
```

### Task 9: Expose versioned REST JSON and localized ProblemDetails

**Files:**
- Create: `src/Kpi.Application/Kpis/KpiDefinitionOperations.cs`
- Create: `src/Kpi.Application/Kpis/KpiVersionOperations.cs`
- Create: `src/Kpi.Application/Periods/KpiPeriodOperations.cs`
- Create: `src/Kpi.Application/Auditing/AuditQueries.cs`
- Create: `src/Kpi.Application/Reconciliation/KpiTimeReconciliationOperation.cs`
- Create: `src/Kpi.Web/Api/V1/FormulaController.cs`
- Create: `src/Kpi.Web/Api/V1/KpisController.cs`
- Create: `src/Kpi.Web/Api/V1/KpiVersionsController.cs`
- Create: `src/Kpi.Web/Api/V1/KpiPeriodsController.cs`
- Create: `src/Kpi.Web/Api/V1/KpiEvaluationsController.cs`
- Create: `src/Kpi.Web/Api/V1/AuditController.cs`
- Create: `src/Kpi.Web/Api/V1/Contracts/FormulaContracts.cs`
- Create: `src/Kpi.Web/Api/V1/Contracts/KpiContracts.cs`
- Create: `src/Kpi.Web/Api/V1/Contracts/PeriodContracts.cs`
- Create: `src/Kpi.Web/Api/V1/Contracts/EvaluationContracts.cs`
- Create: `src/Kpi.Web/Api/V1/Contracts/AuditContracts.cs`
- Create: `src/Kpi.Web/Api/ApiProblemDetailsFactory.cs`
- Create: `src/Kpi.Web/Serialization/FormulaJsonContext.cs`
- Test: `tests/Kpi.IntegrationTests/Api/FormulaApiTests.cs`
- Test: `tests/Kpi.IntegrationTests/Api/KpiWorkflowApiTests.cs`
- Test: `tests/Kpi.IntegrationTests/Api/ProblemDetailsTests.cs`

**Interfaces:**
- Consumes: Application commands/queries and persisted ports.
- Produces: `/api/v1` read/write contracts, server-generated AST, typed Decimal strings, stable error codes/spans, and 409 concurrency handling.

- [ ] **Step 1: Write RED API contract tests**

Use `WebApplicationFactory` and real PostgreSQL. Prove validate accepts source but not trusted AST, GET returns `{formula:{source,ast}, formulaLanguageVersion, astSchemaVersion}`, Decimal is a string, and stale tokens return 409.

- [ ] **Step 2: Write RED workflow and ProblemDetails tests**

Exercise Definition/Version/Period/Evaluation commands and literal ProblemDetails shape with 400/404/409/422 behavior. Delete an eligible never-submitted Draft and assert its content is gone while an audit tombstone with identity, actor, timestamp, and reason remains; assert historical versions reject hard deletion. Publish a future successor and reconcile its due instant, proving predecessor retirement and no duplicate audit on a repeat call.

- [ ] **Step 3: Verify RED**

Run Integration API tests and observe missing routes/controllers.

- [ ] **Step 4: Implement thin controllers and DTO mapping**

Controllers validate transport shape, call one Application operation, and map Result/DomainError through `ApiProblemDetailsFactory`. They contain no lifecycle or formula rules.

```csharp
[HttpPost("validate")]
public ActionResult<FormulaValidationResponse> Validate(
    FormulaValidationRequest request,
    CancellationToken cancellationToken);
```

- [ ] **Step 5: Verify GREEN**

Run all Integration tests and serialize responses twice to confirm deterministic AST/property contracts.

- [ ] **Step 6: Commit**

```powershell
git add src/Kpi.Application src/Kpi.Web/Api src/Kpi.Web/Serialization tests/Kpi.IntegrationTests/Api
git commit -m "feat: expose KPI REST API"
```

### Task 10: Build the Bootstrap KPI list and Draft formula editor

**Files:**
- Create: `src/Kpi.Web/Controllers/KpisController.cs`
- Create: `src/Kpi.Web/Models/Kpis/KpiListViewModel.cs`
- Create: `src/Kpi.Web/Models/Kpis/KpiDraftEditorViewModel.cs`
- Create: `src/Kpi.Web/Views/Shared/_Layout.cshtml`
- Create: `src/Kpi.Web/Views/Kpis/Index.cshtml`
- Create: `src/Kpi.Web/Views/Kpis/EditDraft.cshtml`
- Create: `src/Kpi.Web/Views/Kpis/_VariableCard.cshtml`
- Create: `src/Kpi.Web/Views/Kpis/_FormulaReference.cshtml`
- Create: `src/Kpi.Web/wwwroot/js/formula-editor.js`
- Create: `src/Kpi.Web/wwwroot/css/site.css`
- Create: `src/Kpi.Web/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css`
- Create: `src/Kpi.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.min.js`
- Test: `tests/Kpi.IntegrationTests/Web/KpiPageTests.cs`
- Test: `tests/Kpi.Web.EndToEndTests/FormulaEditorTests.cs`

**Interfaces:**
- Consumes: REST/Application formula validation and Draft commands.
- Produces: Vietnamese KPI list/search/filter and three-region Draft editor with ordered variables, insertion, diagnostics, AST preview, and transient Test Run.

- [ ] **Step 1: Write RED rendered-page tests**

Assert semantic headings, labels, Bootstrap structure, development persona selector region, formula regions, Add Variable fields, and accessible diagnostic container from a real MVC response.

- [ ] **Step 2: Write RED browser interaction test**

Enter variables and a formula, click variable/function insert actions, wait for debounced validation, see AST preview, run a test, reload, and prove Test Run did not create an Evaluation row.

- [ ] **Step 3: Verify RED**

Run Web and Playwright focused tests and observe missing route/page behavior.

- [ ] **Step 4: Implement Razor/Bootstrap and vanilla JS**

Use server-rendered view models. Verify the local CSS banner contains `Bootstrap v5.3.8` and vendor the matching official 5.3.8 CSS/bundle assets when the SDK template differs. JavaScript maintains ordered cards, inserts text at selection range, debounces `/api/v1/formulas/validate`, and renders API diagnostics by source span without duplicating formula semantics.

```javascript
export function insertAtSelection(textarea, text) {
  const start = textarea.selectionStart;
  const end = textarea.selectionEnd;
  textarea.setRangeText(text, start, end, "end");
  textarea.dispatchEvent(new Event("input", { bubbles: true }));
}
```

- [ ] **Step 5: Verify GREEN at localhost**

Run the web app, focused browser tests, and manual keyboard navigation for variable creation, formula insertion, validation, and test result display.

- [ ] **Step 6: Commit**

```powershell
git add src/Kpi.Web/Controllers src/Kpi.Web/Views src/Kpi.Web/wwwroot tests/Kpi.IntegrationTests/Web tests/Kpi.Web.EndToEndTests/FormulaEditorTests.cs
git commit -m "feat: add KPI formula authoring UI"
```

### Task 11: Build governance pages, personas, localization, and scheduled transitions

**Files:**
- Create: `src/Kpi.Web/Development/DevelopmentPersonaProvider.cs`
- Create: `src/Kpi.Web/Development/DevelopmentSeedData.cs`
- Create: `src/Kpi.Web/HostedServices/KpiTimeReconciliationWorker.cs`
- Create: `src/Kpi.Web/Controllers/KpiReviewsController.cs`
- Create: `src/Kpi.Web/Controllers/KpiPeriodsController.cs`
- Create: `src/Kpi.Web/Controllers/KpiEvaluationsController.cs`
- Create: `src/Kpi.Web/Controllers/AuditController.cs`
- Create: `src/Kpi.Web/Models/KpiReviews/KpiReviewViewModels.cs`
- Create: `src/Kpi.Web/Models/KpiPeriods/KpiPeriodViewModels.cs`
- Create: `src/Kpi.Web/Models/KpiEvaluations/KpiEvaluationViewModels.cs`
- Create: `src/Kpi.Web/Models/Audit/AuditViewModels.cs`
- Create: `src/Kpi.Web/Views/KpiReviews/Queue.cshtml`
- Create: `src/Kpi.Web/Views/KpiReviews/Details.cshtml`
- Create: `src/Kpi.Web/Views/KpiPeriods/Index.cshtml`
- Create: `src/Kpi.Web/Views/KpiPeriods/Create.cshtml`
- Create: `src/Kpi.Web/Views/KpiPeriods/Edit.cshtml`
- Create: `src/Kpi.Web/Views/KpiPeriods/Details.cshtml`
- Create: `src/Kpi.Web/Views/KpiEvaluations/Create.cshtml`
- Create: `src/Kpi.Web/Views/KpiEvaluations/History.cshtml`
- Create: `src/Kpi.Web/Views/KpiEvaluations/Correct.cshtml`
- Create: `src/Kpi.Web/Views/Audit/Index.cshtml`
- Create: `src/Kpi.Web/Resources/SharedResource.vi-VN.resx`
- Create: `src/Kpi.Web/Resources/SharedResource.en-US.resx`
- Test: `tests/Kpi.IntegrationTests/Web/DevelopmentSafetyTests.cs`
- Test: `tests/Kpi.IntegrationTests/Web/ReconciliationWorkerTests.cs`
- Test: `tests/Kpi.Web.EndToEndTests/KpiWorkflowTests.cs`

**Interfaces:**
- Consumes: all API/Application workflow operations and injected clock.
- Produces: review/publish, period planning/approval, official evaluation/correction, audit timeline, localized UI, seeded personas, and idempotent worker.

- [ ] **Step 1: Write RED safety and worker tests**

Prove persona switcher exists only in Development, production startup rejects it, startup reconciliation catches overdue version and period transitions, periodic repeat creates no duplicate audit, and all six seeded actors have exact capabilities.

- [ ] **Step 2: Write RED end-to-end governance test**

Automate persona-separated create → review → publish → plan → approve → activate → evaluate → correct → audit → archive/restore. Also hard-delete a separate unused Draft and inspect its audit tombstone. Assert changed input and old/new result display.

- [ ] **Step 3: Verify RED**

Run focused Integration/Playwright tests and observe missing pages/providers/worker.

- [ ] **Step 4: Implement pages, resources, personas, and worker**

Keep controller actions thin. Resolve actor from Development persona provider only when environment is Development. Use stable domain codes with localized `.resx` messages. Worker invokes the same transactional `KpiTimeReconciliationOperation` used by integration tests, which delegates to both version and period reconciliation.

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await reconciliation.ExecuteAsync(stoppingToken);
    using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        await reconciliation.ExecuteAsync(stoppingToken);
    }
}
```

- [ ] **Step 5: Verify GREEN and English fallback**

Run focused tests in `vi-VN`, repeat key page tests in `en-US`, start with Production plus persona enabled and verify intentional startup failure, then run normal Development localhost.

- [ ] **Step 6: Commit**

```powershell
git add src/Kpi.Web/Development src/Kpi.Web/HostedServices src/Kpi.Web/Controllers src/Kpi.Web/Views src/Kpi.Web/Resources tests/Kpi.IntegrationTests/Web tests/Kpi.Web.EndToEndTests/KpiWorkflowTests.cs
git commit -m "feat: complete KPI governance workflow"
```

### Task 12: Complete harness verification, durable docs, and integration guide

**Files:**
- Create: `HUONG_DAN_TICH_HOP_KPI.txt`
- Create: `tests/harness/integration-guide.tests.ps1`
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `docs/quality.md`
- Modify: `.harness/harness.json`
- Modify: `.github/workflows/harness.yml`
- Modify: `docs/plans/2026-08-09-kpi-management.md`
- Test: all projects and harness steps

**Interfaces:**
- Consumes: completed application and every task's verification command.
- Produces: one reproducible setup/check path, CI parity, localhost instructions, extraction map, AI continuation prompts, and pushed `main`.

- [ ] **Step 1: Write the guide acceptance checklist before the guide**

Create `tests/harness/integration-guide.tests.ps1` to verify the guide names exact prerequisites, SDK/database setup, solution modules, package versions, migrations, secrets, harness commands, localhost URL, API examples, formula JSON, workflows, extraction mapping, provider seams, production persona removal, troubleshooting, and AI continuation context.

- [ ] **Step 2: Verify RED**

Run the guide checklist through `./harness.cmd test`; expected failure lists every missing guide section.

- [ ] **Step 3: Write the Vietnamese human-and-agent guide**

Use numbered commands a human can copy and explicit repository file pointers an AI can load. Include a final prompt template that instructs another AI to read `AGENTS.md`, `CONTEXT.md`, the design spec, ADR 0002, architecture, quality policy, and this plan before changing code.

- [ ] **Step 4: Complete harness and CI parity**

Ensure bootstrap restores locked packages, installs Playwright Chromium when absent,
and validates non-secret configuration without changing PostgreSQL schema. The
explicit `./harness.cmd migrate` action applies migrations only to explicitly
configured local/test databases. Lint performs format/build; test runs harness
policy, the migration-command contract, unit, integration, and Playwright smoke
tests.

### Phase 10 implementation status (2026-08-09)

The explicit migration composition is now implemented: `migrate` dispatches the
dedicated `Kpi.Migrator`, the ledger records SHA-256 checksums and skips matching
entries transactionally, and target/checksum failures use stable codes. Web
runtime composition uses `ConnectionStrings:KpiRuntime`; the migrator alone
uses `ConnectionStrings:KpiMigration`. The checked-in Development profile is
explicitly `InMemoryTest`, while a durable profile requires PostgreSQL and never
applies schema on Web startup. `KPI_POSTGRES_TESTS=1` is the opt-in evidence
profile for the real `kpi_lab_test` database; the default harness reports safe
skips without credentials.

- [ ] **Step 5: Run line-by-line spec coverage review**

Map every acceptance criterion in design section 19 to a passing test or manual localhost evidence. Add a focused RED/GREEN test for every uncovered behavior before proceeding.

- [ ] **Step 6: Run final verification**

```powershell
./harness.cmd bootstrap
./harness.cmd migrate
./harness.cmd status
./harness.cmd check
git diff --check
git status --short
```

Expected: all configured steps pass, no warnings/errors, and only intended committed files exist.

- [ ] **Step 7: Review, commit, and push main**

Use `requesting-code-review` or the repository `code-review` skill against the approved spec. Resolve findings through RED/GREEN tests. Then:

```powershell
git add --all
git commit -m "docs: add KPI integration guide"
git fetch origin
git rev-list --left-right --count main...origin/main
git push origin main
```

Push only when the divergence shows remote has no commits absent locally and a fresh `./harness.cmd check` succeeds.
