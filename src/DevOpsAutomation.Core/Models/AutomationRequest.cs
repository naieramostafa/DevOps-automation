namespace DevOpsAutomation.Core.Models;

public class AutomationRequest
{
    public string Intent { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class AutomationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<GeneratedArtifact> Artifacts { get; set; } = new();
}

public class GeneratedArtifact
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
}

public enum RequestType
{
    Infrastructure,
    Testing,
    CICD
}
