using FluentAssertions;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.UnitTests.Models;

[TestClass]
public sealed class LlmSettingsTests
{
    [TestMethod]
    public void LlmSettings_CanBeCreated_WithRequiredProperties()
    {
        var settings = new LlmSettings
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test-api-key",
            DeploymentName = "gpt-4o",
            WhisperDeploymentName = "whisper",
        };

        settings.Endpoint.Should().Be("https://test.openai.azure.com");
        settings.ApiKey.Should().Be("test-api-key");
        settings.DeploymentName.Should().Be("gpt-4o");
        settings.WhisperDeploymentName.Should().Be("whisper");
    }

    [TestMethod]
    public void LlmSettings_Equality_WorksForRecords()
    {
        var settings1 = new LlmSettings
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "key",
            DeploymentName = "gpt-4o",
            WhisperDeploymentName = "whisper",
        };

        var settings2 = new LlmSettings
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "key",
            DeploymentName = "gpt-4o",
            WhisperDeploymentName = "whisper",
        };

        settings1.Should().Be(settings2);
    }
}
