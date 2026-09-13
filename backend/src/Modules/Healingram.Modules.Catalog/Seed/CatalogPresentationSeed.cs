using Npgsql;
using NpgsqlTypes;

namespace Healingram.Modules.Catalog.Seed;

internal static class CatalogPresentationSeed
{
    internal static async Task ApplyAsync(NpgsqlConnection connection, NpgsqlTransaction tx, CancellationToken cancellationToken)
    {
        await UpdateNeedMetadataAsync(connection, tx, cancellationToken);
        await UpsertThemesAsync(connection, tx, cancellationToken);
        await UpsertDiscoveryAsync(connection, tx, cancellationToken);
        await UpsertDestinationsAsync(connection, tx, cancellationToken);
        await UpsertMatchingAsync(connection, tx, cancellationToken);
        await UpsertContentAsync(connection, tx, cancellationToken);
        await UpsertLeadOptionsAsync(connection, tx, cancellationToken);
        await UpsertSettingsAsync(connection, tx, cancellationToken);
        await UpsertListingExtrasAsync(connection, tx, cancellationToken);
    }

    private static async Task UpdateNeedMetadataAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        CancellationToken cancellationToken)
    {
        var rows = new (string Slug, string Description, string Image, string Icon, int Order, string Kind)[]
        {
            ("stress-burnout", "For slowing down, restoring energy and stepping away from constant overwhelm.", "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=900&q=80", "brain", 1, "need"),
            ("ayurveda", "Traditional wellness programmes built around personalised routines, food and therapies.", "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=900&q=80", "leaf", 2, "need"),
            ("panchakarma", "Longer, structured Ayurvedic programmes for people seeking a deeper reset.", "https://images.unsplash.com/photo-1600334129128-685c5582fd35?w=900&q=80", "sparkles", 3, "need"),
            ("yoga", "Retreats centred around movement, breath, stillness and mindful routines.", "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=900&q=80", "flower", 4, "need"),
            ("meditation", "Quiet practice-led stays for people who want space to sit, breathe and reset.", "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=900&q=80", "moon", 5, "need"),
            ("rejuvenation", "For rest, recovery, better routines and feeling physically refreshed.", "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=900&q=80", "battery", 6, "need"),
            ("weekend-wellness", "Short restorative stays when you have only a few nights.", "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=900&q=80", "calendar", 7, "need"),
            ("weight-metabolic", "Programmes aimed at metabolic wellness and structured daily routines.", "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=900&q=80", "leaf", 8, "need"),
            ("detox", "Cleansing-oriented programmes confirmed with the retreat.", "https://images.unsplash.com/photo-1545389336-cf090694435e?w=900&q=80", "sparkles", 9, "need"),
            ("lifestyle-holistic", "Broader lifestyle and holistic wellness stays.", "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=900&q=80", "compass", 10, "need"),
            ("nature-wellness", "Nature-facing stays for quiet rest.", "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=900&q=80", "palmtree", 11, "need"),
            ("long-stay", "Longer 7 / 14 / 21 / 28-day programmes.", "https://images.unsplash.com/photo-1600334129128-685c5582fd35?w=900&q=80", "hourglass", 12, "need"),
            ("not-sure", "Browse published inventory when you are still deciding.", null!, "compass", 99, "need")
        };

        foreach (var row in rows)
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO catalog.needs (slug, label, description, image_url, icon_key, sort_order, kind, active)
                VALUES ($1, $2, $3, $4, $5, $6, $7, TRUE)
                ON CONFLICT (slug) DO UPDATE SET
                    description = EXCLUDED.description,
                    image_url = EXCLUDED.image_url,
                    icon_key = EXCLUDED.icon_key,
                    sort_order = EXCLUDED.sort_order,
                    kind = EXCLUDED.kind,
                    active = TRUE
                """,
                connection,
                tx);
            command.Parameters.AddWithValue(row.Slug);
            command.Parameters.AddWithValue(TitleFrom(row.Slug));
            command.Parameters.Add(Typed(row.Description));
            command.Parameters.Add(Typed(row.Image));
            command.Parameters.Add(Typed(row.Icon));
            command.Parameters.AddWithValue(row.Order);
            command.Parameters.AddWithValue(row.Kind);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task UpsertThemesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        CancellationToken cancellationToken)
    {
        var themes = new (string Slug, string Label, int Order)[]
        {
            ("ayurveda", "Ayurveda", 1),
            ("panchakarma", "Panchakarma", 2),
            ("yoga", "Yoga", 3),
            ("meditation", "Meditation", 4),
            ("stress_burnout", "Stress Management / Burnout Recovery", 5),
            ("rejuvenation", "Rejuvenation", 6),
            ("detox", "Detox / Cleansing", 7),
            ("weight_metabolic", "Weight / Metabolic Wellness", 8),
            ("lifestyle_holistic", "Holistic / Lifestyle Wellness", 9),
            ("weekend", "Weekend Wellness", 10),
            ("nature_wellness", "Nature-based wellness", 11),
            ("long_stay", "Longer restorative stay", 12)
        };

        foreach (var theme in themes)
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO catalog.themes (slug, label, sort_order)
                VALUES ($1, $2, $3)
                ON CONFLICT (slug) DO UPDATE SET label = EXCLUDED.label, sort_order = EXCLUDED.sort_order
                """,
                connection,
                tx);
            command.Parameters.AddWithValue(theme.Slug);
            command.Parameters.AddWithValue(theme.Label);
            command.Parameters.AddWithValue(theme.Order);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task UpsertDiscoveryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        CancellationToken cancellationToken)
    {
        var cards = new (string Slug, string Surface, string Label, string Description, string Image, string Icon, string Href, int Order)[]
        {
            ("calm-mind", "need", "Calm my mind", "Stress, overwhelm, trouble switching off.", "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=900&q=80", "brain", "/retreats?need=calm-mind", 1),
            ("rest-recharge", "need", "Rest & recharge", "Low energy, burnout, poor sleep.", "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=900&q=80", "battery", "/retreats?need=rest-recharge", 2),
            ("reset-body", "need", "Reset my body", "Detox, digestion, metabolic or weight concerns.", "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=900&q=80", "leaf", "/retreats?need=reset-body", 3),
            ("go-deeper", "need", "Go deeper into wellness", "Ayurveda, Panchakarma, yoga or meditation.", "https://images.unsplash.com/photo-1600334129128-685c5582fd35?w=900&q=80", "sparkles", "/retreats?need=go-deeper", 4),
            ("real-break", "need", "I just need a real break", "I’m not sure what I need yet.", "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=900&q=80", "palmtree", "/questionnaire", 5),
            ("karnataka", "destination", "Karnataka", "Wellness retreats in and around Bengaluru, from short restorative stays to structured Ayurveda programmes.", "https://images.unsplash.com/photo-1477587458883-47145ed94245?w=1400&q=80", "map", "/retreats?state=karnataka", 1),
            ("kerala", "destination", "Kerala", "Ayurveda and wellness retreats across one of India’s most established healing destinations.", "https://images.unsplash.com/photo-1602216056096-3b40cc0c9944?w=1400&q=80", "map", "/retreats?state=kerala", 2)
        };

        foreach (var card in cards)
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO catalog.discovery_cards
                    (slug, surface, label, description, image_url, icon_key, href, sort_order, active)
                VALUES ($1, $2, $3, $4, $5, $6, $7, $8, TRUE)
                ON CONFLICT (slug) DO UPDATE SET
                    surface = EXCLUDED.surface,
                    label = EXCLUDED.label,
                    description = EXCLUDED.description,
                    image_url = EXCLUDED.image_url,
                    icon_key = EXCLUDED.icon_key,
                    href = EXCLUDED.href,
                    sort_order = EXCLUDED.sort_order,
                    active = TRUE
                """,
                connection,
                tx);
            command.Parameters.AddWithValue(card.Slug);
            command.Parameters.AddWithValue(card.Surface);
            command.Parameters.AddWithValue(card.Label);
            command.Parameters.AddWithValue(card.Description);
            command.Parameters.AddWithValue(card.Image);
            command.Parameters.AddWithValue(card.Icon);
            command.Parameters.AddWithValue(card.Href);
            command.Parameters.AddWithValue(card.Order);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var hide = new NpgsqlCommand(
            """
            UPDATE catalog.discovery_cards
            SET active = FALSE
            WHERE surface = 'need'
              AND slug NOT IN ('calm-mind', 'rest-recharge', 'reset-body', 'go-deeper', 'real-break')
            """,
            connection,
            tx);
        await hide.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertDestinationsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            UPDATE catalog.destinations SET
                description = CASE slug
                    WHEN 'karnataka' THEN 'Wellness retreats in and around Bengaluru, from short restorative stays to structured Ayurveda programmes.'
                    WHEN 'kerala' THEN 'Ayurveda and wellness retreats across one of India’s most established healing destinations.'
                    ELSE description
                END,
                image_url = CASE slug
                    WHEN 'karnataka' THEN 'https://images.unsplash.com/photo-1477587458883-47145ed94245?w=1400&q=80'
                    WHEN 'kerala' THEN 'https://images.unsplash.com/photo-1602216056096-3b40cc0c9944?w=1400&q=80'
                    ELSE image_url
                END,
                sort_order = CASE slug
                    WHEN 'karnataka' THEN 1
                    WHEN 'kerala' THEN 2
                    ELSE sort_order
                END
            WHERE slug IN ('karnataka', 'kerala')
            """,
            connection,
            tx);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertMatchingAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        CancellationToken cancellationToken)
    {
        var questions = new (string Key, string Label, string Mode, int Order)[]
        {
            ("q1", "What do you need?", "multi", 1),
            ("q2", "What kind of experience are you open to?", "multi", 2),
            ("q3", "How long can you stay?", "single", 3),
            ("q4", "Where would you like to go?", "multi", 4)
        };

        foreach (var question in questions)
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO matching.questions (question_key, label, selection_mode, sort_order, active)
                VALUES ($1, $2, $3, $4, TRUE)
                ON CONFLICT (question_key) DO UPDATE SET
                    label = EXCLUDED.label,
                    selection_mode = EXCLUDED.selection_mode,
                    sort_order = EXCLUDED.sort_order,
                    active = TRUE
                """,
                connection,
                tx);
            command.Parameters.AddWithValue(question.Key);
            command.Parameters.AddWithValue(question.Label);
            command.Parameters.AddWithValue(question.Mode);
            command.Parameters.AddWithValue(question.Order);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var options = new (string Question, string Key, string Label, string? Description, string Icon, int Order, string[] Themes)[]
        {
            ("q1", "calm-mind", "Calm my mind", "Stress, overwhelm, trouble switching off", "brain", 1, ["stress_burnout", "meditation", "yoga"]),
            ("q1", "rest-recharge", "Rest & recharge", "Low energy, burnout, poor sleep", "battery", 2, ["rejuvenation", "stress_burnout", "weekend"]),
            ("q1", "reset-body", "Reset my body", "Detox, digestion, metabolic or weight concerns", "leaf", 3, ["detox", "weight_metabolic", "panchakarma", "ayurveda"]),
            ("q1", "go-deeper", "Go deeper into wellness", "Ayurveda, Panchakarma, yoga or meditation", "sparkles", 4, ["ayurveda", "panchakarma", "yoga", "meditation"]),
            ("q1", "real-break", "I just need a real break", "I’m not sure what I need yet", "palmtree", 5, ["lifestyle_holistic", "nature_wellness", "weekend", "rejuvenation"]),
            ("q2", "doctor-ayurveda", "Doctor-led Ayurveda", null, "stethoscope", 1, ["ayurveda", "panchakarma"]),
            ("q2", "yoga-meditation", "Yoga & meditation", null, "flower", 2, ["yoga", "meditation"]),
            ("q2", "quiet-escape", "Quiet restorative escape", null, "moon", 3, ["nature_wellness", "rejuvenation", "lifestyle_holistic"]),
            ("q2", "structured", "Structured wellness programme", null, "list", 4, ["panchakarma", "long_stay", "detox", "ayurveda"]),
            ("q2", "open-rec", "Open to your recommendation", null, "compass", 5, []),
            ("q3", "weekend", "Weekend", "2–3 nights", "calendar", 1, ["weekend"]),
            ("q3", "few-days", "A few days", "4–5 nights", "calendar", 2, ["weekend", "rejuvenation", "yoga", "meditation"]),
            ("q3", "week", "About a week", "6–8 nights", "hourglass", 3, ["rejuvenation", "ayurveda", "yoga", "detox"]),
            ("q3", "deeper", "A deeper stay", "10 nights or more", "hourglass", 4, ["long_stay", "panchakarma", "detox", "ayurveda"]),
            ("q3", "flexible", "I’m flexible", null, "calendar", 5, []),
            ("q4", "bengaluru-nearby", "Near Bengaluru", null, "map", 1, []),
            ("q4", "karnataka", "Karnataka", null, "map", 2, []),
            ("q4", "kerala", "Kerala", null, "map", 3, []),
            ("q4", "peaceful", "Somewhere peaceful", null, "palmtree", 4, []),
            ("q4", "anywhere", "Anywhere that fits", null, "compass", 5, [])
        };

        foreach (var option in options)
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO matching.question_options
                    (id, question_key, option_key, label, description, icon_key, sort_order, active, theme_slugs)
                VALUES ($1, $2, $3, $4, $5, $6, $7, TRUE, $8)
                ON CONFLICT (question_key, option_key) DO UPDATE SET
                    label = EXCLUDED.label,
                    description = EXCLUDED.description,
                    icon_key = EXCLUDED.icon_key,
                    sort_order = EXCLUDED.sort_order,
                    active = TRUE,
                    theme_slugs = EXCLUDED.theme_slugs
                """,
                connection,
                tx);
            command.Parameters.AddWithValue(DeterministicGuid($"{option.Question}:{option.Key}"));
            command.Parameters.AddWithValue(option.Question);
            command.Parameters.AddWithValue(option.Key);
            command.Parameters.AddWithValue(option.Label);
            command.Parameters.Add(Typed(option.Description));
            command.Parameters.AddWithValue(option.Icon);
            command.Parameters.AddWithValue(option.Order);
            command.Parameters.AddWithValue(option.Themes);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task UpsertContentAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        CancellationToken cancellationToken)
    {
        var pages = new (string Slug, string Title, string Body, string Kind, int Order)[]
        {
            ("about", "About Us", "Healingram.com is India's dedicated marketplace for wellness retreats and healing stays. Find retreats, practices and people that help you return to yourself. We connect guests with verified retreat partners across meditation, yoga, Ayurveda, and emotional wellness.", "page", 1),
            ("terms", "Terms", "Healingram is a programme-led marketplace. Guests request availability, partners confirm, and payment is taken only after a confirmed stay. These terms describe that journey. They do not replace a retreat's own house rules.", "page", 2),
            ("privacy", "Privacy", "Healingram stores account, request, booking and payment records in PostgreSQL. Session tokens stay in the browser only as a temporary access credential. We do not use browser storage as a business database.", "page", 3),
            ("faq-book", "How do I book a retreat?", "Choose a programme, request availability, and pay only after the retreat confirms.", "faq", 1),
            ("faq-cancel", "Can I cancel my booking?", "Cancellation terms are confirmed with the retreat when they accept your request.", "faq", 2),
            ("faq-pay", "Is payment secure?", "Payment is created on the server and marked paid only after a verified provider event.", "faq", 3),
            ("blog-choose", "How to Choose the Right Healing Retreat", "A practical guide to matching your wellness goals with the perfect program.", "post", 1),
            ("blog-detox", "5 Signs You Need a Digital Detox Retreat", "Recognize burnout before it becomes chronic.", "post", 2),
            ("blog-compare", "Meditation vs Therapy Retreats: What's Right for You?", "Compare approaches for stress, anxiety, and emotional healing.", "post", 3),
            ("therapy-meditation", "Meditation", "Find stillness and peace of mind", "category", 1),
            ("therapy-yoga", "Yoga", "Take a break from life and make new friends", "category", 2),
            ("therapy-ayurveda", "Ayurveda", "Ancient healing for mind, body and spirit", "category", 3)
        };

        foreach (var page in pages)
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO content.pages (slug, title, body, status, kind, sort_order, published_at, updated_at)
                VALUES ($1, $2, $3, 'published', $4, $5, now(), now())
                ON CONFLICT (slug) DO UPDATE SET
                    title = EXCLUDED.title,
                    body = EXCLUDED.body,
                    status = 'published',
                    kind = EXCLUDED.kind,
                    sort_order = EXCLUDED.sort_order,
                    published_at = COALESCE(content.pages.published_at, now()),
                    updated_at = now()
                """,
                connection,
                tx);
            command.Parameters.AddWithValue(page.Slug);
            command.Parameters.AddWithValue(page.Title);
            command.Parameters.AddWithValue(page.Body);
            command.Parameters.AddWithValue(page.Kind);
            command.Parameters.AddWithValue(page.Order);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task UpsertLeadOptionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        CancellationToken cancellationToken)
    {
        var options = new (string Kind, string Key, string Label, int Order)[]
        {
            ("help_type", "choosing_retreat", "Choosing the right retreat", 1),
            ("help_type", "comparing_programmes", "Comparing programmes", 2),
            ("help_type", "dates_availability", "Dates & availability", 3),
            ("help_type", "pricing_inclusions", "Pricing & inclusions", 4),
            ("help_type", "something_else", "Something else", 5),
            ("wellness_need", "stress_burnout", "Stress & Burnout", 1),
            ("wellness_need", "ayurveda_panchakarma", "Ayurveda / Panchakarma", 2),
            ("wellness_need", "yoga_meditation", "Yoga & Meditation", 3),
            ("wellness_need", "rejuvenation_reset", "Rejuvenation / Reset", 4),
            ("wellness_need", "not_sure", "I’m not sure", 5),
            ("travel_window", "within_2_weeks", "Within 2 weeks", 1),
            ("travel_window", "this_month", "This month", 2),
            ("travel_window", "next_1_3_months", "Next 1–3 months", 3),
            ("travel_window", "later", "Later", 4),
            ("travel_window", "not_sure", "Not sure yet", 5)
        };

        foreach (var option in options)
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO leads.options (kind, option_key, label, sort_order, active)
                VALUES ($1, $2, $3, $4, TRUE)
                ON CONFLICT (kind, option_key) DO UPDATE SET
                    label = EXCLUDED.label,
                    sort_order = EXCLUDED.sort_order,
                    active = TRUE
                """,
                connection,
                tx);
            command.Parameters.AddWithValue(option.Kind);
            command.Parameters.AddWithValue(option.Key);
            command.Parameters.AddWithValue(option.Label);
            command.Parameters.AddWithValue(option.Order);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task UpsertSettingsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO platform.settings (key, value)
            VALUES ('whatsapp.number', '+919999000000')
            ON CONFLICT (key) DO NOTHING
            """,
            connection,
            tx);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertListingExtrasAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        CancellationToken cancellationToken)
    {
        await using var idCommand = new NpgsqlCommand(
            "SELECT id FROM catalog.retreats WHERE slug = 'ayurvedagram'",
            connection,
            tx);
        var retreatId = await idCommand.ExecuteScalarAsync(cancellationToken);
        if (retreatId is not Guid id)
        {
            return;
        }

        await using (var rooms = new NpgsqlCommand("DELETE FROM catalog.rooms WHERE retreat_id = $1", connection, tx))
        {
            rooms.Parameters.AddWithValue(id);
            await rooms.ExecuteNonQueryAsync(cancellationToken);
        }

        var roomRows = new (string Name, int Max, string Description)[]
        {
            ("Heritage room", 2, "A heritage cottage room with traditional character, shaped for rest within the campus’s restored Kerala homes."),
            ("Garden cottage", 2, "A quieter cottage stay looking onto garden space, used as programme accommodation rather than a hotel room product.")
        };
        foreach (var room in roomRows)
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO catalog.rooms (id, retreat_id, name, occupancy_max, description)
                VALUES ($1, $2, $3, $4, $5)
                """,
                connection,
                tx);
            command.Parameters.AddWithValue(DeterministicGuid($"room:{id}:{room.Name}"));
            command.Parameters.AddWithValue(id);
            command.Parameters.AddWithValue(room.Name);
            command.Parameters.AddWithValue(room.Max);
            command.Parameters.AddWithValue(room.Description);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var experts = new NpgsqlCommand("DELETE FROM catalog.experts WHERE retreat_id = $1", connection, tx))
        {
            experts.Parameters.AddWithValue(id);
            await experts.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var expert = new NpgsqlCommand(
            """
            INSERT INTO catalog.experts (id, retreat_id, name, verified, role, bio, image_url)
            VALUES ($1, $2, $3, TRUE, $4, $5, NULL)
            """,
            connection,
            tx))
        {
            expert.Parameters.AddWithValue(DeterministicGuid($"expert:{id}:harsha"));
            expert.Parameters.AddWithValue(id);
            expert.Parameters.AddWithValue("Dr. Harsha Nair");
            expert.Parameters.AddWithValue("Senior Ayurveda Physician & Psychologist");
            expert.Parameters.AddWithValue(
                "Senior consultant physician supporting guests with women’s health concerns, infertility support and mental wellbeing, integrating Ayurveda with psychology.");
            await expert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var stories = new NpgsqlCommand("DELETE FROM catalog.testimonials WHERE retreat_id = $1", connection, tx))
        {
            stories.Parameters.AddWithValue(id);
            await stories.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var story = new NpgsqlCommand(
            """
            INSERT INTO catalog.testimonials (id, retreat_id, body, consented, verified, guest_name)
            VALUES ($1, $2, $3, TRUE, TRUE, $4)
            """,
            connection,
            tx))
        {
            story.Parameters.AddWithValue(DeterministicGuid($"story:{id}:priya"));
            story.Parameters.AddWithValue(id);
            story.Parameters.AddWithValue(
                "The slower daily rhythm and the programme structure helped me rest without feeling I had to invent a holiday.");
            story.Parameters.AddWithValue("Priya");
            await story.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var programme = new NpgsqlCommand(
            "SELECT id FROM catalog.programmes WHERE retreat_id = $1 AND slug = 'rejuvenation' LIMIT 1",
            connection,
            tx);
        programme.Parameters.AddWithValue(id);
        var programmeId = await programme.ExecuteScalarAsync(cancellationToken);
        if (programmeId is Guid pid)
        {
            await using (var wipe = new NpgsqlCommand("DELETE FROM catalog.inclusions WHERE programme_id = $1", connection, tx))
            {
                wipe.Parameters.AddWithValue(pid);
                await wipe.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var item in new (string Kind, string Label)[]
                     {
                         ("included", "Accommodation"),
                         ("included", "Programme meals"),
                         ("included", "Consultations"),
                         ("included", "Included therapies"),
                         ("excluded", "Flights and transfers"),
                         ("excluded", "Personal shopping")
                     })
            {
                await using var inclusion = new NpgsqlCommand(
                    """
                    INSERT INTO catalog.inclusions (id, programme_id, kind, label)
                    VALUES ($1, $2, $3, $4)
                    """,
                    connection,
                    tx);
                inclusion.Parameters.AddWithValue(DeterministicGuid($"inc:{pid}:{item.Label}"));
                inclusion.Parameters.AddWithValue(pid);
                inclusion.Parameters.AddWithValue(item.Kind);
                inclusion.Parameters.AddWithValue(item.Label);
                await inclusion.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    private static string TitleFrom(string slug) => slug switch
    {
        "stress-burnout" => "Stress & Burnout",
        "weekend-wellness" => "Weekend Wellness",
        "weight-metabolic" => "Weight & Metabolic Wellness",
        "lifestyle-holistic" => "Lifestyle / holistic wellness",
        "nature-wellness" => "Nature-based wellness",
        "long-stay" => "Longer 7 / 14 / 21 / 28-day programmes",
        "not-sure" => "Not sure what I need",
        "detox" => "Detox / cleansing",
        _ => char.ToUpperInvariant(slug[0]) + slug[1..]
    };

    private static Guid DeterministicGuid(string key)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"healingram:{key}"));
        return new Guid(bytes.AsSpan(0, 16));
    }

    private static NpgsqlParameter Typed(string? value)
        => new() { NpgsqlDbType = NpgsqlDbType.Text, Value = (object?)value ?? DBNull.Value };
}
