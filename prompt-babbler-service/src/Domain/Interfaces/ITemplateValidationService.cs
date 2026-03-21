using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface ITemplateValidationService
{
    Task<TemplateValidationResult> ValidateTemplateAsync(
        PromptTemplate template,
        CancellationToken cancellationToken = default);
}
