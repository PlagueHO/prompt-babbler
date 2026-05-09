using System.ComponentModel;
using ModelContextProtocol.Server;
using PromptBabbler.McpServer.Agents;

namespace PromptBabbler.McpServer.Tools;

[McpServerToolType]
public sealed class AgenticTools(IPromptBabblerAgentOrchestrator orchestrator)
{
    [McpServerTool(Name = "ask_prompt_babbler")]
    [Description("Ask Prompt Babbler to reason through multi-step work using available tools and return an answer with an execution trace.")]
    public Task<string> AskPromptBabbler(
        [Description("Natural-language request for Prompt Babbler")] string request,
        CancellationToken cancellationToken = default)
    {
        return orchestrator.RunAsync(request, cancellationToken);
    }
}
