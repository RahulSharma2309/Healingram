using Xunit;

namespace Healingram.BuildingBlocks.Tests;

public class CiWorkflowTests
{
    private static readonly string Workflow = File.ReadAllText(FindWorkflowPath());

    [Fact]
    public void Workflow_runs_on_pull_requests_to_the_iteration_branch()
    {
        Assert.Contains("pull_request:", Workflow);
        Assert.Contains("feature/v1-iteration-1", Workflow);
    }

    [Fact]
    public void Frontend_job_installs_and_builds_the_react_app()
    {
        Assert.Contains("npm install --no-audit --fund=false", Workflow);
        Assert.Contains("npm run build", Workflow);
    }

    [Fact]
    public void Backend_job_builds_and_runs_dotnet_tests()
    {
        Assert.Contains("dotnet restore backend/Healingram.slnx", Workflow);
        Assert.Contains("dotnet build backend/Healingram.slnx", Workflow);
        Assert.Contains("dotnet test backend/Healingram.slnx", Workflow);
    }

    [Fact]
    public void Test_and_build_steps_do_not_continue_on_error()
    {
        Assert.DoesNotContain("continue-on-error", Workflow);
    }

    private static string FindWorkflowPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, ".github", "workflows", "ci.yml");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Could not find .github/workflows/ci.yml from the test output directory.");
    }
}
