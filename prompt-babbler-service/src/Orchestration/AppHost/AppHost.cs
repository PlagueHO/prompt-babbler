var builder = DistributedApplication.CreateBuilder(args);

// Azure AI Foundry resources — host for account-level endpoints and project for model routing.
// Azure:SubscriptionId and Azure:TenantId are REQUIRED — set via dotnet user-secrets (see QUICKSTART-LOCAL.md).
// Azure:Location and Azure:CredentialSource are set in launchSettings.json.
// See: https://aspire.dev/integrations/cloud/azure/azure-ai-foundry/azure-ai-foundry-host/
var foundry = builder.AddFoundry("foundry");
var foundryProject = foundry.AddProject("ai-foundry");

// Model deployment configuration — read from MicrosoftFoundry config section with sensible defaults.
// These are NOT Aspire parameters — just configuration values for the deployment names.
var chatDeployment = foundryProject.AddModelDeployment(
    "chat",
    builder.Configuration["MicrosoftFoundry:chatModelName"] ?? "gpt-5.3-chat",
    builder.Configuration["MicrosoftFoundry:chatModelVersion"] ?? "2026-03-03",
    "OpenAI")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "GlobalStandard";
        deployment.SkuCapacity = 50;
    });

var embeddingDeployment = foundryProject.AddModelDeployment(
    "embedding",
    builder.Configuration["MicrosoftFoundry:embeddingModelName"] ?? "text-embedding-3-small",
    builder.Configuration["MicrosoftFoundry:embeddingModelVersion"] ?? "1",
    "OpenAI")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "GlobalStandard";
        deployment.SkuCapacity = 120;
    });

// Azure Cosmos DB — uses the emulator for local development.
// See: https://aspire.dev/integrations/cloud/azure/azure-cosmos-db/azure-cosmos-db-host/
#pragma warning disable ASPIRECOSMOSDB001
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator(emulator =>
    {
        emulator.WithDataExplorer();
        // Keep the emulator container alive between Aspire runs so the image is not
        // re-pulled and the pgcosmos extension does not need to cold-start every time.
        emulator.WithLifetime(ContainerLifetime.Persistent);
    });
#pragma warning restore ASPIRECOSMOSDB001

var cosmosDb = cosmos.AddCosmosDatabase("prompt-babbler");
var promptTemplatesContainer = cosmosDb.AddContainer("prompt-templates", "/userId");
// Note: AddContainer() does not support vector embedding policies or vector indexes.
// The 'babbles' container requires a quantizedFlat vector index on /contentVector for
// VectorDistance() queries. CosmosVectorContainerInitializationService recreates the
// container with the correct configuration at startup in Development.
// Upstream Aspire issue: https://github.com/microsoft/aspire/issues/14384
// Tracking issue:        https://github.com/PlagueHO/prompt-babbler/issues/122
var babblesContainer = cosmosDb.AddContainer("babbles", "/userId");
var generatedPromptsContainer = cosmosDb.AddContainer("generated-prompts", "/babbleId");
var usersContainer = cosmosDb.AddContainer("users", "/userId");

// Speech-to-text uses Azure AI Speech Service (part of the same AIServices resource)
// instead of an OpenAI model deployment. No Aspire deployment needed — the Speech SDK
// connects directly to the AIServices endpoint via SpeechConfig.
var apiClientId = builder.Configuration["EntraAuth:ApiClientId"] ?? "";
var spaClientId = builder.Configuration["EntraAuth:SpaClientId"] ?? "";
var tenantId = builder.Configuration["Azure:TenantId"] ?? "";

var apiService = builder.AddProject<Projects.PromptBabbler_Api>("api")
    .WithReference(foundry)
    .WithReference(foundryProject)
    .WithReference(chatDeployment)
    .WithReference(embeddingDeployment)
    .WithReference(cosmos)
    .WithReference(promptTemplatesContainer)
    .WithReference(babblesContainer)
    .WithReference(generatedPromptsContainer)
    .WithReference(usersContainer)
    .WaitFor(chatDeployment)
    .WaitFor(embeddingDeployment)
    .WaitFor(cosmos)
    .WithEnvironment("Azure__TenantId", tenantId)
    .WithEnvironment("AZURE_TENANT_ID", tenantId)
    .WithEnvironment("Speech__Region", builder.Configuration["Azure:Location"] ?? "")
    .WithEnvironment("AzureAd__ClientId", apiClientId)
    .WithEnvironment("AzureAd__TenantId", tenantId)
    .WithEnvironment("AzureAd__Instance", "https://login.microsoftonline.com/");

var mcpClientId = builder.Configuration["EntraAuth:McpClientId"] ?? string.Empty;

var mcpServer = builder.AddProject<Projects.PromptBabbler_McpServer>("mcp-server")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("AzureAd__ClientId", mcpClientId)
    .WithEnvironment("AzureAd__TenantId", tenantId)
    .WithEnvironment("AzureAd__Instance", "https://login.microsoftonline.com/")
    .WithEnvironment("AccessControl__AccessCode", builder.Configuration["AccessControl:AccessCode"] ?? string.Empty);

builder.AddViteApp("frontend", "../../../../prompt-babbler-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("MSAL_CLIENT_ID", spaClientId)
    .WithEnvironment("MSAL_TENANT_ID", tenantId);

builder.Build().Run();
