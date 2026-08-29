using DevOpsAutomation.Core.Abstractions;
using DevOpsAutomation.Core.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsAutomation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TestingController : ControllerBase
{
    private readonly ITestAutomationService _testService;
    private readonly IValidator<TestRunRequest> _validator;

    public TestingController(ITestAutomationService testService, IValidator<TestRunRequest> validator)
    {
        _testService = testService;
        _validator = validator;
    }

    [HttpPost("run")]
    public async Task<ActionResult<AutomationResult>> RunTests([FromBody] TestRunRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await _testService.RunTestsAsync(request.Url, request.TestScript);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}

public class TestRunRequest
{
    public string Url { get; set; } = "http://localhost:5000";
    public string? TestScript { get; set; }
}
