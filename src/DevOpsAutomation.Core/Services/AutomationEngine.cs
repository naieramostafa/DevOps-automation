using System.Text.Json;
using DevOpsAutomation.Core.Abstractions;
using DevOpsAutomation.Core.Models;

namespace DevOpsAutomation.Core.Services;

public class AutomationEngine : IAutomationEngine
{
    private readonly ILlmService _llmService;
    private readonly IInfrastructureGenerator _infraGenerator;
    private readonly ITestAutomationService _testService;

    public AutomationEngine(
        ILlmService llmService,
        IInfrastructureGenerator infraGenerator,
        ITestAutomationService testService)
    {
        _llmService = llmService;
        _infraGenerator = infraGenerator;
        _testService = testService;
    }

    public async Task<AutomationResult> ProcessRequestAsync(AutomationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RequestType))
        {
            var parsed = await _llmService.ParseIntentAsync(request.Intent);
            var parsedJson = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(parsed);
            if (parsedJson is null)
                return new AutomationResult { Success = false, Message = "Failed to parse intent" };

            request.RequestType = parsedJson.GetValueOrDefault("requestType").GetString() ?? "";
            var parameters = parsedJson.GetValueOrDefault("parameters");
            if (parameters.ValueKind == JsonValueKind.Object)
            {
                foreach (var param in parameters.EnumerateObject())
                {
                    request.Parameters[param.Name] = JsonElementToString(param.Value);
                }
            }
        }

        return request.RequestType.ToLowerInvariant() switch
        {
            "infrastructure" or "infra" => await HandleInfrastructureAsync(request),
            "testing" or "test" => await HandleTestingAsync(request),
            "cicd" => await HandleCicdAsync(request),
            _ => new AutomationResult { Success = false, Message = $"Unknown request type: {request.RequestType}" }
        };
    }

    private async Task<AutomationResult> HandleInfrastructureAsync(AutomationRequest request)
    {
        var serviceName = request.Parameters.GetValueOrDefault("serviceName", "my-service");
        var language = request.Parameters.GetValueOrDefault("language", "dotnet");

        if (!int.TryParse(request.Parameters.GetValueOrDefault("port", "8080"), out var port) || port is < 1 or > 65535)
            return new AutomationResult { Success = false, Message = "Invalid 'port' parameter. Must be an integer between 1 and 65535." };

        if (!int.TryParse(request.Parameters.GetValueOrDefault("replicas", "1"), out var replicas) || replicas < 1 || replicas > 100)
            return new AutomationResult { Success = false, Message = "Invalid 'replicas' parameter. Must be an integer between 1 and 100." };

        var dockerfile = await _infraGenerator.GenerateDockerfileAsync(serviceName, port, language);
        var k8sManifest = await _infraGenerator.GenerateKubernetesManifestAsync(serviceName, port, replicas, language);

        return new AutomationResult
        {
            Success = true,
            Message = $"Generated infrastructure artifacts for {serviceName}",
            Artifacts = [dockerfile, k8sManifest]
        };
    }

    private async Task<AutomationResult> HandleTestingAsync(AutomationRequest request)
    {
        var testUrl = request.Parameters.GetValueOrDefault("url", "http://localhost:5000");
        var testScript = request.Parameters.GetValueOrDefault("testScript");
        return await _testService.RunTestsAsync(testUrl, testScript);
    }

    private async Task<AutomationResult> HandleCicdAsync(AutomationRequest request)
    {
        var serviceName = request.Parameters.GetValueOrDefault("serviceName", "my-service");
        var dockerRegistry = request.Parameters.GetValueOrDefault("dockerRegistry", "ghcr.io/myorg");
        var language = request.Parameters.GetValueOrDefault("language", "dotnet");

        var workflow = await _infraGenerator.GenerateGitHubActionsWorkflowAsync(serviceName, dockerRegistry, language);

        return new AutomationResult
        {
            Success = true,
            Message = $"Generated CI/CD workflow for {serviceName}",
            Artifacts = [workflow]
        };
    }

    private static string JsonElementToString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? "",
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => "",
        _ => element.GetRawText()
    };
}
