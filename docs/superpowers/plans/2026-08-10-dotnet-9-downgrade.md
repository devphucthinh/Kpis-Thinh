# .NET 9 Repository Downgrade Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Downgrade the complete Kpis-Thinh repository to the same .NET 9 framework and backend package family used by BSC-KPIs-API and BSC-KPIs, then prove the application runs locally.

**Architecture:** Keep SDK, target framework, and package versions centralized in the existing root configuration files. Regenerate NuGet lock files from those declarations, update runtime-coupled scripts and durable documentation, and preserve all domain, API, persistence, and UI behavior.

**Tech Stack:** .NET SDK 9.0.315, ASP.NET Core 9.0.16, Entity Framework Core 9.0.16, Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4, PowerShell harness, xUnit v3, Playwright.

## Global Constraints

- Pin SDK version 9.0.315 in `global.json`.
- Target `net9.0` centrally in `Directory.Build.props`.
- Use version 9.0.16 for Entity Framework Core, ASP.NET Core MVC testing, and Microsoft.Extensions packages.
- Use version 9.0.4 for Npgsql.EntityFrameworkCore.PostgreSQL.
- Keep nullable reference types and implicit global usings enabled.
- Keep current test-framework and Playwright versions when they restore for `net9.0`.
- Do not modify BSC-KPIs-API or BSC-KPIs.
- Do not change business behavior, HTTP contracts, UI behavior, authorization, formula rules, database schema, or persistence profiles.
- Do not run `./harness.cmd migrate`; the smoke run uses `InMemoryTest`.

---

### Task 1: Align the central toolchain and locked dependency graph

**Files:**
- Modify: `global.json`
- Modify: `Directory.Build.props`
- Modify: `Directory.Packages.props`
- Regenerate: `src/Kpi.Application/packages.lock.json`
- Regenerate: `src/Kpi.Domain/packages.lock.json`
- Regenerate: `src/Kpi.Infrastructure.Postgres/packages.lock.json`
- Regenerate: `src/Kpi.Migrator/packages.lock.json`
- Regenerate: `src/Kpi.Web/packages.lock.json`
- Regenerate: `tests/Kpi.Application.Tests/packages.lock.json`
- Regenerate: `tests/Kpi.Domain.Tests/packages.lock.json`
- Regenerate: `tests/Kpi.IntegrationTests/packages.lock.json`
- Regenerate: `tests/Kpi.Web.EndToEndTests/packages.lock.json`

**Interfaces:**
- Consumes: Version baseline recorded in `docs/superpowers/specs/2026-08-10-dotnet-9-downgrade-design.md`.
- Produces: A solution-wide `net9.0` dependency graph that restores under SDK 9.0.315 in locked mode.

- [ ] **Step 1: Reproduce the existing SDK incompatibility**

First inspect and preserve the existing working tree. Do not discard or stage
pre-existing changes:

```powershell
git status --short --branch
git diff -- src/Kpi.Infrastructure.Postgres/packages.lock.json src/Kpi.Migrator/packages.lock.json src/Kpi.Web/packages.lock.json tests/Kpi.Application.Tests/packages.lock.json tests/Kpi.Domain.Tests/packages.lock.json tests/Kpi.IntegrationTests/packages.lock.json tests/Kpi.Web.EndToEndTests/packages.lock.json
```

If any lock-file changes are still present, identify their owner and intent
before regeneration because Task 1 will replace the same generated graph.

Run:

```powershell
.\harness.cmd bootstrap
```

Expected: FAIL before restore because `global.json` requests 10.0.302 while only SDK 9.0.315 is installed.

- [ ] **Step 2: Pin the installed SDK and shared target framework**

Set `global.json` to:

```json
{
  "sdk": {
    "version": "9.0.315",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
```

Change the shared property in `Directory.Build.props` to:

```xml
<TargetFramework>net9.0</TargetFramework>
```

- [ ] **Step 3: Align runtime-coupled central package versions**

In `Directory.Packages.props`, set these exact declarations and leave the existing xUnit, test SDK, runner, and Playwright declarations unchanged:

```xml
<PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.0.16" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.16" />
<PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
<PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.16" />
<PackageVersion Include="Microsoft.Extensions.Configuration" Version="9.0.16" />
<PackageVersion Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.16" />
<PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="9.0.16" />
```

- [ ] **Step 4: Regenerate lock files from the central declarations**

Run:

```powershell
dotnet --version
dotnet restore KpiManagement.slnx --force-evaluate
```

Expected: SDK output is `9.0.315`; restore succeeds and every lock file changes its framework key to `net9.0` with the declared 9.x package family.

- [ ] **Step 5: Prove locked restore and compilation**

Run:

```powershell
dotnet restore KpiManagement.slnx --locked-mode
dotnet build KpiManagement.slnx --no-restore --configuration Release
```

Expected: both commands exit 0 with no warnings because warnings are treated as errors.

- [ ] **Step 6: Commit the toolchain graph**

```powershell
git add global.json Directory.Build.props Directory.Packages.props src/Kpi.Application/packages.lock.json src/Kpi.Domain/packages.lock.json src/Kpi.Infrastructure.Postgres/packages.lock.json src/Kpi.Migrator/packages.lock.json src/Kpi.Web/packages.lock.json tests/Kpi.Application.Tests/packages.lock.json tests/Kpi.Domain.Tests/packages.lock.json tests/Kpi.IntegrationTests/packages.lock.json tests/Kpi.Web.EndToEndTests/packages.lock.json
git commit -m "build: align repository with .NET 9 stack"
```

