using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IPromptBuilder
{
    string BuildSystemPrompt(PromptTemplate template, string outputFormat, bool allowEmojis);
}
