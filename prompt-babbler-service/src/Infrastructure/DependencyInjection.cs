using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string speechRegion,
        string aiServicesEndpoint)
    {
        services.AddTransient<IPromptGenerationService, AzureOpenAiPromptGenerationService>();

        // Speech Service authenticates via a Cognitive Services token obtained by exchanging
        // the AAD token at the AI Services resource's STS endpoint.
        services.AddSingleton<IRealtimeTranscriptionService>(sp =>
        {
            var credential = sp.GetRequiredService<TokenCredential>();
            var logger = sp.GetRequiredService<ILogger<AzureSpeechTranscriptionService>>();
            return new AzureSpeechTranscriptionService(speechRegion, aiServicesEndpoint, credential, logger);
        });

        return services;
    }
}