### Task 2: Align runtime tooling and durable repository documentation

**Files:**
- Modify: `scripts/provision-playwright.ps1`
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `docs/decisions/0002-kpi-application-stack.md`
- Modify: `docs/plans/2026-08-09-kpi-management.md`
- Modify: `docs/superpowers/plans/2026-08-09-kpi-ui-full-flow.md`
- Modify: `docs/superpowers/plans/2026-08-10-formula-language-catalog.md`
- Modify: `docs/superpowers/specs/2026-08-09-kpi-management-design.md`
- Modify: `HUONG_DAN_TICH_HOP_KPI.txt`
- Modify: `specs/001-kpi-management/plan.md`
- Modify: `specs/001-kpi-management/tasks.md`

**Interfaces:**
- Consumes: The `net9.0` build output path produced by Task 1.
- Produces: Playwright provisioning that locates the .NET 9 driver and documentation that consistently declares the approved .NET 9 stack.

- [ ] **Step 1: Find stale .NET 10 runtime references**

Run:

```powershell
rg -n 'net10\.0|10\.0\.302|10\.0\.2|10\.0\.0|\.NET 10|NET 10' -g '!**/bin/**' -g '!**/obj/**' .
```

Expected: matches include the Playwright driver path and the listed durable documentation.

- [ ] **Step 2: Update the Playwright driver output path**

In `scripts/provision-playwright.ps1`, replace the target-framework path segment only:

```powershell
net9.0
```

Preserve the existing project, configuration, driver filename, installation arguments, and failure handling.

- [ ] **Step 3: Update approved-stack documentation**

Replace declarations that prescribe .NET 10 or `net10.0` with .NET 9 or `net9.0` in every file listed for Task 2. Where a package version is stated, use ASP.NET Core/EF Core/Microsoft.Extensions 9.0.16 and Npgsql EF provider 9.0.4. Preserve historical dates, architecture boundaries, commands, acceptance criteria, and business scope.

- [ ] **Step 4: Verify stale references are intentional only**

Run:

```powershell
rg -n 'net10\.0|10\.0\.302|10\.0\.2|10\.0\.0|\.NET 10|NET 10' -g '!**/bin/**' -g '!**/obj/**' .
```

Expected: no stale operational reference remains; references in the downgrade design that describe the previous state or explicitly reject multi-targeting may remain.

- [ ] **Step 5: Verify formatting and build after tooling/documentation changes**

Run:

```powershell
.\harness.cmd lint
dotnet format KpiManagement.slnx --no-restore --verify-no-changes
git diff --check
```

Expected: all commands exit 0.

- [ ] **Step 6: Commit tooling and documentation alignment**

```powershell
git add scripts/provision-playwright.ps1 README.md docs/architecture.md docs/decisions/0002-kpi-application-stack.md docs/plans/2026-08-09-kpi-management.md docs/superpowers/plans/2026-08-09-kpi-ui-full-flow.md docs/superpowers/plans/2026-08-10-formula-language-catalog.md docs/superpowers/specs/2026-08-09-kpi-management-design.md HUONG_DAN_TICH_HOP_KPI.txt specs/001-kpi-management/plan.md specs/001-kpi-management/tasks.md
git commit -m "docs: align KPI stack guidance with .NET 9"
```

### Task 3: Run canonical verification and local Web smoke test

**Files:**
- No source files expected.
- Inspect only: `.harness/harness.json`
- Inspect only: `run-kpi.bat`

**Interfaces:**
- Consumes: The restored `net9.0` solution and updated Playwright path from Tasks 1 and 2.
- Produces: Passing canonical verification and an HTTP-success smoke result from the InMemory Web application.

- [ ] **Step 1: Run canonical bootstrap**

Run:

```powershell
.\harness.cmd bootstrap
```

Expected: locked restore, Release build, and pinned Chromium provisioning all exit 0.

- [ ] **Step 2: Run the full repository verification contract**

Run:

```powershell
.\harness.cmd check
```

Expected: formatting, analyzer build, all .NET tests, integration-guide contract, branch-policy tests, migration-command contract, and runtime-profile isolation contract all pass. PostgreSQL-only tests may report their documented deterministic skips because no migration profile is enabled.

- [ ] **Step 3: Start the Web project with explicit InMemory persistence**

Run the host in a background process with these environment values:

```text
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5080
Kpi__PersistenceProfile=InMemoryTest
```

Command:

```powershell
dotnet run --project src/Kpi.Web/Kpi.Web.csproj --configuration Release --no-restore
```

Expected: the process remains running and logs that it is listening on `http://localhost:5080`.

- [ ] **Step 4: Verify the local HTTP endpoint and stop the spawned host**

Run from a second process:

```powershell
$response = Invoke-WebRequest -Uri 'http://localhost:5080/' -UseBasicParsing -TimeoutSec 10
$response.StatusCode
```

Expected: `200`. Stop only the exact Web process started in Step 3 after capturing the result.

- [ ] **Step 5: Inspect the final repository state**

Run:

```powershell
git status --short --branch
git log -5 --oneline
```

Expected: branch is `main`, no uncommitted generated output remains, and the downgrade commits are visible.
