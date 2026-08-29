using DevOpsAutomation.Testing.Services;
using Xunit;

namespace DevOpsAutomation.Tests;

public class PlaywrightTestServiceTests
{
    private readonly PlaywrightTestService _service = new();

    [Fact]
    public void RunTests_WithoutScript_GeneratesDefaultTests()
    {
        var result = _service.RunTestsAsync("https://example.com").Result;

        Assert.True(result.Success);
        Assert.Single(result.Artifacts);
        Assert.Equal("smoke-test.spec.ts", result.Artifacts[0].Name);
        Assert.Contains("https://example.com", result.Artifacts[0].Content);
        Assert.Contains("test(", result.Artifacts[0].Content);
    }

    [Fact]
    public void RunTests_WithCustomScript_UsesIt()
    {
        const string custom = "import { test } from '@playwright/test';";

        var result = _service.RunTestsAsync("https://example.com", custom).Result;

        Assert.True(result.Success);
        Assert.Equal("custom-test.spec.ts", result.Artifacts[0].Name);
        Assert.Equal(custom, result.Artifacts[0].Content);
    }
}
