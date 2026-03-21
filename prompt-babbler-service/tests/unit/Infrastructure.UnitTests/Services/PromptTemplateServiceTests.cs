using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class PromptTemplateServiceTests
{
    private const string TestUserId = "user-1";

    private readonly IPromptTemplateRepository _repository = Substitute.For<IPromptTemplateRepository>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ILogger<PromptTemplateService> _logger = Substitute.For<ILogger<PromptTemplateService>>();
    private readonly PromptTemplateService _service;

    public PromptTemplateServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PromptTemplates:CacheDurationMinutes"] = "5",
            })
            .Build();

        _service = new PromptTemplateService(_repository, _cache, configuration, _logger);
    }

    private static PromptTemplate CreateTemplate(
        string id = "test-id",
        string userId = TestUserId,
        string name = "User Template",
        bool isBuiltIn = false) => new()
        {
            Id = id,
            UserId = userId,
            Name = name,
            Description = "Test description",
            Instructions = "You are a test assistant.",
            IsBuiltIn = isBuiltIn,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    // ---- GetTemplatesAsync ----

    [TestMethod]
    public async Task GetTemplatesAsync_MergesBuiltInAndUserTemplates()
    {
        var builtIn = CreateTemplate("b1", "_builtin", "Built-in", isBuiltIn: true);
        var user = CreateTemplate("u1", TestUserId, "User");

        _repository.GetBuiltInTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate> { builtIn });
        _repository.GetUserTemplatesAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate> { user });

        var result = await _service.GetTemplatesAsync(TestUserId);

        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Id == "b1");
        result.Should().Contain(t => t.Id == "u1");
    }

    [TestMethod]
    public async Task GetTemplatesAsync_SecondCallReturnsCachedResult()
    {
        _repository.GetBuiltInTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate>());
        _repository.GetUserTemplatesAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate> { CreateTemplate() });

        await _service.GetTemplatesAsync(TestUserId);
        await _service.GetTemplatesAsync(TestUserId);

        // Repository should only be called once because of caching.
        await _repository.Received(1).GetUserTemplatesAsync(TestUserId, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetTemplatesAsync_ForceRefresh_BypassesCache()
    {
        _repository.GetBuiltInTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate>());
        _repository.GetUserTemplatesAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate> { CreateTemplate() });

        await _service.GetTemplatesAsync(TestUserId);
        await _service.GetTemplatesAsync(TestUserId, forceRefresh: true);

        // Repository called twice due to force refresh.
        await _repository.Received(2).GetUserTemplatesAsync(TestUserId, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetTemplatesAsync_AnonymousUser_ReturnsOnlyBuiltIn()
    {
        var builtIn = CreateTemplate("b1", "_builtin", "Built-in", isBuiltIn: true);
        _repository.GetBuiltInTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate> { builtIn });

        var result = await _service.GetTemplatesAsync("_anonymous");

        result.Should().ContainSingle().Which.IsBuiltIn.Should().BeTrue();
        await _repository.DidNotReceive().GetUserTemplatesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetTemplatesAsync_NoUserTemplates_ReturnsOnlyBuiltIn()
    {
        var builtIn = CreateTemplate("b1", "_builtin", "Built-in", isBuiltIn: true);
        _repository.GetBuiltInTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate> { builtIn });
        _repository.GetUserTemplatesAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate>());

        var result = await _service.GetTemplatesAsync(TestUserId);

        result.Should().ContainSingle().Which.IsBuiltIn.Should().BeTrue();
    }

    // ---- GetByIdAsync ----

    [TestMethod]
    public async Task GetByIdAsync_UserTemplate_ReturnsTemplate()
    {
        var template = CreateTemplate();
        _repository.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(template);

        var result = await _service.GetByIdAsync(TestUserId, "test-id");

        result.Should().NotBeNull();
        result!.Id.Should().Be("test-id");
    }

    [TestMethod]
    public async Task GetByIdAsync_NotFoundInUser_FallsBackToBuiltIn()
    {
        var builtIn = CreateTemplate("b1", "_builtin", "Built-in", isBuiltIn: true);
        _repository.GetByIdAsync(TestUserId, "b1", Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);
        _repository.GetByIdAsync("_builtin", "b1", Arg.Any<CancellationToken>())
            .Returns(builtIn);

        var result = await _service.GetByIdAsync(TestUserId, "b1");

        result.Should().NotBeNull();
        result!.IsBuiltIn.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetByIdAsync_AnonymousUser_FallsBackToBuiltIn()
    {
        var builtIn = CreateTemplate("b1", "_builtin", "Built-in", isBuiltIn: true);
        _repository.GetByIdAsync("_builtin", "b1", Arg.Any<CancellationToken>())
            .Returns(builtIn);

        var result = await _service.GetByIdAsync("_anonymous", "b1");

        result.Should().NotBeNull();
        result!.IsBuiltIn.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetByIdAsync_NotFoundAnywhere_ReturnsNull()
    {
        _repository.GetByIdAsync(Arg.Any<string>(), "missing", Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var result = await _service.GetByIdAsync(TestUserId, "missing");

        result.Should().BeNull();
    }

    // ---- CreateAsync ----

    [TestMethod]
    public async Task CreateAsync_DelegatesAndInvalidatesCache()
    {
        var template = CreateTemplate();

        // Pre-populate cache.
        _repository.GetBuiltInTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate>());
        _repository.GetUserTemplatesAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate>());
        await _service.GetTemplatesAsync(TestUserId);

        _repository.CreateAsync(template, Arg.Any<CancellationToken>())
            .Returns(template);

        var result = await _service.CreateAsync(template);

        result.Should().NotBeNull();
        await _repository.Received(1).CreateAsync(template, Arg.Any<CancellationToken>());

        // Cache should be invalidated, so next get should hit repository again.
        await _service.GetTemplatesAsync(TestUserId);
        await _repository.Received(2).GetUserTemplatesAsync(TestUserId, Arg.Any<CancellationToken>());
    }

    // ---- UpdateAsync ----

    [TestMethod]
    public async Task UpdateAsync_DelegatesAndInvalidatesCache()
    {
        var template = CreateTemplate();

        // Pre-populate cache.
        _repository.GetBuiltInTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate>());
        _repository.GetUserTemplatesAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate> { template });
        await _service.GetTemplatesAsync(TestUserId);

        _repository.UpdateAsync(template, Arg.Any<CancellationToken>())
            .Returns(template);

        await _service.UpdateAsync(template);

        await _repository.Received(1).UpdateAsync(template, Arg.Any<CancellationToken>());

        // Cache invalidated — next get hits repository.
        await _service.GetTemplatesAsync(TestUserId);
        await _repository.Received(2).GetUserTemplatesAsync(TestUserId, Arg.Any<CancellationToken>());
    }

    // ---- DeleteAsync ----

    [TestMethod]
    public async Task DeleteAsync_DelegatesAndInvalidatesCache()
    {
        // Pre-populate cache.
        _repository.GetBuiltInTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate>());
        _repository.GetUserTemplatesAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate> { CreateTemplate() });
        await _service.GetTemplatesAsync(TestUserId);

        await _service.DeleteAsync(TestUserId, "test-id");

        await _repository.Received(1).DeleteAsync(TestUserId, "test-id", Arg.Any<CancellationToken>());

        // Cache invalidated.
        await _service.GetTemplatesAsync(TestUserId);
        await _repository.Received(2).GetUserTemplatesAsync(TestUserId, Arg.Any<CancellationToken>());
    }
}
