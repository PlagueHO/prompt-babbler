using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Azure;
using PromptBabbler.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Log tenant configuration at startup for diagnostics.
var startupLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
var configuredTenantId = builder.Configuration["Azure:TenantId"];
var envTenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
startupLogger.LogInformation("Azure:TenantId from config: {TenantId}", string.IsNullOrEmpty(configuredTenantId) ? "(not set)" : configuredTenantId);
startupLogger.LogInformation("AZURE_TENANT_ID from env: {TenantId}", string.IsNullOrEmpty(envTenantId) ? "(not set)" : envTenantId);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Register AzureOpenAIClient from Aspire connection (auto-configured from AppHost).
// When Azure:TenantId is set (local dev), scope the credential to that tenant to avoid
// DefaultAzureCredential picking up a credential from a different tenant.
var tenantId = builder.Configuration["Azure:TenantId"];
builder.AddAzureOpenAIClient("ai-foundry", configureClientBuilder: clientBuilder =>
{
    if (!string.IsNullOrEmpty(tenantId))
    {
        clientBuilder.WithCredential(new DefaultAzureCredential(
            new DefaultAzureCredentialOptions { TenantId = tenantId }));
    }
});

// Register IChatClient for the chat deployment via Microsoft.Extensions.AI.
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var openAiClient = sp.GetRequiredService<AzureOpenAIClient>();
    return openAiClient.GetChatClient("chat").AsIChatClient();
});

// Register TokenCredential for Azure Speech Service and any other Azure SDK clients.
// Uses the same DefaultAzureCredential pattern as the OpenAI client.
builder.Services.AddSingleton<TokenCredential>(sp =>
{
    return !string.IsNullOrEmpty(tenantId)
        ? new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = tenantId })
        : new DefaultAzureCredential();
});

// Parse the AI Services endpoint from the AI Foundry connection string for Speech Service
// STS token exchange. The Speech SDK requires a Cognitive Services token, not a raw AAD token.
var aiFoundryConnStr = builder.Configuration.GetConnectionString("ai-foundry") ?? "";
var aiServicesEndpoint = "";
foreach (var part in aiFoundryConnStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
{
    var trimmed = part.Trim();
    if (trimmed.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
    {
        aiServicesEndpoint = trimmed["Endpoint=".Length..].TrimEnd('/');
        break;
    }
}

// Fall back to treating the whole string as a URL if no Endpoint= prefix found.
if (string.IsNullOrEmpty(aiServicesEndpoint) &&
    aiFoundryConnStr.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
{
    aiServicesEndpoint = aiFoundryConnStr.Split(';')[0].TrimEnd('/');
}

builder.Services.AddInfrastructure(
    speechRegion: builder.Configuration["Speech:Region"] ?? string.Empty,
    aiServicesEndpoint: aiServicesEndpoint);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
               Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Host is "localhost" or "127.0.0.1")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseWebSockets();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

public partial class Program { }
