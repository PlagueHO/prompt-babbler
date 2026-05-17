using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using PromptBabbler.ApiClient;
using PromptBabbler.McpServer;
using PromptBabbler.McpServer.Agents;
using PromptBabbler.McpServer.Configuration;
using PromptBabbler.McpServer.HealthChecks;
using McpServerClient = PromptBabbler.McpServer.Client;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var accessCode = builder.Configuration["AccessControl:AccessCode"] ?? string.Empty;
var azureAdClientId = builder.Configuration["AzureAd:ClientId"] ?? string.Empty;

builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new() { Name = "PromptBabbler.McpServer", Version = "1.0.0" };
    options.ServerInstructions = "Prompt Babbler MCP server. Provides tools for managing babbles (voice transcriptions), prompt templates, and AI-generated prompts. Use search_babbles to find relevant voice notes, generate_prompt to create AI prompts from babbles, and the template tools to manage prompt templates.";
})
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly()
    .WithResourcesFromAssembly();

if (!string.IsNullOrEmpty(azureAdClientId))
{
    var tenantId = builder.Configuration["AzureAd:TenantId"] ?? string.Empty;
    var apiScope = builder.Configuration["AzureAd:ApiScope"] ?? string.Empty;

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

    builder.Services.AddAuthentication()
        .AddMcp(options =>
        {
            options.ResourceMetadata = new ProtectedResourceMetadata
            {
                AuthorizationServers = { $"https://login.microsoftonline.com/{tenantId}/v2.0" },
                ScopesSupported = ["mcp:tools"]
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddSingleton(new McpServerClient.ApiAuthOptions(string.Empty, azureAdClientId, apiScope));
}
else
{
    builder.Services.AddAuthorization(options =>
    {
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)
            .Build();
    });

    builder.Services.AddSingleton(new McpServerClient.ApiAuthOptions(accessCode, string.Empty, string.Empty));
}

builder.Services.AddTransient<McpServerClient.ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IPromptBabblerApiClient, PromptBabblerApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://api");
}).AddHttpMessageHandler<McpServerClient.ApiAuthDelegatingHandler>();

builder.Services.AddSingleton<IAgenticFoundryClientFactory, AgenticFoundryClientFactory>();
builder.Services.AddScoped<IPromptBabblerAgentRunner, PromptBabblerFoundryAgentRunner>();
builder.Services.AddScoped<IPromptBabblerAgentOrchestrator, PromptBabblerAgentOrchestrator>();

builder.Services.AddHttpClient<PromptBabblerApiHealthCheck>(client =>
{
    client.BaseAddress = new Uri("https+http://api");
});
builder.Services.AddHealthChecks()
    .AddCheck<PromptBabblerApiHealthCheck>("prompt-babbler-api", tags: ["ready"]);

var app = builder.Build();

if (!string.IsNullOrEmpty(accessCode) && string.IsNullOrEmpty(azureAdClientId))
{
    app.UseMiddleware<McpAccessCodeMiddleware>();
}

app.MapDefaultEndpoints();
app.MapMcp();

await app.RunAsync();
