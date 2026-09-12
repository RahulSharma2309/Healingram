using Xunit;

namespace Healingram.Gateway.Tests;

public class GatewayIsolationTests
{
    private static readonly string[] ForbiddenModuleTokens =
    [
        "Healingram.Modules.Catalog",
        "Healingram.Modules.Booking",
        "Healingram.Modules.Payment"
    ];

    [Fact]
    public void Program_and_project_do_not_reference_business_modules()
    {
        var program = File.ReadAllText(FindGatewayFile("Program.cs"));
        var project = File.ReadAllText(FindGatewayFile("Healingram.Gateway.csproj"));

        foreach (var token in ForbiddenModuleTokens)
        {
            Assert.DoesNotContain(token, program);
            Assert.DoesNotContain(token, project);
        }

        Assert.DoesNotContain("Healingram.Modules.", program);
        Assert.DoesNotContain("Healingram.Modules.", project);
    }

    [Fact]
    public void Gateway_has_no_business_writes()
    {
        var program = File.ReadAllText(FindGatewayFile("Program.cs"));

        Assert.DoesNotContain("ExecuteSql", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DbContext", program);
        Assert.DoesNotContain("INSERT ", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE ", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MapPost", program);
        Assert.DoesNotContain("MapPut", program);
        Assert.DoesNotContain("MapPatch", program);
        Assert.DoesNotContain("MapDelete", program);
    }

    private static string FindGatewayFile(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidates = new[]
            {
                Path.Combine(dir.FullName, "backend", "src", "Healingram.Gateway", fileName),
                Path.Combine(dir.FullName, "src", "Healingram.Gateway", fileName)
            };

            var match = candidates.FirstOrDefault(File.Exists);
            if (match is not null)
            {
                return match;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not find Healingram.Gateway/{fileName} from {AppContext.BaseDirectory}.");
    }
}
