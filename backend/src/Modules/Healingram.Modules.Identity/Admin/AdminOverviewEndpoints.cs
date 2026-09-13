using Healingram.Contracts.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Healingram.Modules.Identity.Admin;

internal static class AdminOverviewEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/overview", async (
            IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            await using var connection = new NpgsqlConnection(
                configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required."));
            await connection.OpenAsync(cancellationToken);

            async Task<int> Count(string sql)
            {
                await using var command = new NpgsqlCommand(sql, connection);
                var result = await command.ExecuteScalarAsync(cancellationToken);
                return result is int n ? n : Convert.ToInt32(result);
            }

            return Results.Ok(new
            {
                users = await Count("SELECT COUNT(*)::int FROM identity.users"),
                partners = await Count("SELECT COUNT(*)::int FROM partners.partners"),
                memberships = await Count("SELECT COUNT(*)::int FROM partners.partner_users"),
                retreats = await Count("SELECT COUNT(*)::int FROM catalog.retreats"),
                publishedRetreats = await Count("SELECT COUNT(*)::int FROM catalog.retreats WHERE status IN ('active', 'published')"),
                availabilityRequests = await Count("SELECT COUNT(*)::int FROM availability.requests"),
                bookings = await Count("SELECT COUNT(*)::int FROM booking.bookings"),
                payments = await Count("SELECT COUNT(*)::int FROM payment.intents"),
                leads = await Count("SELECT COUNT(*)::int FROM leads.expert_leads"),
                notifications = await Count("SELECT COUNT(*)::int FROM notifications.inbox")
            });
        }).RequireAuthorization(IdentityPolicies.AdminWrite).WithTags("Admin");

        app.MapGet("/api/admin/leads", async (
            IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            await using var connection = new NpgsqlConnection(
                configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required."));
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                """
                SELECT id, full_name, phone_e164, email, whatsapp_consent, source, status, context::text, created_at
                FROM leads.expert_leads
                ORDER BY created_at DESC
                LIMIT 200
                """,
                connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<object>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new
                {
                    id = reader.GetGuid(0),
                    fullName = reader.GetString(1),
                    phone = reader.GetString(2),
                    email = reader.GetString(3),
                    whatsappConsent = reader.GetBoolean(4),
                    source = reader.IsDBNull(5) ? null : reader.GetString(5),
                    status = reader.GetString(6),
                    context = reader.IsDBNull(7) ? "{}" : reader.GetString(7),
                    createdAt = reader.GetFieldValue<DateTimeOffset>(8)
                });
            }

            return Results.Ok(new { items });
        }).RequireAuthorization(IdentityPolicies.AdminWrite).WithTags("Admin");
    }
}
