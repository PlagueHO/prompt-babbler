using Azure.AI.Projects;

namespace PromptBabbler.McpServer.Configuration;

public interface IAgenticFoundryClientFactory
{
    string? ResolveProjectEndpoint();

    AIProjectClient CreateClient();
}