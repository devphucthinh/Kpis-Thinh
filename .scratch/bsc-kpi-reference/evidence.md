# BSC-KPI reference implementation evidence

## T011 — PostgreSQL organization authorization persistence

- Date: 2026-08-13
- Database target: `kpi_lab_test`
- Migration command: `./harness.cmd migrate`
- Migration result: the nine existing migrations were already applied with matching checksums and `202608130001_AuthorizationFoundation` applied successfully.
- Focused command: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --configuration Release --filter "FullyQualifiedName~OrganizationAuthorizationPostgresTests|FullyQualifiedName~KpiMigrationRunnerTests" -m:1`
- Result: 8 passed, 0 failed, 0 skipped.
- Covered behavior: migration ledger/checksum/idempotency, local target allow-list, composite Organization FK isolation, append-only approved baseline/segment facts, and PostgreSQL `xmin` optimistic concurrency.
- Exact requirement traceability: `FR-001`/`FR-002` cover composite Organization FK isolation; `FR-013`/`FR-037` cover append-only baseline and applicability facts; `FR-036` covers stale `xmin` concurrency; `FR-033` covers authorization evidence columns and audit append behavior.
- Composition boundary: `OrganizationAuthorizationSchemaTests` plus `PostgresCompositionTests`/`PostgresRuntimeSelectionTests` verify that runtime persistence uses only `ConnectionStrings:KpiRuntime` and migration-only configuration does not register the runtime `KpiDbContext`.
- Post-refactor verification (2026-08-13, supplied terminal output): `./harness.cmd migrate` targeted `kpi_lab_test` with all declared migrations already applied, then the focused suite completed with 8 passed, 0 failed, and 0 skipped. This satisfies the T016 mapping/refactor verification gate.
- 2026-08-13 local verification: solution Release build passed with 0 warnings and 0 errors; integration tests passed 48 with 8 PostgreSQL tests skipped because opt-in variables were not set in this session.
- 2026-08-13 opt-in verification: `KPI_POSTGRES_TESTS=1`, `Kpi__TestDatabaseName=kpi_lab_test`, and the migration connection targeted the local allow-listed PostgreSQL database; the complete `Kpi.IntegrationTests` assembly passed 61/61 with 0 failed and 0 skipped.
- 2026-08-13 canonical bootstrap: `./harness.cmd bootstrap` passed locked restore, Release Playwright-driver build, and pinned Chromium provisioning after the IntegrationTests lock file was regenerated for the migrator project reference.
- 2026-08-13 rerun `./harness.cmd check`: repository contract, locked bootstrap, lint, all .NET tests, integration-guide, branch-policy, migration-command, and runtime-profile checks passed; the default no-credential profile intentionally reported 8 PostgreSQL skips.
- T024 foundational focused verification: Domain authorization value-object/audit tests, Application freshness/catalog/unit-of-work tests, migration/Problem Details/composition tests, and the opt-in PostgreSQL migration suite all passed; no User Story implementation is included in this checkpoint.
- Credentials: not recorded.
