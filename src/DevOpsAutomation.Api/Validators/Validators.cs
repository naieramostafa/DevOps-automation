using DevOpsAutomation.Api.Controllers;
using DevOpsAutomation.Core.Models;
using FluentValidation;

namespace DevOpsAutomation.Api.Validators;

public class NaturalLanguageRequestValidator : AbstractValidator<NaturalLanguageRequest>
{
    public NaturalLanguageRequestValidator()
    {
        RuleFor(x => x.Prompt)
            .NotEmpty().WithMessage("Prompt is required.")
            .MaximumLength(1000).WithMessage("Prompt cannot exceed 1000 characters.");
    }
}

public class AutomationRequestValidator : AbstractValidator<AutomationRequest>
{
    private static readonly string[] AllowedTypes = ["Infrastructure", "Testing", "CICD"];

    public AutomationRequestValidator()
    {
        RuleFor(x => x.RequestType)
            .NotEmpty().WithMessage("RequestType is required.")
            .Must(t => AllowedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"RequestType must be one of: {string.Join(", ", AllowedTypes)}.");
    }
}

public class TestRunRequestValidator : AbstractValidator<TestRunRequest>
{
    public TestRunRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Url is required.")
            .Must(BeValidUrl).WithMessage("Url must be a valid absolute http/https URL.");
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
