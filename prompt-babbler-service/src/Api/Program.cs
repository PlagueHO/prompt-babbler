using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Azure;
using Microsoft.Identity.Web;
using PromptBabbler.Api.Authentication;
using PromptBabbler.Api.HealthChecks;
using PromptBabbler.Api.Middleware;
using PromptBabbler.Domain.Configuration;
using PromptBabbler.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Map ACCESS_CODE environment variable to AccessControl:AccessCode configuration key.
var accessCodeEnvVar = Environment.GetEnvironmentVariable("ACCESS_CODE");
if (!string.IsNullOrEmpty(accessCodeEnvVar))
{
    builder.Configuration["AccessControl:AccessCode"] = accessCodeEnvVar;
}

builder.AddServiceDefaults();

// Register access control options.
builder.Services.Configure<AccessControlOptions>(builder.Configuration.GetSection(AccessControlOptions.SectionName));

// Cosmos DB client — use ManagedIdentityCredential directly in deployed environments.
// DefaultAzureCredential permanently caches credential unavailability per the Azure Identity
// SDK behavior. In ACA cold starts (scale from 0), the identity sidecar may not be ready on
// the first credential probe, causing DAC to mark ManagedIdentityCredential as permanently
// unavailable and fall through to VS/VSCode credentials which don't exist in a Linux container.
// See: https://learn.microsoft.com/azure/container-apps/managed-identity?tabs=portal,dotnet
builder.AddAzureCosmosClient("cosmos",
    configureSettings: settings =>
    {
        // In non-development environments (ACA), use ManagedIdentityCredential directly
        // with system-assigned identity. DefaultAzureCredential is used in development
        // where it picks up local credentials (Azure CLI, VS, etc.).
        if (!builder.Environment.IsDevelopment())
        {
            settings.Credential = new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
        }
    },
    configureClientOptions: options =>
    {
        options.UseSystemTextJsonSerializerWithOptions = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
    });

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

var tenantId = builder.Configuration["Azure:TenantId"];
var aiFoundryConnStr = builder.Configuration.GetConnectionString("ai-foundry") ?? "";
var isAiConfigured = !string.IsNullOrWhiteSpace(aiFoundryConnStr);
TokenCredential runtimeTokenCredential = builder.Environment.IsDevelopment()
    ? (!string.IsNullOrEmpty(tenantId)
        ? new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = tenantId,
            ExcludeManagedIdentityCredential = true,
        })
        : new DefaultAzureCredential())
    : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);

if (isAiConfigured)
{
    var aiTokenCredential = new AiFoundryTokenCredential(runtimeTokenCredential);

    // Use the "chat" deployment connection string (not the "ai-foundry" project connection string).
    // The deployment connection string inherits the foundry account's Endpoint and EndpointAIInference,
    // and adds Deployment=chat. The Foundry project endpoint (/api/projects/{name}) does not accept
    // AI Inference SDK requests and returns "API version not supported".
    builder.AddAzureChatCompletionsClient("chat", configureSettings: settings =>
    {
        settings.TokenCredential = aiTokenCredential;
    })
    .AddChatClient();
}
else
{
    startupLogger.LogWarning("ConnectionStrings:ai-foundry is not configured. AI features (prompt generation, title generation) will be unavailable.");
}

// Register TokenCredential for Azure Speech Service and any other Azure SDK clients.
// Uses the same runtime credential policy as the rest of the Azure SDK clients.
builder.Services.AddSingleton<TokenCredential>(runtimeTokenCredential);

// Parse the AI Services endpoint from the AI Foundry connection string for Speech Service
// STS token exchange. The Speech SDK requires a Cognitive Services token, not a raw AAD token.
var foundryAccountConnStr = builder.Configuration.GetConnectionString("foundry")
    ?? Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT")
    ?? "";
var aiServicesConnStr = !string.IsNullOrWhiteSpace(foundryAccountConnStr)
    ? foundryAccountConnStr
    : aiFoundryConnStr;
var aiServicesEndpoint = "";
foreach (var part in aiServicesConnStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
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
    aiServicesConnStr.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
{
    aiServicesEndpoint = aiServicesConnStr.Split(';')[0].TrimEnd('/');
}

builder.Services.AddInfrastructure(
    speechRegion: builder.Configuration["Speech:Region"] ?? string.Empty,
    aiServicesEndpoint: aiServicesEndpoint);

// Health checks for dependency monitoring
builder.Services.AddHealthChecks()
    .AddCheck<CosmosDbHealthCheck>("cosmosdb", tags: ["ready"])
    .AddCheck<AiFoundryHealthCheck>("ai-foundry", tags: ["ready"]);

// Managed identity health check only runs in deployed environments
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHealthChecks()
        .AddCheck<ManagedIdentityHealthCheck>("managed-identity", tags: ["ready"]);
}

var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
var isAuthEnabled = !string.IsNullOrEmpty(azureAdClientId);

// Store auth mode in configuration so controllers/extensions can read it.
builder.Configuration["AuthMode:Enabled"] = isAuthEnabled.ToString();

if (isAuthEnabled)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"), jwtBearerScheme: JwtBearerDefaults.AuthenticationScheme, subscribeToJwtBearerMiddlewareDiagnosticsEvents: false);

    builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Events ??= new JwtBearerEvents();
        var originalOnMessageReceived = options.Events.OnMessageReceived;
        options.Events.OnMessageReceived = async context =>
        {
            // Extract access_token from query string for WebSocket connections (same pattern as SignalR).
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/transcribe"))
            {
                context.Token = accessToken;
            }

            if (originalOnMessageReceived is not null)
            {
                await originalOnMessageReceived(context);
            }
        };
    });

    builder.Services.AddAuthorization();
}
else
{
    // No Entra ID ClientId configured — run in anonymous single-user mode.
    // All endpoints are accessible without authentication. Controllers use "_anonymous" as userId.
    startupLogger.LogWarning("AzureAd:ClientId is not configured. Running in anonymous single-user mode.");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();

    builder.Services.AddAuthorization(options =>
    {
        // Override the default policy to allow anonymous access when auth is disabled.
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)
            .Build();

        // Override the fallback policy as well to allow anonymous access.
        options.FallbackPolicy = null;
    });
}

var corsAllowedOrigins = builder.Configuration["CORS:AllowedOrigins"] ?? "";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        static bool IsLocalOrigin(string origin) =>
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Host is "localhost" or "127.0.0.1";

        if (string.IsNullOrEmpty(corsAllowedOrigins))
        {
            // Local dev: allow localhost only.
            policy.SetIsOriginAllowed(IsLocalOrigin)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // Production: allow configured origins + localhost.
            var allowedOrigins = corsAllowedOrigins
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            policy.SetIsOriginAllowed(origin =>
                    IsLocalOrigin(origin) || allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseMiddleware<AccessCodeMiddleware>();
app.UseWebSockets();
app.UseAuthentication();

// In anonymous single-user mode, inject a synthetic ClaimsPrincipal so that
// [Authorize], [RequiredScope("access_as_user")], and User.GetObjectId() all
// work without a real JWT token. The identity contains the minimum claims
// needed: an object ID ("_anonymous") and the expected scope.
if (!isAuthEnabled)
{
    app.Use(async (context, next) =>
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "_anonymous"),
            new System.Security.Claims.Claim("scp", "access_as_user"),
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Anonymous");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        await next();
    });
}

app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

// Log access control status at startup.
var accessCode = builder.Configuration["AccessControl:AccessCode"];
startupLogger.LogInformation("Access control: {Status}", string.IsNullOrEmpty(accessCode) ? "disabled" : "enabled");

app.Run();

public partial class Program { }
