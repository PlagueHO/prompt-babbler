var builder = DistributedApplication.CreateBuilder(args);

// Azure AI Foundry resource — Aspire provisions in your Azure subscription.
// Azure:SubscriptionId and Azure:TenantId are REQUIRED — set via dotnet user-secrets (see QUICKSTART.md).
// Azure:Location and Azure:CredentialSource are set in launchSettings.json.
// See: https://aspire.dev/integrations/cloud/azure/azure-ai-foundry/azure-ai-foundry-host/
var foundry = builder.AddAzureAIFoundry("ai-foundry");

// Model deployment configuration — read from MicrosoftFoundry config section with sensible defaults.
// These are NOT Aspire parameters — just configuration values for the deployment names.
var chatDeployment = foundry.AddDeployment(
    "chat",
    builder.Configuration["MicrosoftFoundry:chatModelName"] ?? "gpt-5.3-chat",
    builder.Configuration["MicrosoftFoundry:chatModelVersion"] ?? "2026-03-03",
    "OpenAI")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "GlobalStandard";
        deployment.SkuCapacity = 50;
    });

// Azure Cosmos DB — uses the emulator for local development.
// See: https://aspire.dev/integrations/cloud/azure/azure-cosmos-db/azure-cosmos-db-host/
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator();

var cosmosDb = cosmos.AddCosmosDatabase("prompt-babbler");
var templatesContainer = cosmosDb.AddContainer("prompt-templates", "/userId");

// Speech-to-text uses Azure AI Speech Service (part of the same AIServices resource)
// instead of an OpenAI model deployment. No Aspire deployment needed — the Speech SDK
// connects directly to the AIServices endpoint via SpeechConfig.
var apiService = builder.AddProject<Projects.PromptBabbler_Api>("api")
    .WithReference(foundry)
    .WithReference(chatDeployment)
    .WithReference(cosmos)
    .WithReference(templatesContainer)
    .WaitFor(chatDeployment)
    .WaitFor(cosmos)
    .WithEnvironment("Azure__TenantId", builder.Configuration["Azure:TenantId"])
    .WithEnvironment("AZURE_TENANT_ID", builder.Configuration["Azure:TenantId"])
    .WithEnvironment("Speech__Region", builder.Configuration["Azure:Location"] ?? "");

builder.AddViteApp("frontend", "../../../../prompt-babbler-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
