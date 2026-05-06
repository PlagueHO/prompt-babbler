using PromptBabbler.McpServer.Client.Models;

namespace PromptBabbler.McpServer.Client;

public interface IPromptBabblerApiClient
{
    // Babbles
    Task<BabbleSearchResponseDto> SearchBabblesAsync(string query, int topK, CancellationToken cancellationToken);
    Task<PagedResponseDto<BabbleDto>> ListBabblesAsync(string? continuationToken, int pageSize, CancellationToken cancellationToken);
    Task<BabbleDto?> GetBabbleAsync(string id, CancellationToken cancellationToken);

    // Prompt Templates
    Task<IReadOnlyList<PromptTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken);
    Task<PromptTemplateDto?> GetTemplateAsync(string id, CancellationToken cancellationToken);
    Task<PromptTemplateDto> CreateTemplateAsync(CreatePromptTemplateRequest request, CancellationToken cancellationToken);
    Task<PromptTemplateDto> UpdateTemplateAsync(string id, UpdatePromptTemplateRequest request, CancellationToken cancellationToken);
    Task DeleteTemplateAsync(string id, CancellationToken cancellationToken);

    // Generated Prompts
    Task<PagedResponseDto<GeneratedPromptDto>> ListGeneratedPromptsAsync(string babbleId, string? continuationToken, int pageSize, CancellationToken cancellationToken);
    Task<GeneratedPromptDto?> GetGeneratedPromptAsync(string babbleId, string id, CancellationToken cancellationToken);

    // Generation
    Task<string> GeneratePromptAsync(string babbleId, string templateId, string? promptFormat, bool? allowEmojis, CancellationToken cancellationToken);
}
