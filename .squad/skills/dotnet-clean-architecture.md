# Skill: .NET Clean Architecture Patterns

## Confidence: medium

## Overview

The prompt-babbler backend follows Clean Architecture with strict layering: Domain → Infrastructure → Api. This skill covers the layer boundaries, repository/service patterns, DI registration, controller conventions, and Cosmos DB query patterns used throughout the codebase.

## Layer Boundaries

### Domain (`src/Domain/`)

- Contains **models** (C# records) and **interfaces** only
- Zero external package dependencies — no Azure SDKs, no NuGet packages
- Models use `required init` properties with `[JsonPropertyName]` attributes
- Interfaces define the contract: `IXxxRepository` for data access, `IXxxService` for business logic

### Infrastructure (`src/Infrastructure/`)

- Implements domain interfaces using Azure SDKs
- Contains: Cosmos DB repositories, Azure OpenAI service, Azure Speech service, caching services
- Depends on Domain (references interfaces and models)
- Registers all services via `AddInfrastructure()` extension method in `DependencyInjection.cs`

### Api (`src/Api/`)

- ASP.NET Core controllers, middleware, `Program.cs` DI wiring
- Depends on Domain and Infrastructure
- Controllers inject domain interfaces, never infrastructure types directly
- Validation is inline in controllers (string length, enum checks)

### Orchestration (`src/Orchestration/`)

- `AppHost`: .NET Aspire orchestration — provisions Azure resources, Cosmos emulator, wires env vars
- `ServiceDefaults`: OpenTelemetry, health checks, resilience, service discovery

## Domain Model Pattern

All models are C# records with `required init` properties:

```csharp
public record Babble
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }
}
```

Key conventions:

- Always `record` (not class) for immutability
- Always `[JsonPropertyName("camelCase")]` for explicit serialization
- ID generation: `Guid.NewGuid().ToString()` in factory methods or controllers
- Timestamps: `DateTimeOffset.UtcNow` for `CreatedAt`/`UpdatedAt`

## Repository Pattern (Cosmos DB)

All repositories follow the same pattern:

```csharp
public class CosmosBabbleRepository : IBabbleRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosBabbleRepository> _logger;

    public CosmosBabbleRepository(CosmosClient cosmosClient, ILogger<CosmosBabbleRepository> logger)
    {
        _container = cosmosClient.GetContainer("prompt-babbler", "babbles");
        _logger = logger;
    }
}
```

Query patterns:

- **Paginated list**: `QueryDefinition` with `ORDER BY c.createdAt DESC`, `MaxItemCount`, continuation tokens
- **Get by ID**: `container.ReadItemAsync<T>(id, new PartitionKey(partitionKeyValue))`
- **Create**: `container.CreateItemAsync<T>(item, new PartitionKey(partitionKeyValue))`
- **Update**: `container.ReplaceItemAsync<T>(item, id, new PartitionKey(partitionKeyValue))`
- **Delete**: `container.DeleteItemAsync<T>(id, new PartitionKey(partitionKeyValue))`
- **Not found**: Catch `CosmosException` where `StatusCode == HttpStatusCode.NotFound`, return `null`

## Service Layer Pattern

Services wrap repositories with business logic:

- Cascade delete: `BabbleService.DeleteAsync()` deletes generated prompts first, then the babble
- Ownership validation: `GeneratedPromptService` validates babble ownership before operations
- Lazy creation: `UserService.GetOrCreateAsync()` creates default profile if not found
- Caching: `PromptTemplateService` uses `IMemoryCache` with 5-minute sliding expiration

## DI Registration Pattern

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    string speechRegion,
    string aiServicesEndpoint)
{
    // Transient: fresh context per request
    services.AddTransient<IPromptGenerationService, AzureOpenAiPromptGenerationService>();

    // Singleton: thread-safe, maintains state
    services.AddSingleton<IBabbleRepository>(sp =>
        new CosmosBabbleRepository(sp.GetRequiredService<CosmosClient>(), ...));
    services.AddSingleton<IBabbleService, BabbleService>();

    // HostedService: async startup initialization
    services.AddHostedService<BuiltInTemplateSeedingService>();

    return services;
}
```

Lifetime rules:

- **Singleton**: Cosmos repositories (thread-safe), Speech service (token cache), template service (in-memory cache)
- **Transient**: Prompt generation (fresh LLM context per request)
- **HostedService**: One-time async initialization (template seeding)

## Controller Pattern

```csharp
[ApiController]
[Route("api/babbles")]
[Authorize]
[RequiredScope("access_as_user")]
public class BabbleController : ControllerBase
{
    // Inject domain interfaces
    private readonly IBabbleService _babbleService;

    // User ID extraction
    var userId = User.GetUserIdOrAnonymous();

    // Validation: inline checks, return BadRequest
    if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
        return BadRequest("Title is required (1-200 characters)");

    // Response: map domain model to response DTO
    return CreatedAtAction(nameof(GetById), new { id = babble.Id }, MapToResponse(babble));
}
```

## Solution Structure

- Solution file: `PromptBabbler.slnx` (lightweight format)
- Build config: `Directory.Build.props` (shared: `net10.0`, nullable, TreatWarningsAsErrors)
- Package versions: `Directory.Packages.props` (central package management)
- SDK version: `global.json` (.NET 10.0.100)
