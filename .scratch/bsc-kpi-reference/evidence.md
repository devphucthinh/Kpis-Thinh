# BSC-KPI reference implementation evidence

## T011 — PostgreSQL organization authorization persistence

- Date: 2026-08-12
- Database target: `kpi_lab_test`
- Migration command: `./harness.cmd migrate`
- Migration result: all 9 declared migrations were already applied with matching checksums.
- Focused command: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --configuration Release --filter "FullyQualifiedName~OrganizationAuthorizationPostgresTests|FullyQualifiedName~KpiMigrationRunnerTests" -m:1`
- Result: 8 passed, 0 failed, 0 skipped.
- Covered behavior: migration ledger/checksum/idempotency, local target allow-list, composite Organization FK isolation, append-only approved baseline/segment facts, and PostgreSQL `xmin` optimistic concurrency.
- Composition boundary: `OrganizationAuthorizationSchemaTests` plus `PostgresCompositionTests`/`PostgresRuntimeSelectionTests` verify that runtime persistence uses only `ConnectionStrings:KpiRuntime` and migration-only configuration does not register the runtime `KpiDbContext`.
- Post-refactor note: the 8/8 PostgreSQL run predates the `OrganizationAuthorizationConfiguration` extraction; rerun the same opt-in suite after that refactor before marking T016 complete.
- 2026-08-13 local verification: solution Release build passed with 0 warnings and 0 errors; integration tests passed 48 with 8 PostgreSQL tests skipped because opt-in variables were not set in this session.
- 2026-08-13 `harness.cmd check`: repository contract, bootstrap, lint, build, Application/Domain/Integration tests passed; 3 existing Playwright tests failed before browser launch with environment-level `spawn EPERM`, so the full harness is not green.
- Credentials: not recorded.
