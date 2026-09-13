namespace Healingram.Modules.Catalog.Persistence;

internal static class CatalogPublicSchema
{
    internal const string ResourceName = "Healingram.Modules.Catalog.sql.002_catalog_public.sql";

    internal static string Load()
    {
        var assembly = typeof(CatalogPublicSchema).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is not null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        foreach (var path in FileCandidates())
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }

        throw new FileNotFoundException("002_catalog_public.sql was not embedded or found on disk.");
    }

    private static IEnumerable<string> FileCandidates()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "db", "002_catalog_public.sql");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "db", "002_catalog_public.sql");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "backend", "db", "002_catalog_public.sql");

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            yield return Path.Combine(dir.FullName, "db", "002_catalog_public.sql");
            dir = dir.Parent;
        }
    }
}
