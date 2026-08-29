using System.Text;
using System.Text.Json;
using DevOpsAutomation.Core.Abstractions;
using DevOpsAutomation.Core.Configuration;
using Microsoft.Extensions.Options;

namespace DevOpsAutomation.Core.Services;

public class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmSettings _settings;

    public LlmService(HttpClient httpClient, IOptions<LlmSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<string> ParseIntentAsync(string userInput)
    {
        var systemPrompt = """
            You are a DevOps automation assistant. Parse the user's request and extract:
            1. The type of request (Infrastructure, Testing, or CICD)
            2. Key parameters (service name, port, replicas, etc.)
            3. Return as JSON with fields: requestType, parameters (object)

            Example: "Deploy a new microservice on port 8080"
            Response: {"requestType": "Infrastructure", "parameters": {"action": "deploy", "port": "8080"}}
            """;

        return await CallLlmAsync(systemPrompt, userInput);
    }

    public async Task<string> GenerateContentAsync(string prompt)
    {
        return await CallLlmAsync("You are a helpful DevOps assistant.", prompt);
    }

    private async Task<string> CallLlmAsync(string systemPrompt, string userInput)
    {
        if (_settings.Provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            return await CallAzureOpenAIAsync(systemPrompt, userInput);
        }
        if (_settings.Provider.Equals("Groq", StringComparison.OrdinalIgnoreCase))
        {
            return await CallCompatibleApiAsync(systemPrompt, userInput, "https://api.groq.com/openai/v1/chat/completions");
        }
        return await CallCompatibleApiAsync(systemPrompt, userInput, "https://api.openai.com/v1/chat/completions");
    }

    private async Task<string> CallAzureOpenAIAsync(string systemPrompt, string userInput)
    {
        var url = $"{_settings.Endpoint.TrimEnd('/')}/openai/deployments/{_settings.DeploymentName}/chat/completions?api-version=2024-02-15-preview";
        var requestBody = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userInput }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        return await SendAndParseAsync(request);
    }

    private async Task<string> CallCompatibleApiAsync(string systemPrompt, string userInput, string baseUrl)
    {
        var requestBody = new
        {
            model = _settings.DeploymentName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userInput }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, baseUrl);
        request.Headers.Add("Authorization", $"Bearer {_settings.ApiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        return await SendAndParseAsync(request);
    }

    private async Task<string> SendAndParseAsync(HttpRequestMessage request)
    {
        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"LLM API error: {(int)response.StatusCode} {response.StatusCode} - {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }
}
