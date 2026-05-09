using System.ComponentModel;
using FluentAssertions;
using ModelContextProtocol.Server;
using NSubstitute;
using PromptBabbler.McpServer.Agents;
using PromptBabbler.McpServer.Tools;

namespace PromptBabbler.McpServer.UnitTests.Tools;

[TestClass]
[TestCategory("Unit")]
public sealed class AgenticToolsTests
{
    private readonly IPromptBabblerAgentOrchestrator _orchestrator =
        Substitute.For<IPromptBabblerAgentOrchestrator>();

    private readonly AgenticTools _sut;

    public AgenticToolsTests()
    {
        _sut = new AgenticTools(_orchestrator);
    }

    [TestMethod]
    public async Task AskPromptBabbler_ShouldForwardRequestToOrchestrator()
    {
        const string request = "List my babbles";
        const string expected = """{"answer":"done","trace":[]}""";
        _orchestrator.RunAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.AskPromptBabbler(request);

        result.Should().Be(expected);
    }

    [TestMethod]
    public async Task AskPromptBabbler_ShouldPassCancellationTokenToOrchestrator()
    {
        using var cts = new CancellationTokenSource();
        const string request = "Get template";
        _orchestrator.RunAsync(request, cts.Token).Returns("{}");

        await _sut.AskPromptBabbler(request, cts.Token);

        await _orchestrator.Received(1).RunAsync(request, cts.Token);
    }

    [TestMethod]
    public async Task AskPromptBabbler_WhenOrchestratorThrowsOperationCancelledException_ShouldPropagate()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _orchestrator
            .RunAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new OperationCanceledException());

        Func<Task> act = () => _sut.AskPromptBabbler("request", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task AskPromptBabbler_ShouldReturnOrchestratorResultUnmodified()
    {
        const string orchestratorResult =
            """{"answer":"result","trace":[{"kind":"reason","name":"text","content":"thinking"}]}""";
        _orchestrator
            .RunAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(orchestratorResult);

        var result = await _sut.AskPromptBabbler("anything");

        result.Should().Be(orchestratorResult);
    }

    [TestMethod]
    public void AgenticTools_ShouldBeDiscoverableAsMcpToolType()
    {
        typeof(AgenticTools)
            .GetCustomAttributes(typeof(McpServerToolTypeAttribute), inherit: false)
            .Should().ContainSingle();
    }

    [TestMethod]
    public void AskPromptBabbler_ShouldDeclareExpectedMcpToolMetadata()
    {
        var method = typeof(AgenticTools).GetMethod(nameof(AgenticTools.AskPromptBabbler));
        method.Should().NotBeNull();

        method!
            .GetCustomAttributes(typeof(McpServerToolAttribute), inherit: false)
            .Should().ContainSingle();

        var toolAttribute = (McpServerToolAttribute)method
            .GetCustomAttributes(typeof(McpServerToolAttribute), inherit: false)
            .Single();
        toolAttribute.Name.Should().Be("ask_prompt_babbler");

        var descriptionAttribute = (System.ComponentModel.DescriptionAttribute)method
            .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), inherit: false)
            .Single();
        descriptionAttribute.Description.Should().Contain("execution trace");
    }
}
