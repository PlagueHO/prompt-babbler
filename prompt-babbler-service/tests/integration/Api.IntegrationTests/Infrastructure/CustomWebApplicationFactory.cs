using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Api.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory configured with test authentication and mocked domain services.
/// All requests are automatically authenticated as the test user.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace default authentication with test auth that always succeeds
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            // Ensure the test scheme is used for all auth operations
            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });

            // Replace domain services with NSubstitute mocks
            ReplaceService<IPromptTemplateRepository>(services);
            ReplaceService<IPromptTemplateService>(services);
            ReplaceService<IPromptGenerationService>(services);
            ReplaceService<IRealtimeTranscriptionService>(services);
            ReplaceService<IChatClient>(services);
        });
    }

    private static void ReplaceService<T>(IServiceCollection services) where T : class
    {
        // Remove existing registrations
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }

        // Add mock
        services.AddSingleton(Substitute.For<T>());
    }
}
