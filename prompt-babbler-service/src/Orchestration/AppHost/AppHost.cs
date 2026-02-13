var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.PromptBabbler_Api>("api");

builder.AddViteApp("frontend", "../../../../prompt-babbler-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
