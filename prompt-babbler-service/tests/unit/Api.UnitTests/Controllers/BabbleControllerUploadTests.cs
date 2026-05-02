using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PromptBabbler.Api.Controllers;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.UnitTests.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class BabbleControllerUploadTests
{
    private const string TestUserId = "00000000-0000-0000-0000-000000000000";

    private readonly IBabbleService _babbleService = Substitute.For<IBabbleService>();
    private readonly IPromptGenerationService _promptGenerationService = Substitute.For<IPromptGenerationService>();
    private readonly IPromptTemplateService _templateService = Substitute.For<IPromptTemplateService>();
    private readonly IGeneratedPromptService _generatedPromptService = Substitute.For<IGeneratedPromptService>();
    private readonly IFileTranscriptionService _fileTranscriptionService = Substitute.For<IFileTranscriptionService>();
    private readonly ILogger<BabbleController> _logger = Substitute.For<ILogger<BabbleController>>();
    private readonly BabbleController _controller;

    public BabbleControllerUploadTests()
    {
        _controller = new BabbleController(
            _babbleService, _promptGenerationService, _templateService, _generatedPromptService, _fileTranscriptionService, _logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", TestUserId),
            new Claim("preferred_username", "test@contoso.com"),
        ], "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };
    }

    private static IFormFile CreateFormFile(
        string contentType = "audio/mpeg",
        long length = 1024,
        string fileName = "test.mp3")
    {
        var file = Substitute.For<IFormFile>();
        file.ContentType.Returns(contentType);
        file.Length.Returns(length);
        file.FileName.Returns(fileName);
        file.OpenReadStream().Returns(new MemoryStream(new byte[length > 0 ? (int)length : 0]));
        return file;
    }

    private static Babble CreateBabble(
        string id = "upload-babble-id",
        string title = "Transcribed Title",
        string text = "Transcribed text from audio.") => new()
        {
            Id = id,
            UserId = TestUserId,
            Title = title,
            Text = text,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    // ---- POST /api/babbles/upload ----

    [TestMethod]
    public async Task UploadAudio_NullFile_ReturnsBadRequest()
    {
        var result = await _controller.UploadAudio(null!, null, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task UploadAudio_EmptyFile_ReturnsBadRequest()
    {
        var file = CreateFormFile(length: 0);

        var result = await _controller.UploadAudio(file, null, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task UploadAudio_UnsupportedContentType_ReturnsBadRequest()
    {
        var file = CreateFormFile(contentType: "video/mp4");

        var result = await _controller.UploadAudio(file, null, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task UploadAudio_ValidMp3_ReturnsCreated()
    {
        var file = CreateFormFile(contentType: "audio/mpeg");
        var created = CreateBabble();

        _fileTranscriptionService
            .TranscribeAsync(Arg.Any<Stream>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Transcribed text from audio.");

        _babbleService
            .CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(created);

        var result = await _controller.UploadAudio(file, null, null, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeOfType<BabbleResponse>();
    }

    [TestMethod]
    public async Task UploadAudio_EmptyTranscription_ReturnsBadRequest()
    {
        var file = CreateFormFile(contentType: "audio/mpeg");

        _fileTranscriptionService
            .TranscribeAsync(Arg.Any<Stream>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(string.Empty);

        var result = await _controller.UploadAudio(file, null, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task UploadAudio_ValidFile_CreatesBabbleWithGeneratedTitle()
    {
        const string longText = "This is a very long transcription that exceeds fifty characters and should be truncated.";
        var file = CreateFormFile(contentType: "audio/mpeg");
        var created = CreateBabble(text: longText);

        _fileTranscriptionService
            .TranscribeAsync(Arg.Any<Stream>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(longText);

        _babbleService
            .CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var babble = ci.Arg<Babble>();
                // Title should be at most 50 chars + "..." (53 total) or ≤50
                babble.Title.Length.Should().BeLessThanOrEqualTo(53);
                return babble;
            });

        var result = await _controller.UploadAudio(file, null, null, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        await _babbleService.Received(1).CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UploadAudio_ValidWavFile_ReturnsCreated()
    {
        var file = CreateFormFile(contentType: "audio/wav", fileName: "test.wav");
        var created = CreateBabble();

        _fileTranscriptionService
            .TranscribeAsync(Arg.Any<Stream>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Audio content transcribed.");

        _babbleService
            .CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(created);

        var result = await _controller.UploadAudio(file, null, null, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [TestMethod]
    public async Task UploadAudio_TranscriptionServiceThrows_Returns502()
    {
        var file = CreateFormFile(contentType: "audio/mpeg", fileName: "test.mp3");

        _fileTranscriptionService
            .TranscribeAsync(Arg.Any<Stream>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Azure service unavailable"));

        var result = await _controller.UploadAudio(file, null, null, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(502);
    }

    [TestMethod]
    public async Task UploadAudio_ValidContentTypeInvalidExtension_ReturnsBadRequest()
    {
        var file = CreateFormFile(contentType: "audio/mpeg", fileName: "malicious.exe");

        var result = await _controller.UploadAudio(file, null, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task UploadAudio_InvalidLanguageParameter_ReturnsBadRequest()
    {
        var file = CreateFormFile(contentType: "audio/mpeg", fileName: "test.mp3");

        var result = await _controller.UploadAudio(file, null, "invalid-language-code-that-is-way-too-long", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task UploadAudio_WithTitle_CreatesBabbleWithProvidedTitle()
    {
        const string providedTitle = "My Custom Babble Title";
        var file = CreateFormFile(contentType: "audio/mpeg");
        var created = CreateBabble(title: providedTitle);

        _fileTranscriptionService
            .TranscribeAsync(Arg.Any<Stream>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Transcribed text from audio.");

        _babbleService
            .CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var babble = ci.Arg<Babble>();
                babble.Title.Should().Be(providedTitle);
                return babble;
            });

        var result = await _controller.UploadAudio(file, providedTitle, null, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        await _babbleService.Received(1).CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UploadAudio_TitleTooLong_ReturnsBadRequest()
    {
        var file = CreateFormFile(contentType: "audio/mpeg", fileName: "test.mp3");
        var tooLongTitle = new string('x', 201);

        var result = await _controller.UploadAudio(file, tooLongTitle, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
