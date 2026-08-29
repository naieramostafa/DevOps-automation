using DevOpsAutomation.Infrastructure.Services;
using Xunit;

namespace DevOpsAutomation.Tests;

public class KubernetesManifestTests
{
    private readonly InfrastructureGenerator _generator = new();

    [Fact]
    public void GenerateKubernetesManifest_ContainsDeploymentAndService()
    {
        var result = _generator.GenerateKubernetesManifestAsync("my-app", 8080).Result;

        Assert.Equal("k8s-manifest.yaml", result.Name);
        Assert.Contains("kind: Deployment", result.Content);
        Assert.Contains("kind: Service", result.Content);
        Assert.Equal("yaml", result.FileType);
    }

    [Fact]
    public void GenerateKubernetesManifest_UsesReplicaCount()
    {
        var result = _generator.GenerateKubernetesManifestAsync("my-app", 8080, replicas: 3).Result;

        Assert.Contains("replicas: 3", result.Content);
    }

    [Theory]
    [InlineData("node", "/health", "/ready")]
    [InlineData("go", "/healthz", "/readyz")]
    [InlineData("java", "/actuator/health", "/actuator/health")]
    [InlineData("dotnet", "/health", "/health")]
    public void GenerateKubernetesManifest_LanguageSpecificProbes(string language, string expectedLiveness, string expectedReadiness)
    {
        var result = _generator.GenerateKubernetesManifestAsync("my-app", 8080, language: language).Result;

        Assert.Contains(expectedLiveness, result.Content);
        Assert.Contains(expectedReadiness, result.Content);
    }
}
