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
        builder.ConfigureServices(services =>
        {
            // Replace domain services with NSubstitute mocks (same as CustomWebApplicationFactory)
            // but do NOT replace authentication — use the real JWT Bearer middleware.
            ReplaceService<IPromptTemplateService>(services);
            ReplaceService<IPromptGenerationService>(services);
            ReplaceService<IRealtimeTranscriptionService>(services);
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
