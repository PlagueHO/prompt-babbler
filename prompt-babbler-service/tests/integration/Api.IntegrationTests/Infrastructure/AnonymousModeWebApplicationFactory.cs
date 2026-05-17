using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Api.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory for anonymous single-user mode (AzureAd not configured).
/// Used to verify Program.cs startup wiring for auth-disabled scenarios.
/// </summary>
public class AnonymousModeWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:cosmos",
            "AccountEndpoint=https://localhost:8081/;AccountKey=AQIDBAUGBwgJCgsMDQ4PEA==;");

        builder.ConfigureServices(services =>
        {
            RemoveHostedService<CosmosVectorContainerInitializationService>(services);

            ReplaceService<IPromptTemplateRepository>(services);
            ReplaceService<IPromptTemplateService>(services);
            ReplaceService<IPromptGenerationService>(services);
            ReplaceService<IRealtimeTranscriptionService>(services);
            ReplaceService<IFileTranscriptionService>(services);
            ReplaceService<IBabbleService>(services);
            ReplaceService<IGeneratedPromptService>(services);
            ReplaceService<ITemplateValidationService>(services);
            ReplaceService<IUserService>(services);
            ReplaceService<IChatClient>(services);

            services.AddSingleton(Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>());
        });
    }

    private static void ReplaceService<T>(IServiceCollection services) where T : class
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }

        services.AddSingleton(Substitute.For<T>());
    }

    private static void RemoveHostedService<THostedService>(IServiceCollection services)
        where THostedService : class, IHostedService
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(THostedService))
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
}
