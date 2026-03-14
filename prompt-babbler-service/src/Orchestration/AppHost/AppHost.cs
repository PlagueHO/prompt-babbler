var builder = DistributedApplication.CreateBuilder(args);

// Azure AI Foundry resource — Aspire provisions in your Azure subscription.
// Azure:SubscriptionId and Azure:TenantId are REQUIRED — set via dotnet user-secrets (see QUICKSTART.md).
// Azure:Location and Azure:CredentialSource are set in launchSettings.json.
// See: https://aspire.dev/integrations/cloud/azure/azure-ai-foundry/azure-ai-foundry-host/
var foundry = builder.AddAzureAIFoundry("ai-foundry");

// Model deployment configuration — read from MicrosoftFoundry config section with sensible defaults.
// These are NOT Aspire parameters — just configuration values for the deployment names.
// Defaults: gpt-4.1 for chat/LLM, gpt-4o-transcribe for STT.
var chatDeployment = foundry.AddDeployment(
    "chat",
    builder.Configuration["MicrosoftFoundry:chatModelName"] ?? "gpt-4.1",
    builder.Configuration["MicrosoftFoundry:chatModelVersion"] ?? "2025-04-14",
    "OpenAI")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "Standard";
        deployment.SkuCapacity = 1;
    });

var sttDeployment = foundry.AddDeployment(
    "stt",
    builder.Configuration["MicrosoftFoundry:sttModelName"] ?? "gpt-4o-transcribe",
    builder.Configuration["MicrosoftFoundry:sttModelVersion"] ?? "2025-03-20",
    "OpenAI")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "GlobalStandard";
        deployment.SkuCapacity = 1;
    });

var apiService = builder.AddProject<Projects.PromptBabbler_Api>("api")
    .WithReference(foundry)
    .WithReference(chatDeployment)
    .WithReference(sttDeployment)
    .WaitFor(chatDeployment)
    .WaitFor(sttDeployment);

builder.AddViteApp("frontend", "../../../../prompt-babbler-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
