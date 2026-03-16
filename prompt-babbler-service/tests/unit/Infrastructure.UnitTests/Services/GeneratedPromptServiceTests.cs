using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class GeneratedPromptServiceTests
{
    private readonly IGeneratedPromptRepository _promptRepository = Substitute.For<IGeneratedPromptRepository>();
    private readonly IBabbleRepository _babbleRepository = Substitute.For<IBabbleRepository>();
    private readonly ILogger<GeneratedPromptService> _logger = Substitute.For<ILogger<GeneratedPromptService>>();
    private readonly GeneratedPromptService _service;

    public GeneratedPromptServiceTests()
    {
        _service = new GeneratedPromptService(_promptRepository, _babbleRepository, _logger);
    }

    private static Babble CreateBabble(
        string id = "test-babble-id",
        string userId = "test-user-id") => new()
        {
            Id = id,
            UserId = userId,
            Title = "Test Babble",
            Text = "Test babble text.",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    private static GeneratedPrompt CreatePrompt(
        string id = "test-prompt-id",
        string babbleId = "test-babble-id",
        string userId = "test-user-id") => new()
        {
            Id = id,
            BabbleId = babbleId,
            UserId = userId,
            TemplateId = "template-1",
            TemplateName = "Test Template",
            PromptText = "Generated prompt text.",
            GeneratedAt = DateTimeOffset.UtcNow,
        };

    // ---- GetByBabbleAsync ----

    [TestMethod]
    public async Task GetByBabbleAsync_ValidOwnership_ReturnsPrompts()
    {
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(CreateBabble());
        var prompts = new List<GeneratedPrompt> { CreatePrompt() };
        _promptRepository.GetByBabbleAsync("test-babble-id", null, 20, Arg.Any<CancellationToken>())
            .Returns((prompts.AsReadOnly(), (string?)null));

        var (items, token) = await _service.GetByBabbleAsync("test-user-id", "test-babble-id");

        items.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task GetByBabbleAsync_BabbleNotFound_ThrowsInvalidOperationException()
    {
        _babbleRepository.GetByIdAsync("test-user-id", "missing", Arg.Any<CancellationToken>())
            .Returns((Babble?)null);

        var act = () => _service.GetByBabbleAsync("test-user-id", "missing");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ---- GetByIdAsync ----

    [TestMethod]
    public async Task GetByIdAsync_ValidOwnership_ReturnsPrompt()
    {
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(CreateBabble());
        var prompt = CreatePrompt();
        _promptRepository.GetByIdAsync("test-babble-id", "test-prompt-id", Arg.Any<CancellationToken>())
            .Returns(prompt);

        var result = await _service.GetByIdAsync("test-user-id", "test-babble-id", "test-prompt-id");

        result.Should().Be(prompt);
    }

    [TestMethod]
    public async Task GetByIdAsync_BabbleNotOwned_ThrowsInvalidOperationException()
    {
        _babbleRepository.GetByIdAsync("wrong-user", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns((Babble?)null);

        var act = () => _service.GetByIdAsync("wrong-user", "test-babble-id", "test-prompt-id");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ---- CreateAsync ----

    [TestMethod]
    public async Task CreateAsync_ValidOwnership_CreatesPrompt()
    {
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(CreateBabble());
        var prompt = CreatePrompt();
        _promptRepository.CreateAsync(prompt, Arg.Any<CancellationToken>())
            .Returns(prompt);

        var result = await _service.CreateAsync("test-user-id", prompt);

        result.Should().Be(prompt);
    }

    [TestMethod]
    public async Task CreateAsync_BabbleNotOwned_ThrowsInvalidOperationException()
    {
        _babbleRepository.GetByIdAsync("wrong-user", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns((Babble?)null);
        var prompt = CreatePrompt();

        var act = () => _service.CreateAsync("wrong-user", prompt);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ---- DeleteAsync ----

    [TestMethod]
    public async Task DeleteAsync_ValidOwnership_DeletesPrompt()
    {
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(CreateBabble());

        await _service.DeleteAsync("test-user-id", "test-babble-id", "test-prompt-id");

        await _promptRepository.Received(1).DeleteAsync("test-babble-id", "test-prompt-id", Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteAsync_BabbleNotOwned_ThrowsInvalidOperationException()
    {
        _babbleRepository.GetByIdAsync("wrong-user", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns((Babble?)null);

        var act = () => _service.DeleteAsync("wrong-user", "test-babble-id", "test-prompt-id");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
