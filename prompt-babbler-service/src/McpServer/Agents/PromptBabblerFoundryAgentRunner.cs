using Azure.AI.Projects;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;
using PromptBabbler.McpServer.Configuration;

namespace PromptBabbler.McpServer.Agents;

public sealed class PromptBabblerFoundryAgentRunner(IAgenticFoundryClientFactory foundryClientFactory) : IPromptBabblerAgentRunner
{
    public async Task<PromptBabblerAgentRunResult> RunAsync(
        string request,
        string modelDeploymentName,
        string agentInstructions,
        IReadOnlyList<AITool> tools,
        CancellationToken cancellationToken)
    {
        var aiProjectClient = foundryClientFactory.CreateClient();

        var agent = aiProjectClient.AsAIAgent(
            modelDeploymentName,
            agentInstructions,
            "PromptBabblerAgent",
            string.Empty,
            tools.ToList());

        var session = await agent.CreateSessionAsync(cancellationToken);
        var response = await agent.RunAsync(request, session, cancellationToken: cancellationToken);
        var contents = response.Messages
            .SelectMany(message => message.Contents)
            .ToArray();

        return new PromptBabblerAgentRunResult(response.Text ?? string.Empty, contents);
    }
}
