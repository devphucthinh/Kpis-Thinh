# .NET 9 Repository Downgrade Design

## Goal

Make the complete KPI Management repository build, test, and run with the
already-installed .NET SDK 9.0.315. The downgrade becomes the repository's
official toolchain baseline rather than a machine-local workaround.

## Scope

- Pin `global.json` to SDK 9.0.315.
- Change the shared target framework from `net10.0` to `net9.0`.
- Move Entity Framework Core, ASP.NET Core MVC testing, and
  Microsoft.Extensions to 9.0.16; move the Npgsql Entity Framework Core
  provider to 9.0.4.
- Regenerate every NuGet lock file through the canonical restore workflow.
- Update durable documentation that currently declares .NET 10 as the approved
  stack.
- Verify the repository with the canonical harness and launch the Web project
  with the explicit `InMemoryTest` persistence profile.

## Non-goals

- No business behavior, HTTP contract, UI, formula language, authorization, or
  database schema changes.
- No multi-targeting of .NET 9 and .NET 10.
- No PostgreSQL migration or durable database setup is required for the local
  smoke run.

## Implementation Design

The repository remains centrally configured. `global.json` owns SDK selection,
`Directory.Build.props` owns the target framework, and
`Directory.Packages.props` owns package versions. Project files should not gain
per-project framework or package-version overrides.

Package families tied to ASP.NET Core or Entity Framework Core must use 9.x
versions aligned with the two neighboring integration repositories. Package
lock files are generated output of that declared dependency graph and must be
refreshed rather than edited by hand. Framework-independent test and browser
tooling stays at its existing version unless restore proves it incompatible.

## Integration Repository Baseline

The local repositories `BSC-KPIs-API` and `BSC-KPIs` are read-only references
for this downgrade. Their existing uncommitted feature work is out of scope and
must not be modified.

| Concern | Repository baseline | Kpis-Thinh target |
| --- | --- | --- |
| SDK | Installed SDK is 9.0.315 | Pin 9.0.315 in `global.json` |
| Target framework | Both references target `net9.0` | Target `net9.0` centrally |
| ASP.NET Core packages | API uses 9.0.16 | Use 9.0.16 |
| Entity Framework Core | API uses 9.0.16 | Use 9.0.16 |
| Npgsql EF provider | API uses 9.0.4 | Use 9.0.4 |
| Nullable reference types | Enabled in both references | Keep enabled |
| Implicit global usings | Enabled in both references | Keep enabled |

`BSC-KPIs` relies primarily on the ASP.NET Core shared framework and does not
declare EF Core package versions. Therefore `BSC-KPIs-API` is authoritative for
backend package alignment. Existing test-framework and Playwright versions in
`Kpis-Thinh` remain unchanged when they support `net9.0`; only packages coupled
to the Microsoft runtime major version are downgraded.

Documentation references to the approved .NET stack must describe .NET 9 after
the change. The architecture boundaries, persistence profiles, and explicit
migration policy remain unchanged.

## Verification

1. `dotnet --version` resolves to 9.0.315 from the repository root.
2. `./harness.cmd bootstrap` restores locked dependencies and provisions the
   browser test dependency.
3. `./harness.cmd check` passes formatting, analyzer builds, automated tests,
   and repository contract checks.
4. Start `Kpi.Web` with `Kpi__PersistenceProfile=InMemoryTest` at
   `http://localhost:5080` and verify an HTTP success response.
5. Confirm `git diff` contains only coherent toolchain, dependency-lock, and
   documentation changes.

## Risks and Controls

- API differences between framework versions may cause compile failures. Fix
  only compatibility issues required by the downgrade and protect behavior with
  the existing tests.
- A mismatched Npgsql provider can break restore or EF integration. Keep EF Core
  and its Npgsql provider on the same major version.
- Locked restore fails until all lock files match the new graph. Use a one-time
  unlocked canonical `dotnet restore` to regenerate them, then prove locked
  restore through `bootstrap`.
