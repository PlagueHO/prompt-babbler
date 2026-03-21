using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class BabbleServiceTests
{
    private readonly IBabbleRepository _babbleRepository = Substitute.For<IBabbleRepository>();
    private readonly IGeneratedPromptRepository _generatedPromptRepository = Substitute.For<IGeneratedPromptRepository>();
    private readonly ILogger<BabbleService> _logger = Substitute.For<ILogger<BabbleService>>();
    private readonly BabbleService _service;

    public BabbleServiceTests()
    {
        _service = new BabbleService(_babbleRepository, _generatedPromptRepository, _logger);
    }

    private static Babble CreateBabble(
        string id = "test-babble-id",
        string userId = "test-user-id") => new()
        {
            Id = id,
            UserId = userId,
            Title = "Test Babble",
            Text = "This is a test babble transcription.",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    // ---- GetByUserAsync ----

    [TestMethod]
    public async Task GetByUserAsync_DelegatesToRepository()
    {
        var expected = new List<Babble> { CreateBabble() };
        _babbleRepository.GetByUserAsync("user-1", null, 20, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns((expected.AsReadOnly(), (string?)null));

        var (items, token) = await _service.GetByUserAsync("user-1");

        items.Should().HaveCount(1);
        token.Should().BeNull();
    }

    // ---- GetByIdAsync ----

    [TestMethod]
    public async Task GetByIdAsync_DelegatesToRepository()
    {
        var babble = CreateBabble();
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(babble);

        var result = await _service.GetByIdAsync("test-user-id", "test-babble-id");

        result.Should().Be(babble);
    }

    // ---- CreateAsync ----

    [TestMethod]
    public async Task CreateAsync_DelegatesToRepository()
    {
        var babble = CreateBabble();
        _babbleRepository.CreateAsync(babble, Arg.Any<CancellationToken>())
            .Returns(babble);

        var result = await _service.CreateAsync(babble);

        result.Should().Be(babble);
    }

    // ---- UpdateAsync ----

    [TestMethod]
    public async Task UpdateAsync_ExistingBabble_DelegatesToRepository()
    {
        var babble = CreateBabble();
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(babble);
        _babbleRepository.UpdateAsync(babble, Arg.Any<CancellationToken>())
            .Returns(babble);

        var result = await _service.UpdateAsync("test-user-id", babble);

        result.Should().Be(babble);
    }

    [TestMethod]
    public async Task UpdateAsync_NonExistentBabble_ThrowsInvalidOperationException()
    {
        var babble = CreateBabble();
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns((Babble?)null);

        var act = () => _service.UpdateAsync("test-user-id", babble);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ---- DeleteAsync ----

    [TestMethod]
    public async Task DeleteAsync_CascadeDeletesGeneratedPrompts()
    {
        var babble = CreateBabble();
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(babble);

        await _service.DeleteAsync("test-user-id", "test-babble-id");

        await _generatedPromptRepository.Received(1).DeleteByBabbleAsync("test-babble-id", Arg.Any<CancellationToken>());
        await _babbleRepository.Received(1).DeleteAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteAsync_DeletesGeneratedPromptsBeforeBabble()
    {
        var callOrder = new List<string>();
        _generatedPromptRepository.DeleteByBabbleAsync("test-babble-id", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("prompts"));
        _babbleRepository.DeleteAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("babble"));

        await _service.DeleteAsync("test-user-id", "test-babble-id");

        callOrder.Should().ContainInOrder("prompts", "babble");
    }

    // ---- SetPinAsync ----

    [TestMethod]
    public async Task SetPinAsync_WithValidBabble_CallsRepositoryAndReturnsResult()
    {
        // Arrange
        var babble = CreateBabble() with { IsPinned = true };
        _babbleRepository.SetPinAsync("test-user-id", "test-babble-id", true, Arg.Any<CancellationToken>())
            .Returns(babble);

        // Act
        var result = await _service.SetPinAsync("test-user-id", "test-babble-id", true);

        // Assert
        result.Should().Be(babble);
        result.IsPinned.Should().BeTrue();
        await _babbleRepository.Received(1).SetPinAsync("test-user-id", "test-babble-id", true, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SetPinAsync_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        _babbleRepository.SetPinAsync("test-user-id", "missing-id", true, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Babble not found"));

        // Act
        var act = () => _service.SetPinAsync("test-user-id", "missing-id", true);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }
}
