using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class AzureFastTranscriptionServiceTests
{
    private readonly ITranscriptionClientWrapper _transcriptionClient = Substitute.For<ITranscriptionClientWrapper>();
    private readonly ILogger<AzureFastTranscriptionService> _logger = Substitute.For<ILogger<AzureFastTranscriptionService>>();
    private readonly AzureFastTranscriptionService _service;

    public AzureFastTranscriptionServiceTests()
    {
        _service = new AzureFastTranscriptionService(_transcriptionClient, _logger);
    }

    // ---- TranscribeAsync ----

    [TestMethod]
    public async Task TranscribeAsync_ValidAudioStream_ReturnsTranscribedText()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        _transcriptionClient
            .TranscribeAsync(stream, "en-US", Arg.Any<CancellationToken>())
            .Returns("Hello world");

        var result = await _service.TranscribeAsync(stream, "en-US");

        result.Should().Be("Hello world");
    }

    [TestMethod]
    public async Task TranscribeAsync_NullLanguage_DefaultsToEnUS()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        _transcriptionClient
            .TranscribeAsync(stream, "en-US", Arg.Any<CancellationToken>())
            .Returns("Default locale text");

        var result = await _service.TranscribeAsync(stream, null);

        result.Should().Be("Default locale text");
        await _transcriptionClient.Received(1).TranscribeAsync(stream, "en-US", Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TranscribeAsync_CustomLanguage_UsesProvidedLocale()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        _transcriptionClient
            .TranscribeAsync(stream, "fr-FR", Arg.Any<CancellationToken>())
            .Returns("Bonjour le monde");

        var result = await _service.TranscribeAsync(stream, "fr-FR");

        result.Should().Be("Bonjour le monde");
        await _transcriptionClient.Received(1).TranscribeAsync(stream, "fr-FR", Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TranscribeAsync_EmptyResult_ReturnsEmptyString()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        _transcriptionClient
            .TranscribeAsync(stream, "en-US", Arg.Any<CancellationToken>())
            .Returns(string.Empty);

        var result = await _service.TranscribeAsync(stream, "en-US");

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task TranscribeAsync_PassesCancellationToken()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        using var cts = new CancellationTokenSource();
        _transcriptionClient
            .TranscribeAsync(stream, "en-US", cts.Token)
            .Returns("Some text");

        await _service.TranscribeAsync(stream, "en-US", cts.Token);

        await _transcriptionClient.Received(1).TranscribeAsync(stream, "en-US", cts.Token);
    }
}
