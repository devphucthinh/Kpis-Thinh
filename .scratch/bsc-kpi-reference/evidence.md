# BSC-KPI reference implementation evidence

## T011 — PostgreSQL organization authorization persistence

- Date: 2026-08-12
- Database target: `kpi_lab_test`
- Migration command: `./harness.cmd migrate`
- Migration result: all 9 declared migrations were already applied with matching checksums.
- Focused command: `dotnet test tests/Kpi.IntegrationTests/Kpi.IntegrationTests.csproj --no-restore --configuration Release --filter "FullyQualifiedName~OrganizationAuthorizationPostgresTests|FullyQualifiedName~KpiMigrationRunnerTests" -m:1`
- Result: 8 passed, 0 failed, 0 skipped.
- Covered behavior: migration ledger/checksum/idempotency, local target allow-list, composite Organization FK isolation, append-only approved baseline/segment facts, and PostgreSQL `xmin` optimistic concurrency.
- Credentials: not recorded.
