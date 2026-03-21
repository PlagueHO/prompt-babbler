using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Api.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory WITHOUT test authentication.
/// Requests are unauthenticated, so protected endpoints return 401.
/// Used to test that auth enforcement works correctly.
/// </summary>
public class NoAuthWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide fake AzureAd config so JwtBearer middleware doesn't throw during initialization
        builder.UseSetting("AzureAd:ClientId", "00000000-0000-0000-0000-000000000000");
        builder.UseSetting("AzureAd:TenantId", "00000000-0000-0000-0000-000000000000");
        builder.UseSetting("AzureAd:Instance", "https://login.microsoftonline.com/");

        builder.ConfigureServices(services =>
        {
            // Replace domain services with NSubstitute mocks (same as CustomWebApplicationFactory)
            // but do NOT replace authentication — use the real JWT Bearer middleware.
            ReplaceService<IPromptTemplateRepository>(services);
            ReplaceService<IPromptTemplateService>(services);
            ReplaceService<IPromptGenerationService>(services);
            ReplaceService<IRealtimeTranscriptionService>(services);
            ReplaceService<IBabbleService>(services);
            ReplaceService<IGeneratedPromptService>(services);
            ReplaceService<ITemplateValidationService>(services);
            ReplaceService<IChatClient>(services);
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
}
