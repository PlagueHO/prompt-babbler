using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using PromptBabbler.ApiClient;
using PromptBabbler.McpServer.Agents;

namespace PromptBabbler.McpServer.UnitTests.Agents;

[TestClass]
[TestCategory("Unit")]
public sealed class PromptBabblerAgentOrchestratorTests
{
    private readonly IPromptBabblerAgentRunner _agentRunner = Substitute.For<IPromptBabblerAgentRunner>();
    private readonly IPromptBabblerApiClient _apiClient = Substitute.For<IPromptBabblerApiClient>();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().Build();

    private readonly PromptBabblerAgentOrchestrator _sut;

    public PromptBabblerAgentOrchestratorTests()
    {
        _sut = new PromptBabblerAgentOrchestrator(_agentRunner, _apiClient, _configuration);
    }

    [TestMethod]
    public async Task RunAsync_ShouldSerializeAnswerAndTrace()
    {
        _agentRunner.RunAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<AITool>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PromptBabblerAgentRunResult(
                "Here is a Strahd-themed image prompt.",
                [
                    new TextContent("Reviewing matching babbles."),
                    new TextContent("   "),
                    new FunctionCallContent(
                        "call-1",
                        "search_babbles_api",
                        new Dictionary<string, object?>
                        {
                            ["query"] = "Strahd castle",
                        }),
                    new FunctionResultContent("call-1", "[{\"id\":\"babble-1\"}]"),
                ]));

        var result = await _sut.RunAsync("Create a prompt", CancellationToken.None);
        using var document = JsonDocument.Parse(result);
        var root = document.RootElement;

        root.GetProperty("answer").GetString().Should().Be("Here is a Strahd-themed image prompt.");

        var trace = root.GetProperty("trace");
        trace.GetArrayLength().Should().Be(3);

        trace[0].GetProperty("kind").GetString().Should().Be("reason");
        trace[0].GetProperty("name").GetString().Should().Be("text");
        trace[0].GetProperty("content").GetString().Should().Be("Reviewing matching babbles.");

        trace[1].GetProperty("kind").GetString().Should().Be("act");
        trace[1].GetProperty("name").GetString().Should().Be("search_babbles_api");
        trace[1].GetProperty("content").GetString().Should().Contain("Strahd castle");

        trace[2].GetProperty("kind").GetString().Should().Be("observe");
        trace[2].GetProperty("name").GetString().Should().Be("call-1");
        trace[2].GetProperty("content").GetString().Should().Be("[{\"id\":\"babble-1\"}]");
    }

    [TestMethod]
    public async Task RunAsync_ShouldPassCancellationTokenToRunner()
    {
        using var cts = new CancellationTokenSource();
        _agentRunner.RunAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<AITool>>(),
                cts.Token)
            .Returns(new PromptBabblerAgentRunResult("done", []));

        await _sut.RunAsync("Create a prompt", cts.Token);

        await _agentRunner.Received(1).RunAsync(
            "Create a prompt",
            "chat",
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<AITool>>(),
            cts.Token);
    }

    [TestMethod]
    public async Task RunAsync_ShouldRegisterExpectedAgentTools()
    {
        IReadOnlyList<AITool>? capturedTools = null;
        _agentRunner.RunAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<AITool>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedTools = callInfo.ArgAt<IReadOnlyList<AITool>>(3);
                return new PromptBabblerAgentRunResult("done", []);
            });

        await _sut.RunAsync("Create a prompt", CancellationToken.None);

        capturedTools.Should().NotBeNull();
        capturedTools!
            .Select(tool => tool.Name)
            .Should().Equal(
                "search_babbles_api",
                "get_babble_api",
                "list_prompt_templates_api",
                "get_prompt_template_api",
                "generate_prompt_api",
                "list_generated_prompts_api");
    }
}
