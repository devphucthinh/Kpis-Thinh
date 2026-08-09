namespace Kpi.Infrastructure.Postgres.Migrations;

/// <summary>Forward-only product migration order consumed by the explicit local/test migrator.</summary>
public static class KpiMigrationManifest
{
    public static IReadOnlyList<KpiMigrationScript> Scripts { get; } =
    [
        new("202608090001_DraftAuthoring", DraftAuthoringSql),
        new("202608090002_VersionGovernance", VersionGovernanceSql),
        new("202608090003_DefinitionRetention", DefinitionRetentionSql),
        new("202608090004_PeriodActivation", PeriodActivationSql),
        new("202608090005_PeriodAmendments", PeriodAmendmentsSql),
        new("202608090006_Evaluations", EvaluationsSql)
    ];

    public static IReadOnlyList<string> ProductMigrations => Scripts.Select(x => x.Id).ToArray();

    private const string DraftAuthoringSql = """
        CREATE TABLE IF NOT EXISTS organizations (
            id uuid PRIMARY KEY,
            name text NOT NULL
        );
        CREATE TABLE IF NOT EXISTS actors (
            id uuid PRIMARY KEY,
            organization_id uuid NOT NULL REFERENCES organizations(id),
            display_name text NOT NULL,
            capabilities integer NOT NULL DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS kpi_definitions (
            id uuid PRIMARY KEY,
            organization_id uuid NOT NULL REFERENCES organizations(id),
            code text NOT NULL,
            name text NOT NULL,
            description text NOT NULL,
            owner_id uuid NOT NULL,
            archived boolean NOT NULL DEFAULT false,
            archived_at timestamptz NULL,
            archived_by uuid NULL,
            created_at timestamptz NOT NULL DEFAULT now(),
            CONSTRAINT kpi_definitions_code_uq UNIQUE (organization_id, code)
        );
        CREATE TABLE IF NOT EXISTS kpi_versions (
            id uuid PRIMARY KEY,
            definition_id uuid NOT NULL REFERENCES kpi_definitions(id),
            version_number integer NOT NULL,
            name text NOT NULL,
            description text NOT NULL,
            change_summary text NOT NULL,
            predecessor_version_id uuid NULL REFERENCES kpi_versions(id),
            variables_json jsonb NOT NULL,
            formula_json jsonb NOT NULL,
            declared_result_type text NOT NULL,
            cadence text NOT NULL,
            status text NOT NULL,
            review_comment text NULL,
            effective_from timestamptz NULL,
            effective_to timestamptz NULL,
            CONSTRAINT kpi_versions_number_uq UNIQUE (definition_id, version_number)
        );
        CREATE TABLE IF NOT EXISTS audit_records (
            id uuid PRIMARY KEY,
            organization_id uuid NOT NULL REFERENCES organizations(id),
            actor_id uuid NOT NULL,
            entity_type text NOT NULL,
            entity_id uuid NOT NULL,
            event_type text NOT NULL,
            occurred_at timestamptz NOT NULL,
            correlation_id text NOT NULL,
            reason text NULL,
            summary_json jsonb NOT NULL DEFAULT '{}'::jsonb
        );
        CREATE INDEX IF NOT EXISTS audit_records_entity_idx ON audit_records (organization_id, entity_type, entity_id, occurred_at);
        CREATE OR REPLACE FUNCTION reject_audit_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN RAISE EXCEPTION 'audit_records are append-only'; END;
        $$;
        DROP TRIGGER IF EXISTS audit_records_append_only ON audit_records;
        CREATE TRIGGER audit_records_append_only BEFORE UPDATE OR DELETE ON audit_records
            FOR EACH ROW EXECUTE FUNCTION reject_audit_mutation();
        DO $$
        BEGIN
            IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'kpi_runtime') THEN
                REVOKE UPDATE, DELETE, TRUNCATE ON audit_records FROM kpi_runtime;
                GRANT SELECT, INSERT ON audit_records TO kpi_runtime;
            END IF;
        END $$;
        """;

    private const string VersionGovernanceSql = """
        CREATE EXTENSION IF NOT EXISTS btree_gist;
        ALTER TABLE kpi_versions ADD CONSTRAINT kpi_versions_effective_range_ck
            CHECK (effective_to IS NULL OR effective_from IS NOT NULL AND effective_to > effective_from);
        ALTER TABLE kpi_versions DROP CONSTRAINT IF EXISTS kpi_versions_effective_overlap_excl;
        ALTER TABLE kpi_versions ADD CONSTRAINT kpi_versions_effective_overlap_excl
            EXCLUDE USING gist (
                definition_id WITH =,
                tstzrange(effective_from, COALESCE(effective_to, 'infinity'::timestamptz), '[)') WITH &&
            ) WHERE (effective_from IS NOT NULL AND status IN ('Published', 'Retired'));
        CREATE INDEX IF NOT EXISTS kpi_versions_definition_effective_idx ON kpi_versions (definition_id, effective_from);
        ALTER TABLE kpi_versions ADD COLUMN IF NOT EXISTS revision bigint NOT NULL DEFAULT 0;
        """;

