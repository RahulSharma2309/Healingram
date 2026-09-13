using Healingram.Modules.Catalog.Application;
using Healingram.Modules.Catalog.Domain;
using Healingram.Modules.Catalog.Seed;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace Healingram.Modules.Catalog.Persistence;

internal sealed class NpgsqlCatalogStore(IConfiguration configuration) : ICatalogStore
{
    public async Task<IReadOnlyList<RetreatSnapshot>> ListRetreatsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return await LoadAllAsync(connection, cancellationToken);
    }

    public async Task<RetreatSnapshot?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var all = await ListRetreatsAsync(cancellationToken);
        return all.FirstOrDefault(r => r.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task EnsurePublicSchemaAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(CatalogPublicSchema.Load(), connection);
        command.CommandTimeout = 60;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> CountRetreatsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT COUNT(*)::int FROM catalog.retreats", connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is int count ? count : Convert.ToInt32(result);
    }

    public async Task SeedAsync(IReadOnlyList<LaunchRetreatSeed> retreats, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var (slug, label) in LaunchCatalogData.Needs)
        {
            await using var need = new NpgsqlCommand(
                """
                INSERT INTO catalog.needs (slug, label)
                VALUES ($1, $2)
                ON CONFLICT (slug) DO NOTHING
                """,
                connection,
                tx);
            need.Parameters.AddWithValue(slug);
            need.Parameters.AddWithValue(label);
            await need.ExecuteNonQueryAsync(cancellationToken);
        }

        var states = retreats.Select(r => r.StateSlug).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var state in states)
        {
            await using var dest = new NpgsqlCommand(
                """
                INSERT INTO catalog.destinations (slug, kind, parent_slug, label)
                VALUES ($1, 'state', NULL, $2)
                ON CONFLICT (slug) DO NOTHING
                """,
                connection,
                tx);
            dest.Parameters.AddWithValue(state);
            dest.Parameters.AddWithValue(PlaceNaming.StateLabel(state));
            await dest.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var retreat in retreats)
        {
            var localitySlug = PlaceNaming.Slugify(retreat.Locality);
            await using var dest = new NpgsqlCommand(
                """
                INSERT INTO catalog.destinations (slug, kind, parent_slug, label)
                VALUES ($1, 'locality', $2, $3)
                ON CONFLICT (slug) DO NOTHING
                """,
                connection,
                tx);
            dest.Parameters.AddWithValue(localitySlug);
            dest.Parameters.AddWithValue(retreat.StateSlug);
            dest.Parameters.AddWithValue(retreat.Locality);
            await dest.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var retreat in retreats)
        {
            var snapshot = LaunchCatalogMapper.ToSnapshot(retreat);
            await using var insertRetreat = new NpgsqlCommand(
                """
                INSERT INTO catalog.retreats (
                    id, slug, name, status, state_slug, region_slug, locality,
                    identity_complete, settlement_mode, image_url, typical_duration,
                    positioning, locality_slug)
                VALUES ($1, $2, $3, $4, $5, $5, $6, TRUE, 'MARKETPLACE_SPLIT', $7, $8, $9, $10)
                ON CONFLICT (slug) DO NOTHING
                """,
                connection,
                tx);
            insertRetreat.Parameters.AddWithValue(snapshot.Id);
            insertRetreat.Parameters.AddWithValue(snapshot.Slug);
            insertRetreat.Parameters.AddWithValue(snapshot.Name);
            insertRetreat.Parameters.AddWithValue("active");
            insertRetreat.Parameters.AddWithValue(snapshot.StateSlug);
            insertRetreat.Parameters.AddWithValue(snapshot.Locality);
            insertRetreat.Parameters.Add(Typed(snapshot.ImageUrl, NpgsqlDbType.Text));
            insertRetreat.Parameters.Add(Typed(snapshot.TypicalDuration, NpgsqlDbType.Text));
            insertRetreat.Parameters.Add(Typed(snapshot.Positioning, NpgsqlDbType.Text));
            insertRetreat.Parameters.AddWithValue(snapshot.LocalitySlug);
            await insertRetreat.ExecuteNonQueryAsync(cancellationToken);

            foreach (var programme in snapshot.Programmes)
            {
                await using var insertProgramme = new NpgsqlCommand(
                    """
                    INSERT INTO catalog.programmes (
                        id, retreat_id, slug, name, supported_durations, need_slug, theme_slug)
                    VALUES ($1, $2, $3, $4, $5, $6, $7)
                    ON CONFLICT (retreat_id, slug) DO NOTHING
                    """,
                    connection,
                    tx);
                insertProgramme.Parameters.AddWithValue(programme.Id);
                insertProgramme.Parameters.AddWithValue(snapshot.Id);
                insertProgramme.Parameters.AddWithValue(programme.Slug);
                insertProgramme.Parameters.AddWithValue(programme.Name);
                insertProgramme.Parameters.AddWithValue(programme.SupportedDurations);
                insertProgramme.Parameters.AddWithValue(programme.NeedSlug);
                insertProgramme.Parameters.AddWithValue(programme.ThemeSlug);
                await insertProgramme.ExecuteNonQueryAsync(cancellationToken);

                foreach (var price in programme.Prices)
                {
                    var priceId = LaunchCatalogMapper.DeterministicGuid(
                        $"{snapshot.Slug}:{programme.Slug}:{price.Occupancy}:{price.DurationNights}");
                    await using var insertPrice = new NpgsqlCommand(
                        """
                        INSERT INTO catalog.programme_prices (
                            id, programme_id, occupancy, duration_nights, amount_inr, status)
                        VALUES ($1, $2, $3, $4, $5, $6)
                        ON CONFLICT (id) DO NOTHING
                        """,
                        connection,
                        tx);
                    insertPrice.Parameters.AddWithValue(priceId);
                    insertPrice.Parameters.AddWithValue(programme.Id);
                    insertPrice.Parameters.AddWithValue(price.Occupancy);
                    insertPrice.Parameters.AddWithValue(price.DurationNights);
                    insertPrice.Parameters.Add(Typed(price.AmountInr, NpgsqlDbType.Numeric));
                    insertPrice.Parameters.AddWithValue(PricePresentation.ToApi(price.Status));
                    await insertPrice.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }

        await tx.CommitAsync(cancellationToken);
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<IReadOnlyList<RetreatSnapshot>> LoadAllAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        var retreats = new Dictionary<Guid, RetreatRow>();
        await using (var command = new NpgsqlCommand(
            """
            SELECT id, slug, name, status, state_slug, locality, identity_complete,
                   image_url, typical_duration, positioning, locality_slug
            FROM catalog.retreats
            """,
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var locality = reader.GetString(5);
                retreats[id] = new RetreatRow(
                    id,
                    reader.GetString(1),
                    reader.GetString(2),
                    ParseStatus(reader.GetString(3)),
                    reader.GetString(4),
                    locality,
                    reader.GetBoolean(6),
                    reader.IsDBNull(7) ? null : reader.GetString(7),
                    reader.IsDBNull(8) ? null : reader.GetString(8),
                    reader.IsDBNull(9) ? null : reader.GetString(9),
                    reader.IsDBNull(10) || string.IsNullOrWhiteSpace(reader.GetString(10))
                        ? PlaceNaming.Slugify(locality)
                        : reader.GetString(10));
            }
        }

        var programmes = new Dictionary<Guid, ProgrammeRow>();
        var programmesByRetreat = new Dictionary<Guid, List<Guid>>();
        await using (var command = new NpgsqlCommand(
            """
            SELECT id, retreat_id, slug, name, supported_durations, need_slug, theme_slug
            FROM catalog.programmes
            """,
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var retreatId = reader.GetGuid(1);
                var slug = reader.GetString(2);
                var theme = reader.IsDBNull(6) || string.IsNullOrWhiteSpace(reader.GetString(6))
                    ? slug
                    : reader.GetString(6);
                var need = reader.IsDBNull(5) || string.IsNullOrWhiteSpace(reader.GetString(5))
                    ? NeedCatalog.NeedSlugForTheme(theme)
                    : reader.GetString(5);
                var durations = reader.IsDBNull(4)
                    ? []
                    : reader.GetFieldValue<int[]>(4);

                programmes[id] = new ProgrammeRow(id, retreatId, slug, reader.GetString(3), need, theme, durations);
                if (!programmesByRetreat.TryGetValue(retreatId, out var list))
                {
                    list = [];
                    programmesByRetreat[retreatId] = list;
                }

                list.Add(id);
            }
        }

        var pricesByProgramme = new Dictionary<Guid, List<PriceSnapshot>>();
        await using (var command = new NpgsqlCommand(
            """
            SELECT programme_id, occupancy, duration_nights, amount_inr, status
            FROM catalog.programme_prices
            """,
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var programmeId = reader.GetGuid(0);
                if (!pricesByProgramme.TryGetValue(programmeId, out var list))
                {
                    list = [];
                    pricesByProgramme[programmeId] = list;
                }

                list.Add(new PriceSnapshot
                {
                    Occupancy = reader.GetString(1),
                    DurationNights = reader.GetInt32(2),
                    AmountInr = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                    Status = PricePresentation.Parse(reader.GetString(4))
                });
            }
        }

        var inclusionsByProgramme = new Dictionary<Guid, List<InclusionSnapshot>>();
        await using (var command = new NpgsqlCommand(
            """
            SELECT programme_id, kind, label
            FROM catalog.inclusions
            """,
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var programmeId = reader.GetGuid(0);
                if (!inclusionsByProgramme.TryGetValue(programmeId, out var list))
                {
                    list = [];
                    inclusionsByProgramme[programmeId] = list;
                }

                list.Add(new InclusionSnapshot { Kind = reader.GetString(1), Label = reader.GetString(2) });
            }
        }

        var roomsByRetreat = new Dictionary<Guid, List<RoomSnapshot>>();
        await using (var command = new NpgsqlCommand(
            "SELECT retreat_id, name, occupancy_max FROM catalog.rooms",
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var retreatId = reader.GetGuid(0);
                if (!roomsByRetreat.TryGetValue(retreatId, out var list))
                {
                    list = [];
                    roomsByRetreat[retreatId] = list;
                }

                list.Add(new RoomSnapshot { Name = reader.GetString(1), OccupancyMax = reader.GetInt32(2) });
            }
        }

        var expertsByRetreat = new Dictionary<Guid, List<ExpertSnapshot>>();
        await using (var command = new NpgsqlCommand(
            "SELECT retreat_id, name, verified, role FROM catalog.experts",
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var retreatId = reader.GetGuid(0);
                if (!expertsByRetreat.TryGetValue(retreatId, out var list))
                {
                    list = [];
                    expertsByRetreat[retreatId] = list;
                }

                expertsByRetreat[retreatId].Add(new ExpertSnapshot
                {
                    Name = reader.GetString(1),
                    Verified = reader.GetBoolean(2),
                    Role = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
        }

        var testimonialsByRetreat = new Dictionary<Guid, List<TestimonialSnapshot>>();
        await using (var command = new NpgsqlCommand(
            "SELECT retreat_id, body, consented, verified, guest_name FROM catalog.testimonials",
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var retreatId = reader.GetGuid(0);
                if (!testimonialsByRetreat.TryGetValue(retreatId, out var list))
                {
                    list = [];
                    testimonialsByRetreat[retreatId] = list;
                }

                list.Add(new TestimonialSnapshot
                {
                    Body = reader.GetString(1),
                    Consented = reader.GetBoolean(2),
                    Verified = reader.GetBoolean(3),
                    GuestName = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }
        }

        return retreats.Values.Select(row =>
        {
            var programmeIds = programmesByRetreat.GetValueOrDefault(row.Id) ?? [];
            var programmeSnapshots = programmeIds.Select(id =>
            {
                var p = programmes[id];
                return new ProgrammeSnapshot
                {
                    Id = p.Id,
                    RetreatId = p.RetreatId,
                    Slug = p.Slug,
                    Name = p.Name,
                    NeedSlug = p.NeedSlug,
                    ThemeSlug = p.ThemeSlug,
                    SupportedDurations = p.Durations,
                    Prices = pricesByProgramme.GetValueOrDefault(id) ?? [],
                    Inclusions = inclusionsByProgramme.GetValueOrDefault(id) ?? []
                };
            }).ToArray();

            return new RetreatSnapshot
            {
                Id = row.Id,
                Slug = row.Slug,
                Name = row.Name,
                Status = row.Status,
                StateSlug = row.StateSlug,
                Locality = row.Locality,
                LocalitySlug = row.LocalitySlug,
                IdentityComplete = row.IdentityComplete,
                ImageUrl = row.ImageUrl,
                TypicalDuration = row.TypicalDuration,
                Positioning = row.Positioning,
                Programmes = programmeSnapshots,
                Rooms = roomsByRetreat.GetValueOrDefault(row.Id) ?? [],
                Experts = expertsByRetreat.GetValueOrDefault(row.Id) ?? [],
                Testimonials = testimonialsByRetreat.GetValueOrDefault(row.Id) ?? []
            };
        }).ToArray();
    }

    private static RetreatPublicationStatus ParseStatus(string status) => status.ToLowerInvariant() switch
    {
        "active" => RetreatPublicationStatus.Active,
        "archived" => RetreatPublicationStatus.Archived,
        _ => RetreatPublicationStatus.Draft
    };

    private static NpgsqlParameter Typed(object? value, NpgsqlDbType type) =>
        new() { NpgsqlDbType = type, Value = value ?? DBNull.Value };

    private sealed record RetreatRow(
        Guid Id,
        string Slug,
        string Name,
        RetreatPublicationStatus Status,
        string StateSlug,
        string Locality,
        bool IdentityComplete,
        string? ImageUrl,
        string? TypicalDuration,
        string? Positioning,
        string LocalitySlug);

    private sealed record ProgrammeRow(
        Guid Id,
        Guid RetreatId,
        string Slug,
        string Name,
        string NeedSlug,
        string ThemeSlug,
        int[] Durations);
}
