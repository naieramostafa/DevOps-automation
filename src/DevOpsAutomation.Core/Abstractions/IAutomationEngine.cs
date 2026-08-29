using DevOpsAutomation.Core.Models;

namespace DevOpsAutomation.Core.Abstractions;

public interface IAutomationEngine
{
    Task<AutomationResult> ProcessRequestAsync(AutomationRequest request);
}

public interface IInfrastructureGenerator
{
    Task<GeneratedArtifact> GenerateDockerfileAsync(string serviceName, int port, string language = "dotnet");
    Task<GeneratedArtifact> GenerateKubernetesManifestAsync(string serviceName, int port, int replicas = 1, string language = "dotnet");
    Task<GeneratedArtifact> GenerateGitHubActionsWorkflowAsync(string serviceName, string dockerRegistry, string language = "dotnet");
}

public interface ITestAutomationService
{
    Task<AutomationResult> RunTestsAsync(string testUrl, string? testScript = null);
}

public interface ILlmService
{
    Task<string> ParseIntentAsync(string userInput);
    Task<string> GenerateContentAsync(string prompt);
}
