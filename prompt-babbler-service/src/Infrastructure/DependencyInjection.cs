using Azure.AI.Speech.Transcription;
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
        services.AddSingleton<IPromptBuilder, PromptBuilder>();
        services.AddTransient<IPromptGenerationService, AzureOpenAiPromptGenerationService>();
        services.AddTransient<ITemplateValidationService, TemplateValidationService>();

        // Speech Service authenticates via a Cognitive Services token obtained by exchanging
        // the AAD token at the AI Services resource's STS endpoint.
        services.AddSingleton<IRealtimeTranscriptionService>(sp =>
        {
            var credential = sp.GetRequiredService<TokenCredential>();
            var logger = sp.GetRequiredService<ILogger<AzureSpeechTranscriptionService>>();
            return new AzureSpeechTranscriptionService(speechRegion, aiServicesEndpoint, credential, logger);
        });

        // File transcription (Azure Fast Transcription API)
        services.AddSingleton<ITranscriptionClientWrapper>(sp =>
        {
            var endpoint = new Uri(aiServicesEndpoint);
            var credential = sp.GetRequiredService<TokenCredential>();
            var client = new TranscriptionClient(endpoint, credential);
            return new TranscriptionClientWrapper(client);
        });

        services.AddSingleton<IFileTranscriptionService, AzureFastTranscriptionService>();

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
        services.AddSingleton<IEmbeddingService, EmbeddingService>();

        // Workaround: Aspire's AddContainer() does not support vector index policies, so the
        // local emulator creates 'babbles' without the quantizedFlat vector index required for
        // VectorDistance() queries. This service runs only in Development and recreates the
        // container with the correct vector configuration.
        // See: https://github.com/microsoft/aspire/issues/14384
        // See: https://github.com/PlagueHO/prompt-babbler/issues/122
        services.AddHostedService<CosmosVectorContainerInitializationService>();

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

        // Async import/export job infrastructure.
        services.AddSingleton<IImportExportJobRepository>(sp =>
        {
            var cosmosClient = sp.GetRequiredService<CosmosClient>();
            var logger = sp.GetRequiredService<ILogger<CosmosImportExportJobRepository>>();
            return new CosmosImportExportJobRepository(cosmosClient, logger);
        });
        services.AddSingleton<IImportExportJobQueue, ChannelImportExportJobQueue>();
        services.AddSingleton<IExportJobProcessor, ExportJobProcessor>();
        services.AddSingleton<IImportJobProcessor, ImportJobProcessor>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IImportService, ImportService>();
        services.AddHostedService<ImportExportJobWorker>();

        return services;
    }
}
