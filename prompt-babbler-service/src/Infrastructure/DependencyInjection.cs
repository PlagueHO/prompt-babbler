using Azure.Core;
using Microsoft.Azure.Cosmos;
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

        // Prompt template repository and service backed by Cosmos DB with in-memory caching.
        services.AddMemoryCache();
        services.AddSingleton<IPromptTemplateRepository>(sp =>
        {
            var cosmosClient = sp.GetRequiredService<CosmosClient>();
            var logger = sp.GetRequiredService<ILogger<CosmosPromptTemplateRepository>>();
            return new CosmosPromptTemplateRepository(cosmosClient, logger);
        });
        services.AddSingleton<IPromptTemplateService, PromptTemplateService>();
        services.AddHostedService<BuiltInTemplateSeedingService>();

        // Babble repository and service backed by Cosmos DB.
        services.AddSingleton<IBabbleRepository>(sp =>
        {
            var cosmosClient = sp.GetRequiredService<CosmosClient>();
            var logger = sp.GetRequiredService<ILogger<CosmosBabbleRepository>>();
            return new CosmosBabbleRepository(cosmosClient, logger);
        });
        services.AddSingleton<IBabbleService, BabbleService>();

        // Generated prompt repository and service backed by Cosmos DB.
        services.AddSingleton<IGeneratedPromptRepository>(sp =>
        {
            var cosmosClient = sp.GetRequiredService<CosmosClient>();
            var logger = sp.GetRequiredService<ILogger<CosmosGeneratedPromptRepository>>();
            return new CosmosGeneratedPromptRepository(cosmosClient, logger);
        });
        services.AddSingleton<IGeneratedPromptService, GeneratedPromptService>();

        // User profile repository and service backed by Cosmos DB.
        services.AddSingleton<IUserRepository>(sp =>
        {
            var cosmosClient = sp.GetRequiredService<CosmosClient>();
            var logger = sp.GetRequiredService<ILogger<CosmosUserRepository>>();
            return new CosmosUserRepository(cosmosClient, logger);
        });
        services.AddSingleton<IUserService, UserService>();

        return services;
    }
}
