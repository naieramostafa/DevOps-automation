using DevOpsAutomation.Infrastructure.Services;
using Xunit;

namespace DevOpsAutomation.Tests;

public class CicdWorkflowTests
{
    private readonly InfrastructureGenerator _generator = new();

    [Fact]
    public void GenerateWorkflow_ContainsBuildAndDeployJobs()
    {
        var result = _generator.GenerateGitHubActionsWorkflowAsync("my-app", "ghcr.io/myorg").Result;

        Assert.Equal("deploy.yml", result.Name);
        Assert.Contains("jobs:", result.Content);
        Assert.Contains("build:", result.Content);
        Assert.Contains("deploy:", result.Content);
    }

    [Theory]
    [InlineData("dotnet", "actions/setup-dotnet@v4")]
    [InlineData("node", "actions/setup-node@v4")]
    [InlineData("python", "actions/setup-python@v5")]
    [InlineData("go", "actions/setup-go@v5")]
    [InlineData("java", "actions/setup-java@v4")]
    public void GenerateWorkflow_LanguageSpecificSetup(string language, string expectedAction)
    {
        var result = _generator.GenerateGitHubActionsWorkflowAsync("my-app", "ghcr.io/myorg", language).Result;

        Assert.Contains(expectedAction, result.Content);
    }

    [Fact]
    public void GenerateWorkflow_IncludesRegistryTag()
    {
        var result = _generator.GenerateGitHubActionsWorkflowAsync("my-app", "ghcr.io/myorg").Result;

        Assert.Contains("ghcr.io/myorg/my-app", result.Content);
    }
}
