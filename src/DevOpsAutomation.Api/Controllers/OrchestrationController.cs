using DevOpsAutomation.Core.Abstractions;
using DevOpsAutomation.Core.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsAutomation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrchestrationController : ControllerBase
{
    private readonly IAutomationEngine _engine;
    private readonly IValidator<AutomationRequest> _requestValidator;
    private readonly IValidator<NaturalLanguageRequest> _naturalValidator;

    public OrchestrationController(
        IAutomationEngine engine,
        IValidator<AutomationRequest> requestValidator,
        IValidator<NaturalLanguageRequest> naturalValidator)
    {
        _engine = engine;
        _requestValidator = requestValidator;
        _naturalValidator = naturalValidator;
    }

    [HttpPost("process")]
    public async Task<ActionResult<AutomationResult>> ProcessRequest([FromBody] AutomationRequest request)
    {
        var validation = await _requestValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await _engine.ProcessRequestAsync(request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("process-natural")]
    public async Task<ActionResult<AutomationResult>> ProcessNaturalLanguage([FromBody] NaturalLanguageRequest request)
    {
        var validation = await _naturalValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var automationRequest = new AutomationRequest { Intent = request.Prompt };
        var result = await _engine.ProcessRequestAsync(automationRequest);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}

public class NaturalLanguageRequest
{
    public string Prompt { get; set; } = string.Empty;
}
