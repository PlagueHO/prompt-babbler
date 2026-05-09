using Microsoft.Extensions.AI;

namespace PromptBabbler.McpServer.Agents;

public interface IPromptBabblerAgentRunner
{
    Task<PromptBabblerAgentRunResult> RunAsync(
        string request,
        string modelDeploymentName,
        string agentInstructions,
        IReadOnlyList<AITool> tools,
        CancellationToken cancellationToken);
}

public sealed record PromptBabblerAgentRunResult(string Answer, IReadOnlyList<AIContent> Contents);
