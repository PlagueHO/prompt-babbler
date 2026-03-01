using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PromptBabbler.Api.Controllers;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.UnitTests.Controllers;

[TestClass]
public sealed class TranscriptionControllerTests
{
    private readonly ITranscriptionService _transcriptionService = Substitute.For<ITranscriptionService>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly TranscriptionController _controller;

    public TranscriptionControllerTests()
    {
        _controller = new TranscriptionController(_transcriptionService, _settingsService);
    }

    [TestMethod]
    public async Task Transcribe_WithNoFile_ReturnsBadRequest()
    {
        var result = await _controller.Transcribe(null!, null, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(400);
        problem.Detail.Should().Contain("audio file");
    }

    [TestMethod]
    public async Task Transcribe_WhenSettingsNotConfigured_Returns422()
    {
        var file = CreateFormFile("test.wav", 1024);
        _settingsService.GetSettingsAsync(Arg.Any<CancellationToken>()).Returns((LlmSettings?)null);

        var result = await _controller.Transcribe(file, null, CancellationToken.None);

        var unprocessable = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problem = unprocessable.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(422);
    }

    [TestMethod]
    public async Task Transcribe_WithValidFile_ReturnsTranscription()
    {
        var file = CreateFormFile("test.wav", 1024);
        var settings = new LlmSettings
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test-key",
            DeploymentName = "gpt-4o",
            WhisperDeploymentName = "whisper",
        };
        _settingsService.GetSettingsAsync(Arg.Any<CancellationToken>()).Returns(settings);

        var transcriptionResult = new TranscriptionResult
        {
            Text = "Hello world",
            Language = "en",
            Duration = 2.5f,
        };
        _transcriptionService.TranscribeAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(transcriptionResult);

        var result = await _controller.Transcribe(file, "en", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TranscriptionResponse>().Subject;
        response.Text.Should().Be("Hello world");
        response.Language.Should().Be("en");
        response.Duration.Should().Be(2.5f);
    }

    private static IFormFile CreateFormFile(string fileName, int sizeInBytes)
    {
        var content = new byte[sizeInBytes];
        var stream = new MemoryStream(content);
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.Length.Returns(sizeInBytes);
        file.OpenReadStream().Returns(stream);
        return file;
    }
}
