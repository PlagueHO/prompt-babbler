namespace PromptBabbler.McpServer.Agents;

public interface IPromptBabblerAgentOrchestrator
{
    Task<string> RunAsync(string request, CancellationToken cancellationToken);
}
