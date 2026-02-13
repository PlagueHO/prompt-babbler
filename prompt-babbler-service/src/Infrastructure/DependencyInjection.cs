using Microsoft.Extensions.DependencyInjection;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, FileSettingsService>();
        services.AddTransient<IPromptGenerationService, AzureOpenAiPromptGenerationService>();
        services.AddTransient<ITranscriptionService, AzureOpenAiTranscriptionService>();
        return services;
    }
}
