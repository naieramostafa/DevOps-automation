using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DevOpsAutomation.Core.Configuration;

namespace DevOpsAutomation.Api.Controllers;

[ApiController]
[Route("api/health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly IOptions<LlmSettings> _llmSettings;

    public HealthController(IOptions<LlmSettings> llmSettings)
    {
        _llmSettings = llmSettings;
    }

    [HttpGet]
    public ActionResult<object> Get()
    {
        var settings = _llmSettings.Value;
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            llmProvider = settings.Provider,
            llmConfigured = !string.IsNullOrWhiteSpace(settings.ApiKey),
            version = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "unknown"
        });
    }
}
