using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed partial class TemplateValidationService : ITemplateValidationService
{
    private const string SyntheticBabbleText =
        "So yeah I was thinking about, um, maybe we should build a feature that lets users " +
        "upload their profile pictures, and then we can resize them automatically. Also need " +
        "to handle different file formats like PNG and JPEG. Oh and, uh, make sure there's " +
        "some kind of validation so people can't upload huge files.";

    private static readonly string[] PromptInjectionPatterns =
    [
        "ignore previous instructions",
        "ignore all previous",
        "disregard previous",
        "disregard all previous",
        "forget your instructions",
        "you are now",
        "act as if you have no restrictions",
        "override your system prompt",
        "pretend you are",
        "bypass your safety",
        "ignore your safety",
        "jailbreak",
        "do anything now",
    ];

    private readonly IPromptBuilder _promptBuilder;
    private readonly IChatClient _chatClient;
    private readonly ILogger<TemplateValidationService> _logger;

    public TemplateValidationService(
        IPromptBuilder promptBuilder,
        IChatClient chatClient,
        ILogger<TemplateValidationService> logger)
    {
        _promptBuilder = promptBuilder;
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<TemplateValidationResult> ValidateTemplateAsync(
        PromptTemplate template,
        CancellationToken cancellationToken = default)
    {
        // Stage 1: Local content safety heuristics
        var localErrors = RunLocalValidation(template);
        if (localErrors.Count > 0)
        {
            _logger.LogWarning(
                "Template '{TemplateName}' failed local content safety validation with {ErrorCount} error(s)",
                template.Name, localErrors.Count);
            return TemplateValidationResult.Failure(localErrors);
        }

        // Stage 2: Foundry Model test generation
        var foundryErrors = await RunFoundryValidationAsync(template, cancellationToken);
        if (foundryErrors.Count > 0)
        {
            _logger.LogWarning(
                "Template '{TemplateName}' failed Foundry validation with {ErrorCount} error(s)",
                template.Name, foundryErrors.Count);
            return TemplateValidationResult.Failure(foundryErrors);
        }

        return TemplateValidationResult.Success();
    }

    public static List<string> RunLocalValidation(PromptTemplate template)
    {
        var errors = new List<string>();

        // Check all text fields for prompt injection patterns
        CheckPromptInjection(template.Instructions, "Instructions", errors);

        if (template.OutputDescription is not null)
        {
            CheckPromptInjection(template.OutputDescription, "OutputDescription", errors);
        }

        if (template.OutputTemplate is not null)
        {
            CheckPromptInjection(template.OutputTemplate, "OutputTemplate", errors);
        }

        if (template.Guardrails is { Count: > 0 })
        {
            for (var i = 0; i < template.Guardrails.Count; i++)
            {
                CheckPromptInjection(template.Guardrails[i], $"Guardrails[{i}]", errors);
            }
        }

        if (template.Examples is { Count: > 0 })
        {
            for (var i = 0; i < template.Examples.Count; i++)
            {
                CheckPromptInjection(template.Examples[i].Input, $"Examples[{i}].Input", errors);
                CheckPromptInjection(template.Examples[i].Output, $"Examples[{i}].Output", errors);
            }
        }

        // Check for PII patterns in instructions (the primary directive field)
        CheckPiiPatterns(template.Instructions, "Instructions", errors);

        return errors;
    }

    private async Task<List<string>> RunFoundryValidationAsync(
        PromptTemplate template,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        try
        {
            var effectiveFormat = template.DefaultOutputFormat ?? "text";
            var effectiveEmojis = template.DefaultAllowEmojis ?? false;
            var systemPrompt = _promptBuilder.BuildSystemPrompt(template, effectiveFormat, effectiveEmojis);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, SyntheticBabbleText),
            };

            await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Foundry validation failed for template '{TemplateName}'", template.Name);
            errors.Add($"Content safety validation failed: {ex.Message}");
        }

        return errors;
    }

    private static void CheckPromptInjection(string text, string fieldName, List<string> errors)
    {
        var lowerText = text.ToLowerInvariant();
        foreach (var pattern in PromptInjectionPatterns)
        {
            if (lowerText.Contains(pattern, StringComparison.Ordinal))
            {
                errors.Add($"Potential prompt injection detected in {fieldName}: '{pattern}'");
            }
        }
    }

    private static void CheckPiiPatterns(string text, string fieldName, List<string> errors)
    {
        if (EmailRegex().IsMatch(text))
        {
            errors.Add($"Potential email address detected in {fieldName}. Remove PII from template content.");
        }

        if (PhoneRegex().IsMatch(text))
        {
            errors.Add($"Potential phone number detected in {fieldName}. Remove PII from template content.");
        }

        if (SsnRegex().IsMatch(text))
        {
            errors.Add($"Potential SSN detected in {fieldName}. Remove PII from template content.");
        }
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnRegex();
}
