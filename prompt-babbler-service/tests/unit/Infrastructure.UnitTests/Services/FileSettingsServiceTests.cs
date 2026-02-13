using System.Text.Json;
using FluentAssertions;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
public sealed class FileSettingsServiceTests
{
    [TestMethod]
    public void FileSettingsService_CanBeInstantiated()
    {
        var service = new FileSettingsService();
        service.Should().NotBeNull();
    }

    [TestMethod]
    public void JsonSerialization_RoundTrips_LlmSettings()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var settings = new LlmSettings
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test-api-key-1234",
            DeploymentName = "gpt-4o",
            WhisperDeploymentName = "whisper",
        };

        var json = JsonSerializer.Serialize(settings, options);
        var deserialized = JsonSerializer.Deserialize<LlmSettings>(json, options);

        deserialized.Should().NotBeNull();
        deserialized.Should().Be(settings);
    }

    [TestMethod]
    public void JsonDeserialization_WithInvalidJson_ReturnsNull()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var act = () => JsonSerializer.Deserialize<LlmSettings>("not valid json", options);

        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void JsonSerialization_UsesCamelCase()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var settings = new LlmSettings
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "key",
            DeploymentName = "gpt-4o",
            WhisperDeploymentName = "whisper",
        };

        var json = JsonSerializer.Serialize(settings, options);

        json.Should().Contain("\"endpoint\"");
        json.Should().Contain("\"apiKey\"");
        json.Should().Contain("\"deploymentName\"");
        json.Should().Contain("\"whisperDeploymentName\"");
    }
}
