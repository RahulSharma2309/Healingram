using Healingram.BuildingBlocks.Persistence;
using Xunit;

namespace Healingram.Api.Tests;

public class SchemaInstallerTests
{
    [Fact]
    public void Pending_files_skip_already_applied_ids()
    {
        var files = new[]
        {
            @"C:\db\001_schemas.sql",
            "/var/db/010_enterprise_foundation.sql",
            "backend/db/011_enterprise_hardening.sql",
            "backend/db/012_backend_driven_application.sql"
        };
        var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "001_schemas.sql",
            "010_enterprise_foundation.sql",
            "011_enterprise_hardening.sql"
        };

        var pending = SchemaInstaller.PendingFiles(files, applied);

        Assert.Equal(["012_backend_driven_application.sql"], pending.Select(SchemaInstaller.MigrationId));
        Assert.Equal("012", SchemaInstaller.MigrationVersion(pending[0]));
    }

    [Fact]
    public void Migration_id_is_the_filename()
    {
        Assert.Equal("011_enterprise_hardening.sql", SchemaInstaller.MigrationId("backend/db/011_enterprise_hardening.sql"));
        Assert.Equal("011_enterprise_hardening.sql", SchemaInstaller.MigrationId(@"C:\db\011_enterprise_hardening.sql"));
        Assert.Equal("001", SchemaInstaller.MigrationVersion("001_schemas.sql"));
        Assert.Equal("010", SchemaInstaller.MigrationVersion(@"D:\sql\010_enterprise_foundation.sql"));
    }

    [Fact]
    public void Legacy_ledger_records_older_scripts_on_existing_databases()
    {
        var files = new[]
        {
            "001_schemas.sql",
            "010_enterprise_foundation.sql",
            "011_enterprise_hardening.sql",
            "012_backend_driven_application.sql"
        };

        var missingFoundation = SchemaInstaller.LegacyIdsToRecord(
            files,
            hasExistingSchema: true,
            hasFoundationTables: false,
            hasHardeningColumns: false);
        Assert.Equal(["001_schemas.sql"], missingFoundation);

        var withoutHardening = SchemaInstaller.LegacyIdsToRecord(
            files,
            hasExistingSchema: true,
            hasFoundationTables: true,
            hasHardeningColumns: false);
        Assert.Equal(["001_schemas.sql", "010_enterprise_foundation.sql"], withoutHardening);

        var withHardening = SchemaInstaller.LegacyIdsToRecord(
            files,
            hasExistingSchema: true,
            hasFoundationTables: true,
            hasHardeningColumns: true);
        Assert.Equal(["001_schemas.sql", "010_enterprise_foundation.sql", "011_enterprise_hardening.sql"], withHardening);
        Assert.DoesNotContain("012_backend_driven_application.sql", withHardening);

        Assert.Empty(SchemaInstaller.LegacyIdsToRecord(
            files,
            hasExistingSchema: false,
            hasFoundationTables: false,
            hasHardeningColumns: false));
    }
}
