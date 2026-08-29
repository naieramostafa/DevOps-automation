using DevOpsAutomation.Core.Abstractions;
using DevOpsAutomation.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsAutomation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InfrastructureController : ControllerBase
{
    private readonly IInfrastructureGenerator _generator;

    public InfrastructureController(IInfrastructureGenerator generator)
    {
        _generator = generator;
    }

    [HttpPost("dockerfile")]
    public async Task<ActionResult<GeneratedArtifact>> GenerateDockerfile(
        [FromQuery] string serviceName = "my-service",
        [FromQuery] int port = 8080,
        [FromQuery] string language = "dotnet")
    {
        if (port is < 1 or > 65535)
            return BadRequest(new { error = "Port must be between 1 and 65535." });

        var result = await _generator.GenerateDockerfileAsync(serviceName, port, language);
        return Ok(result);
    }

    [HttpPost("kubernetes")]
    public async Task<ActionResult<GeneratedArtifact>> GenerateKubernetes(
        [FromQuery] string serviceName = "my-service",
        [FromQuery] int port = 8080,
        [FromQuery] int replicas = 1,
        [FromQuery] string language = "dotnet")
    {
        if (port is < 1 or > 65535)
            return BadRequest(new { error = "Port must be between 1 and 65535." });
        if (replicas is < 1 or > 100)
            return BadRequest(new { error = "Replicas must be between 1 and 100." });

        var result = await _generator.GenerateKubernetesManifestAsync(serviceName, port, replicas, language);
        return Ok(result);
    }

    [HttpPost("cicd")]
    public async Task<ActionResult<GeneratedArtifact>> GenerateCicd(
        [FromQuery] string serviceName = "my-service",
        [FromQuery] string dockerRegistry = "ghcr.io/myorg",
        [FromQuery] string language = "dotnet")
    {
        var result = await _generator.GenerateGitHubActionsWorkflowAsync(serviceName, dockerRegistry, language);
        return Ok(result);
    }
}