    private const string DefinitionRetentionSql = """
        ALTER TABLE kpi_definitions ADD COLUMN IF NOT EXISTS deletion_tombstone_json jsonb NULL;
        ALTER TABLE kpi_definitions ADD COLUMN IF NOT EXISTS revision bigint NOT NULL DEFAULT 0;
        CREATE INDEX IF NOT EXISTS kpi_definitions_owner_idx ON kpi_definitions (organization_id, owner_id);
        """;

    private const string PeriodActivationSql = """
        CREATE TABLE IF NOT EXISTS kpi_periods (
            id uuid PRIMARY KEY,
            organization_id uuid NOT NULL REFERENCES organizations(id),
            code text NOT NULL,
            name text NOT NULL,
            description text NOT NULL,
            cadence text NOT NULL,
            starts_at timestamptz NOT NULL,
            ends_at timestamptz NOT NULL,
            planner_id uuid NOT NULL,
            approver_id uuid NULL,
            status text NOT NULL,
            revision bigint NOT NULL DEFAULT 0,
            CONSTRAINT kpi_periods_interval_ck CHECK (ends_at > starts_at)
        );
        CREATE TABLE IF NOT EXISTS kpi_period_activations (
            id uuid PRIMARY KEY,
            period_id uuid NOT NULL REFERENCES kpi_periods(id),
            definition_id uuid NOT NULL REFERENCES kpi_definitions(id),
            version_id uuid NOT NULL REFERENCES kpi_versions(id),
            effective_revision_number integer NOT NULL,
            activated_at timestamptz NOT NULL,
            closed_at timestamptz NULL,
            CONSTRAINT kpi_period_activations_definition_uq UNIQUE (period_id, definition_id)
        );
        ALTER TABLE kpi_periods DROP CONSTRAINT IF EXISTS kpi_periods_overlap_excl;
        ALTER TABLE kpi_periods ADD CONSTRAINT kpi_periods_overlap_excl
            EXCLUDE USING gist (
                organization_id WITH =,
                cadence WITH =,
                tstzrange(starts_at, ends_at, '[)') WITH &&
            ) WHERE (status NOT IN ('Cancelled'));
        """;

    private const string PeriodAmendmentsSql = """
        CREATE TABLE IF NOT EXISTS kpi_period_amendments (
            id uuid PRIMARY KEY,
            period_id uuid NOT NULL REFERENCES kpi_periods(id),
            revision_number integer NOT NULL,
            base_revision_number integer NOT NULL,
            proposed_starts_at timestamptz NOT NULL,
            proposed_ends_at timestamptz NOT NULL,
            proposed_selections_json jsonb NOT NULL,
            reason text NOT NULL,
            proposed_by uuid NOT NULL,
            proposed_at timestamptz NOT NULL,
            status text NOT NULL,
            reviewed_by uuid NULL,
            reviewed_at timestamptz NULL,
            review_comment text NULL,
            CONSTRAINT kpi_period_amendments_revision_uq UNIQUE (period_id, revision_number),
            CONSTRAINT kpi_period_amendments_interval_ck CHECK (proposed_ends_at > proposed_starts_at)
        );
        CREATE INDEX IF NOT EXISTS kpi_period_amendments_period_idx ON kpi_period_amendments (period_id, revision_number);
        """;

    private const string EvaluationsSql = """
        CREATE TABLE IF NOT EXISTS kpi_evaluations (
            id uuid PRIMARY KEY,
            activation_id uuid NOT NULL REFERENCES kpi_period_activations(id),
            version_id uuid NOT NULL REFERENCES kpi_versions(id),
            formula_snapshot_json jsonb NOT NULL,
            inputs_json jsonb NOT NULL,
            outcome_json jsonb NOT NULL,
            evaluator_actor_id uuid NOT NULL,
            evaluated_at timestamptz NOT NULL,
            supersedes_id uuid NULL REFERENCES kpi_evaluations(id),
            correction_reason text NULL,
            correction_diff_json jsonb NULL,
            is_current_success boolean NOT NULL DEFAULT false
        );
        CREATE UNIQUE INDEX IF NOT EXISTS kpi_evaluations_one_current_success_idx
            ON kpi_evaluations (activation_id) WHERE is_current_success;
        CREATE INDEX IF NOT EXISTS kpi_evaluations_history_idx ON kpi_evaluations (activation_id, evaluated_at, id);
        """;
}

public sealed record KpiMigrationScript(string Id, string Sql)
{
    public string Checksum { get; } = Convert.ToHexString(
        System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Sql)));
}
