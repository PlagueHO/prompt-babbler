using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PromptBabbler.Api.Controllers;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.UnitTests.Controllers;

[TestClass]
public sealed class SettingsControllerTests
{
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _controller = new SettingsController(_settingsService, _httpClientFactory);
    }

    [TestMethod]
    public async Task GetSettings_WhenNotConfigured_ReturnsEmptySettings()
    {
        _settingsService.GetSettingsAsync(Arg.Any<CancellationToken>()).Returns((LlmSettings?)null);

        var result = await _controller.GetSettings(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LlmSettingsResponse>().Subject;
        response.IsConfigured.Should().BeFalse();
        response.Endpoint.Should().BeEmpty();
        response.ApiKeyHint.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetSettings_WhenConfigured_ReturnsMaskedApiKey()
    {
        var settings = new LlmSettings
        {
            Endpoint = "https://myendpoint.openai.azure.com",
            ApiKey = "my-secret-api-key-1234",
            DeploymentName = "gpt-4o",
            WhisperDeploymentName = "whisper",
        };
        _settingsService.GetSettingsAsync(Arg.Any<CancellationToken>()).Returns(settings);

        var result = await _controller.GetSettings(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LlmSettingsResponse>().Subject;
        response.IsConfigured.Should().BeTrue();
        response.Endpoint.Should().Be("https://myendpoint.openai.azure.com");
        response.ApiKeyHint.Should().Be("...1234");
        response.DeploymentName.Should().Be("gpt-4o");
        response.WhisperDeploymentName.Should().Be("whisper");
    }

    [TestMethod]
    public async Task UpdateSettings_WithValidRequest_SavesAndReturnsMasked()
    {
        var request = new LlmSettingsSaveRequest
        {
            Endpoint = "https://myendpoint.openai.azure.com",
            ApiKey = "my-secret-api-key-5678",
            DeploymentName = "gpt-4o",
            WhisperDeploymentName = "whisper",
        };

        var result = await _controller.UpdateSettings(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LlmSettingsResponse>().Subject;
        response.IsConfigured.Should().BeTrue();
        response.ApiKeyHint.Should().Be("...5678");
        await _settingsService.Received(1).SaveSettingsAsync(
            Arg.Is<LlmSettings>(s =>
                s.Endpoint == "https://myendpoint.openai.azure.com" &&
                s.DeploymentName == "gpt-4o"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateSettings_WithEmptyEndpoint_ReturnsBadRequest()
    {
        var request = new LlmSettingsSaveRequest
        {
            Endpoint = "",
            ApiKey = "key",
            DeploymentName = "gpt-4o",
            WhisperDeploymentName = "whisper",
        };

        var result = await _controller.UpdateSettings(request, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Contain("Endpoint");
    }

    [TestMethod]
    public async Task UpdateSettings_WithEmptyApiKey_ReturnsBadRequest()
    {
        var request = new LlmSettingsSaveRequest
        {
            Endpoint = "https://myendpoint.openai.azure.com",
            ApiKey = "",
            DeploymentName = "gpt-4o",
            WhisperDeploymentName = "whisper",
        };

        var result = await _controller.UpdateSettings(request, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Contain("API key");
    }

    [TestMethod]
    public async Task TestConnection_WhenNotConfigured_Returns422()
    {
        _settingsService.GetSettingsAsync(Arg.Any<CancellationToken>()).Returns((LlmSettings?)null);

        var result = await _controller.TestConnection(CancellationToken.None);

        var unprocessable = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problem = unprocessable.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(422);
    }
}
