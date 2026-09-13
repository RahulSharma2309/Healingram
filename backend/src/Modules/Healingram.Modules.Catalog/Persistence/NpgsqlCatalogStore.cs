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
        return await LoadAsync(connection, null, null, cancellationToken);
    }

    public async Task<RetreatSnapshot?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var items = await LoadAsync(connection, null, slug, cancellationToken);
        return items.FirstOrDefault();
    }

    public async Task<CatalogSearchPage> SearchPublishedRetreatsAsync(
        RetreatSearchQuery query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = page < 1 ? 1 : page;
        var safeSize = pageSize < 1 ? 24 : pageSize;
        await using var connection = await OpenAsync(cancellationToken);
        var (ids, total) = await SearchPublishedIdsAsync(connection, query, safePage, safeSize, cancellationToken);
        if (ids.Count == 0)
        {
            return new CatalogSearchPage([], safePage, safeSize, total);
        }

        var loaded = await LoadAsync(connection, ids, null, cancellationToken);
        var byId = loaded.ToDictionary(item => item.Id);
        var items = ids.Select(id => byId.GetValueOrDefault(id)).OfType<RetreatSnapshot>().ToArray();
        return new CatalogSearchPage(items, safePage, safeSize, total);
    }

    public async Task<IReadOnlyList<string>> ListPublishedSlugsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"""
            SELECT DISTINCT r.slug
            FROM catalog.retreats r
            INNER JOIN catalog.programmes p ON p.retreat_id = r.id
            WHERE {CatalogSearchSql.PublishedPredicate}
            ORDER BY r.slug
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var slugs = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            slugs.Add(reader.GetString(0));
        }

        return slugs;
    }

    public async Task<IReadOnlyList<PlaceStatRow>> ListPublishedPlaceStatsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"""
            SELECT r.state_slug,
                   r.locality,
                   COALESCE(NULLIF(r.locality_slug, ''), r.locality),
                   COUNT(DISTINCT r.id)::int
            FROM catalog.retreats r
            INNER JOIN catalog.programmes p ON p.retreat_id = r.id
            WHERE {CatalogSearchSql.PublishedPredicate}
            GROUP BY r.state_slug, r.locality, COALESCE(NULLIF(r.locality_slug, ''), r.locality)
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<PlaceStatRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PlaceStatRow(
                reader.GetString(0),
                reader.GetString(1),
                PlaceNaming.Slugify(reader.GetString(2)),
                reader.GetInt32(3)));
        }

        return rows;
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

    public async Task<IReadOnlyList<ContentSectionRecord>> ListSectionsAsync(string surface, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT slug, surface, title, body, image_url, cta_label, cta_href, payload::text, sort_order
            FROM content.sections
            WHERE surface = $1 AND active = TRUE
            ORDER BY sort_order, slug
            """,
            connection);
        command.Parameters.AddWithValue(surface);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<ContentSectionRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ContentSectionRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? "{}" : reader.GetString(7),
                reader.GetInt32(8)));
        }

        return items;
    }

    public async Task<IReadOnlyList<NavigationItemRecord>> ListNavigationAsync(string menuKey, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT menu_key, label, href, sort_order, parent_key
            FROM content.navigation_items
            WHERE menu_key = $1 AND active = TRUE
            ORDER BY sort_order, label
            """,
            connection);
        command.Parameters.AddWithValue(menuKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<NavigationItemRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new NavigationItemRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetString(4)));
        }

        return items;
    }

    private static async Task<(IReadOnlyList<Guid> Ids, int Total)> SearchPublishedIdsAsync(
        NpgsqlConnection connection,
        RetreatSearchQuery query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var needs = NeedCatalog.ExpandNeedFilter(query.Need)
            .Select(value => value.ToLowerInvariant())
            .ToArray();
        var states = CatalogRetreatFilter.SplitCsv(query.State)
            .Select(value => value.ToLowerInvariant())
            .ToArray();
        var localities = CatalogRetreatFilter.SplitCsv(query.Locality);
        var localitySlugs = localities
            .Select(PlaceNaming.Slugify)
            .Where(value => value.Length > 0)
            .Select(value => value.ToLowerInvariant())
            .ToArray();
        var localityNames = localities.Select(value => value.ToLowerInvariant()).ToArray();
        var themes = CatalogRetreatFilter.SplitCsv(query.Theme)
            .Concat(CatalogRetreatFilter.SplitCsv(query.RetreatType))
            .Select(value => value.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var programmes = CatalogRetreatFilter.SplitCsv(query.Programme)
            .Select(value => value.ToLowerInvariant())
            .ToArray();

        var filters = new List<string> { CatalogSearchSql.PublishedPredicate };
        if (states.Length > 0)
        {
            filters.Add("lower(r.state_slug) = ANY(@states)");
        }

        if (localitySlugs.Length > 0 || localityNames.Length > 0)
        {
            filters.Add("(lower(r.locality_slug) = ANY(@localitySlugs) OR lower(r.locality) = ANY(@localityNames))");
        }

        if (needs.Length > 0)
        {
            filters.Add(CatalogSearchSql.NeedPredicate(needs));
        }

        filters.Add(CatalogSearchSql.DurationPredicate(query.Duration));

        if (themes.Length > 0)
        {
            filters.Add("""
                (
                    lower(p.theme_slug) = ANY(@themes)
                    OR replace(lower(p.theme_slug), '_', '-') = ANY(@themes)
                    OR lower(p.need_slug) = ANY(@themes)
                )
                """);
        }

        if (programmes.Length > 0)
        {
            filters.Add("lower(p.slug) = ANY(@programmes)");
        }

        if (query.MinPriceInr is not null || query.MaxPriceInr is not null)
        {
            filters.Add("""
                EXISTS (
                    SELECT 1
                    FROM catalog.programme_prices pp
                    WHERE pp.programme_id = p.id
                      AND upper(pp.status) = 'VERIFIED'
                      AND pp.amount_inr IS NOT NULL
                      AND (@minPrice IS NULL OR pp.amount_inr >= @minPrice)
                      AND (@maxPrice IS NULL OR pp.amount_inr <= @maxPrice)
                )
                """);
        }

        var where = string.Join(" AND ", filters);
        var from = $"""
            FROM catalog.retreats r
            INNER JOIN catalog.programmes p ON p.retreat_id = r.id
            LEFT JOIN catalog.programme_prices verified_price
                ON verified_price.programme_id = p.id AND upper(verified_price.status) = 'VERIFIED'
            LEFT JOIN LATERAL unnest(COALESCE(p.supported_durations, ARRAY[]::int[])) AS duration_nights(n) ON TRUE
            WHERE {where}
            """;

        await using var countCommand = new NpgsqlCommand($"SELECT COUNT(DISTINCT r.id)::int {from}", connection);
        BindSearchParameters(countCommand, needs, states, localitySlugs, localityNames, themes, programmes, query);
        var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var idCommand = new NpgsqlCommand(
            $"""
            SELECT r.id
            {from}
            GROUP BY r.id, r.name
            ORDER BY {CatalogSearchSql.OrderBy(query.Sort)}
            LIMIT @limit OFFSET @offset
            """,
            connection);
        BindSearchParameters(idCommand, needs, states, localitySlugs, localityNames, themes, programmes, query);
        idCommand.Parameters.AddWithValue("limit", pageSize);
        idCommand.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        var ids = new List<Guid>();
        await using var reader = await idCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetGuid(0));
        }

        return (ids, total);
    }

    private static void BindSearchParameters(
        NpgsqlCommand command,
        string[] needs,
        string[] states,
        string[] localitySlugs,
        string[] localityNames,
        string[] themes,
        string[] programmes,
        RetreatSearchQuery query)
    {
        void AddTextArray(string name, string[] values)
            => command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = values
            });

        AddTextArray("needs", needs);
        AddTextArray("states", states);
        AddTextArray("localitySlugs", localitySlugs);
        AddTextArray("localityNames", localityNames);
        AddTextArray("themes", themes);
        AddTextArray("programmes", programmes);
        command.Parameters.Add(new NpgsqlParameter("minPrice", NpgsqlDbType.Numeric)
        {
            Value = query.MinPriceInr is { } min ? min : DBNull.Value
        });
        command.Parameters.Add(new NpgsqlParameter("maxPrice", NpgsqlDbType.Numeric)
        {
            Value = query.MaxPriceInr is { } max ? max : DBNull.Value
        });
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<IReadOnlyList<RetreatSnapshot>> LoadAsync(
        NpgsqlConnection connection,
        IReadOnlyList<Guid>? ids,
        string? slug,
        CancellationToken cancellationToken)
    {
        var retreats = new Dictionary<Guid, RetreatRow>();
        await using (var command = new NpgsqlCommand(
            """
            SELECT id, slug, name, status, state_slug, locality, identity_complete,
                   image_url, typical_duration, positioning, locality_slug
            FROM catalog.retreats
            WHERE (@ids IS NULL OR id = ANY(@ids))
              AND (@slug IS NULL OR lower(slug) = lower(@slug))
            """,
            connection))
        {
            BindLoadFilter(command, ids, slug);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
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

        if (retreats.Count == 0)
        {
            return [];
        }

        var retreatIds = retreats.Keys.ToArray();
        var programmes = new Dictionary<Guid, ProgrammeRow>();
        var programmesByRetreat = new Dictionary<Guid, List<Guid>>();
        await using (var command = new NpgsqlCommand(
            """
            SELECT id, retreat_id, slug, name, supported_durations, need_slug, theme_slug, description, best_for
            FROM catalog.programmes
            WHERE retreat_id = ANY(@ids)
            """,
            connection))
        {
        command.Parameters.Add(IdArray("ids", retreatIds));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var retreatId = reader.GetGuid(1);
                var programmeSlug = reader.GetString(2);
                var theme = reader.IsDBNull(6) || string.IsNullOrWhiteSpace(reader.GetString(6))
                    ? ""
                    : reader.GetString(6);
                var need = reader.IsDBNull(5) || string.IsNullOrWhiteSpace(reader.GetString(5))
                    ? ""
                    : reader.GetString(5);
                var durations = reader.IsDBNull(4)
                    ? []
                    : reader.GetFieldValue<int[]>(4);

                programmes[id] = new ProgrammeRow(
                    id,
                    retreatId,
                    programmeSlug,
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
            WHERE programme_id IN (SELECT id FROM catalog.programmes WHERE retreat_id = ANY(@ids))
            """,
            connection))
        {
            command.Parameters.Add(IdArray("ids", retreatIds));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
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
            WHERE programme_id IN (SELECT id FROM catalog.programmes WHERE retreat_id = ANY(@ids))
            """,
            connection))
        {
            command.Parameters.Add(IdArray("ids", retreatIds));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
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
            "SELECT retreat_id, name, occupancy_max, description FROM catalog.rooms WHERE retreat_id = ANY(@ids)",
            connection))
        {
            command.Parameters.Add(IdArray("ids", retreatIds));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
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
            "SELECT retreat_id, name, verified, role, bio, image_url FROM catalog.experts WHERE retreat_id = ANY(@ids)",
            connection))
        {
            command.Parameters.Add(IdArray("ids", retreatIds));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
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
            "SELECT retreat_id, body, consented, verified, guest_name FROM catalog.testimonials WHERE retreat_id = ANY(@ids)",
            connection))
        {
            command.Parameters.Add(IdArray("ids", retreatIds));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
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
            "SELECT retreat_id, url, alt, category, sort_order FROM catalog.retreat_media WHERE retreat_id = ANY(@ids) ORDER BY sort_order",
            connection))
        {
            command.Parameters.Add(IdArray("ids", retreatIds));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
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
            "SELECT retreat_id, kind, payload::text FROM catalog.retreat_sections WHERE retreat_id = ANY(@ids)",
            connection))
        {
            command.Parameters.Add(IdArray("ids", retreatIds));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
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

    private static void BindLoadFilter(NpgsqlCommand command, IReadOnlyList<Guid>? ids, string? slug)
    {
        command.Parameters.Add(new NpgsqlParameter("ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid)
        {
            Value = ids is null ? DBNull.Value : ids.ToArray()
        });
        command.Parameters.Add(new NpgsqlParameter("slug", NpgsqlDbType.Text)
        {
            Value = string.IsNullOrWhiteSpace(slug) ? DBNull.Value : slug
        });
    }

    private static NpgsqlParameter IdArray(string name, Guid[] ids)
        => new(name, NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = ids };

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
