using DevOpsAutomation.Core.Abstractions;
using DevOpsAutomation.Core.Models;

namespace DevOpsAutomation.Testing.Services;

public class PlaywrightTestService : ITestAutomationService
{
    public Task<AutomationResult> RunTestsAsync(string testUrl, string? testScript = null)
    {
        try
        {
            var artifacts = new List<GeneratedArtifact>();

            if (!string.IsNullOrWhiteSpace(testScript))
            {
                artifacts.Add(new GeneratedArtifact
                {
                    Name = "custom-test.spec.ts",
                    Content = testScript,
                    FileType = "typescript"
                });
            }
            else
            {
                var defaultTest = GenerateDefaultTest(testUrl);
                artifacts.Add(new GeneratedArtifact
                {
                    Name = "smoke-test.spec.ts",
                    Content = defaultTest,
                    FileType = "typescript"
                });
            }

            return Task.FromResult(new AutomationResult
            {
                Success = true,
                Message = $"Generated Playwright tests for {testUrl}",
                Artifacts = artifacts
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new AutomationResult
            {
                Success = false,
                Message = $"Test generation failed: {ex.Message}"
            });
        }
    }

    private static string GenerateDefaultTest(string url)
    {
        return $$"""
            import {{'{'}} test, expect {{'}'}} from '@playwright/test';

            test('homepage loads successfully', async ({{'{'}} page {{'}'}}) => {
              await page.goto('{{url}}');
              await expect(page).toHaveTitle(/.*/);
            });

            test('page responds with 200', async ({{'{'}} page {{'}'}}) => {
              const response = await page.goto('{{url}}');
              expect(response?.status()).toBe(200);
            });

            test('page has body content', async ({{'{'}} page {{'}'}}) => {
              await page.goto('{{url}}');
              const body = await page.textContent('body');
              expect(body?.length).toBeGreaterThan(0);
            });
            """;
    }
}
