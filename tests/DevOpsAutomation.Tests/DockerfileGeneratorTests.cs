using DevOpsAutomation.Infrastructure.Services;
using Xunit;

namespace DevOpsAutomation.Tests;

public class DockerfileGeneratorTests
{
    private readonly InfrastructureGenerator _generator = new();

    [Theory]
    [InlineData("dotnet", "mcr.microsoft.com/dotnet/aspnet:8.0")]
    [InlineData("node", "node:20-alpine")]
    [InlineData("python", "python:3.12-slim")]
    [InlineData("go", "golang:1.22")]
    [InlineData("java", "eclipse-temurin:21-jdk")]
    public void GenerateDockerfile_ReturnsCorrectBaseImage(string language, string expectedImage)
    {
        var result = _generator.GenerateDockerfileAsync("my-app", 8080, language).Result;

        Assert.Equal("Dockerfile", result.Name);
        Assert.Contains(expectedImage, result.Content);
        Assert.Equal("dockerfile", result.FileType);
    }

    [Fact]
    public void GenerateDockerfile_DefaultLanguage_IsDotNet()
    {
        var result = _generator.GenerateDockerfileAsync("my-app", 5000).Result;

        Assert.Contains("mcr.microsoft.com/dotnet/aspnet:8.0", result.Content);
        Assert.Contains("EXPOSE 5000", result.Content);
        Assert.Contains("dotnet", result.Content);
    }

    [Fact]
    public void GenerateDockerfile_Node_UsesExposedPort()
    {
        var result = _generator.GenerateDockerfileAsync("my-app", 3000, "node").Result;

        Assert.Contains("EXPOSE 3000", result.Content);
        Assert.Contains("npm install", result.Content);
    }
}
