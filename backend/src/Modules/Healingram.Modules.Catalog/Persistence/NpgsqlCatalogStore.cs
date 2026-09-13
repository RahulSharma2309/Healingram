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

    public async Task SeedPresentationAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        await CatalogPresentationSeed.ApplyAsync(connection, tx, cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NeedRecord>> ListNeedRecordsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT slug, label, description, image_url, icon_key, sort_order, kind, active
            FROM catalog.needs
            ORDER BY sort_order, label
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<NeedRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new NeedRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetInt32(5),
                reader.GetString(6),
                reader.GetBoolean(7)));
        }

        return items;
    }

    public async Task<IReadOnlyList<DiscoveryCardRecord>> ListDiscoveryCardsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT slug, surface, label, description, image_url, icon_key, href, sort_order
            FROM catalog.discovery_cards
            WHERE active = TRUE
            ORDER BY surface, sort_order, label
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<DiscoveryCardRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new DiscoveryCardRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetInt32(7)));
        }

        return items;
    }

    public async Task<IReadOnlyList<ThemeRecord>> ListThemesAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT slug, label, sort_order FROM catalog.themes ORDER BY sort_order, label",
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<ThemeRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ThemeRecord(reader.GetString(0), reader.GetString(1), reader.GetInt32(2)));
        }

        return items;
    }

    public async Task<IReadOnlyList<DestinationRecord>> ListDestinationRecordsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT slug, kind, parent_slug, label, description, image_url, sort_order
            FROM catalog.destinations
            ORDER BY sort_order, label
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<DestinationRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new DestinationRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetInt32(6)));
        }

        return items;
    }

    public async Task<CatalogQuoteRecord> SaveQuoteAsync(CatalogQuoteRecord quote, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO catalog.price_quotes (
                id, retreat_slug, programme_slug, duration_nights, occupancy, guests,
                currency, base_amount, tax_amount, total_amount, price_status, pricing_version, snapshot)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13::jsonb)
            """,
            connection);
        command.Parameters.AddWithValue(quote.Id);
        command.Parameters.AddWithValue(quote.RetreatSlug);
        command.Parameters.AddWithValue(quote.ProgrammeSlug);
        command.Parameters.AddWithValue(quote.DurationNights);
        command.Parameters.AddWithValue(quote.Occupancy);
        command.Parameters.AddWithValue(quote.Guests);
        command.Parameters.AddWithValue(quote.Currency);
        command.Parameters.Add(Typed(quote.BaseAmount, NpgsqlDbType.Numeric));
        command.Parameters.Add(Typed(quote.TaxAmount, NpgsqlDbType.Numeric));
        command.Parameters.Add(Typed(quote.TotalAmount, NpgsqlDbType.Numeric));
        command.Parameters.AddWithValue(quote.PriceStatus);
        command.Parameters.AddWithValue(quote.PricingVersion);
        command.Parameters.AddWithValue(quote.SnapshotJson);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return quote;
    }

    public async Task<CatalogQuoteRecord?> GetQuoteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, retreat_slug, programme_slug, duration_nights, occupancy, guests,
                   currency, base_amount, tax_amount, total_amount, price_status, pricing_version, snapshot::text
            FROM catalog.price_quotes
            WHERE id = $1
            """,
            connection);
        command.Parameters.AddWithValue(id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CatalogQuoteRecord(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetString(4),
            reader.GetInt32(5),
            reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetDecimal(7),
            reader.IsDBNull(8) ? null : reader.GetDecimal(8),
            reader.IsDBNull(9) ? null : reader.GetDecimal(9),
            reader.GetString(10),
            reader.GetString(11),
            reader.GetString(12));
    }

    public async Task<IReadOnlyList<ContentPageRecord>> ListPublishedContentAsync(string? kind, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT slug, title, body, status, kind, sort_order, published_at
            FROM content.pages
            WHERE status = 'published'
              AND ($1::text IS NULL OR kind = $1)
            ORDER BY sort_order, title
            """,
            connection);
        command.Parameters.Add(Typed(kind, NpgsqlDbType.Text));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<ContentPageRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ContentPageRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt32(5),
                reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6)));
        }

        return items;
    }

    public async Task<ContentPageRecord?> GetPublishedContentAsync(string slug, CancellationToken cancellationToken)
    {
        var pages = await ListPublishedContentAsync(null, cancellationToken);
        return pages.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
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
            SELECT id, retreat_id, slug, name, supported_durations, need_slug, theme_slug, description, best_for
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

                programmes[id] = new ProgrammeRow(
                    id,
                    retreatId,
                    slug,
                    reader.GetString(3),
                    need,
                    theme,
                    durations,
                    reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetString(7) : null,
                    reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetString(8) : null);
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
            "SELECT retreat_id, name, occupancy_max, description FROM catalog.rooms",
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

                list.Add(new RoomSnapshot
                {
                    Name = reader.GetString(1),
                    OccupancyMax = reader.GetInt32(2),
                    Description = reader.FieldCount > 3 && !reader.IsDBNull(3) ? reader.GetString(3) : null
                });
            }
        }

        var expertsByRetreat = new Dictionary<Guid, List<ExpertSnapshot>>();
        await using (var command = new NpgsqlCommand(
            "SELECT retreat_id, name, verified, role, bio, image_url FROM catalog.experts",
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
                    Role = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Bio = reader.FieldCount > 4 && !reader.IsDBNull(4) ? reader.GetString(4) : null,
                    ImageUrl = reader.FieldCount > 5 && !reader.IsDBNull(5) ? reader.GetString(5) : null
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

        var mediaByRetreat = new Dictionary<Guid, List<MediaSnapshot>>();
        await using (var command = new NpgsqlCommand(
            "SELECT retreat_id, url, alt, category, sort_order FROM catalog.retreat_media ORDER BY sort_order",
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var retreatId = reader.GetGuid(0);
                if (!mediaByRetreat.TryGetValue(retreatId, out var list))
                {
                    list = [];
                    mediaByRetreat[retreatId] = list;
                }

                list.Add(new MediaSnapshot
                {
                    Url = reader.GetString(1),
                    Alt = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Category = reader.GetString(3),
                    SortOrder = reader.GetInt32(4)
                });
            }
        }

        var sectionsByRetreat = new Dictionary<Guid, List<SectionSnapshot>>();
        await using (var command = new NpgsqlCommand(
            "SELECT retreat_id, kind, payload::text FROM catalog.retreat_sections",
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var retreatId = reader.GetGuid(0);
                if (!sectionsByRetreat.TryGetValue(retreatId, out var list))
                {
                    list = [];
                    sectionsByRetreat[retreatId] = list;
                }

                list.Add(new SectionSnapshot
                {
                    Kind = reader.GetString(1),
                    PayloadJson = reader.GetString(2)
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
                    Description = p.Description,
                    BestFor = p.BestFor,
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
                Testimonials = testimonialsByRetreat.GetValueOrDefault(row.Id) ?? [],
                Media = mediaByRetreat.GetValueOrDefault(row.Id) ?? [],
                Sections = sectionsByRetreat.GetValueOrDefault(row.Id) ?? []
            };
        }).ToArray();
    }

    private static RetreatPublicationStatus ParseStatus(string status) => status.ToLowerInvariant() switch
    {
        "active" or "published" => RetreatPublicationStatus.Active,
        "archived" => RetreatPublicationStatus.Archived,
        "pending_review" => RetreatPublicationStatus.PendingReview,
        "approved" => RetreatPublicationStatus.Approved,
        "suspended" => RetreatPublicationStatus.Suspended,
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
        int[] Durations,
        string? Description,
        string? BestFor);
}
