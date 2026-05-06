# MCP C# SDK Research — Remote Server with Streamable HTTP

## Research Topics

1. MCP C# SDK setup and project structure
1. Streamable HTTP transport configuration for ASP.NET Core
1. MCP capabilities/primitives (Tools, Resources, Prompts)
1. Authentication patterns for remote MCP servers
1. Required NuGet packages
1. Example code for defining tools, resources, and prompts

## Status: Complete

---

## Findings

### 1. SDK Setup and Project Structure

The official C# SDK (maintained by Microsoft in collaboration with the MCP org) is at v1.2.0. The SDK repository is `modelcontextprotocol/csharp-sdk` on GitHub.

**Three NuGet packages:**

| Package | Purpose |
|---------|---------|
| `ModelContextProtocol.Core` | Low-level client/server APIs with minimum dependencies |
| `ModelContextProtocol` | Hosting, DI, attribute-based tool/prompt/resource discovery (references Core) |
| `ModelContextProtocol.AspNetCore` | HTTP-based MCP servers with ASP.NET Core (references ModelContextProtocol) |

For a remote HTTP server, use `ModelContextProtocol.AspNetCore`.

**Project scaffolding:**

```bash
dotnet new web -n <ProjectName>
cd <ProjectName>
dotnet add package ModelContextProtocol.AspNetCore
```

Alternatively, the `dotnet new mcpserver --transport remote` template is available via `Microsoft.McpServer.ProjectTemplates`.

**Minimum .NET version:** .NET 10.0+ (latest SDK requires this).

---

### 2. Streamable HTTP Transport

Streamable HTTP is the modern transport (replaces legacy HTTP+SSE from protocol version 2024-11-05). It uses a single HTTP endpoint (e.g., `https://example.com/mcp`) that supports both POST and GET methods.

**ASP.NET Core setup:**

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    .WithHttpTransport()         // Optional: pass HttpServerTransportOptions
    .WithToolsFromAssembly();

var app = builder.Build();
app.MapMcp();                    // Maps to /mcp by default
app.Run();
```

**Key configuration options (`HttpServerTransportOptions`):**

| Option | Description |
|--------|-------------|
| `Stateless = true` | Disables session state; each request is independent. Recommended for servers that don't need server-to-client requests (sampling/elicitation). Enables horizontal scaling without session affinity. |
| `IdleTimeout` | Configure session cleanup timeout (e.g., `TimeSpan.FromMinutes(30)`) |
| Custom path | `app.MapMcp("/custom-path")` |

**Protocol details:**

- Client sends JSON-RPC messages as HTTP POST to the MCP endpoint
- Client includes `Accept: application/json, text/event-stream`
- Server responds with either `application/json` (single response) or `text/event-stream` (SSE stream for multiple messages)
- Optional session management via `Mcp-Session-Id` header
- Supports resumability via SSE event IDs and `Last-Event-ID` header

**Security requirements (from spec):**

- Servers MUST validate the `Origin` header on incoming connections to prevent DNS rebinding attacks
- When running locally, bind only to localhost (127.0.0.1)
- Implement proper authentication for all connections
- Configure `AllowedHosts` in ASP.NET Core for hostname validation
- CORS only needed if browser-based cross-origin access is required

---

### 3. MCP Capabilities (Primitives)

The MCP protocol defines three core primitives:

#### Tools

Functions the LLM can invoke. Primary mechanism for exposing server functionality.

- Decorated with `[McpServerToolType]` (class) and `[McpServerTool]` (method)
- Support `[Description]` for LLM discoverability
- Attribute properties: `Name`, `Title`, `Destructive`, `Idempotent`, `OpenWorld`, `ReadOnly`
- Return types: `string`, `TextContentBlock`, `ImageContentBlock`, `AudioContentBlock`, `EmbeddedResourceBlock`, `CallToolResult`, `IEnumerable<AIContent>`
- DI-injected parameters (don't appear in schema): `CancellationToken`, `IMcpServer`, `IProgress<ProgressNotificationValue>`, any registered service

#### Prompts

Reusable LLM interaction templates for common patterns.

- Decorated with `[McpServerPromptType]` (class) and `[McpServerPrompt]` (method)
- Return `ChatMessage` or `IEnumerable<ChatMessage>`
- Parameters exposed as prompt arguments

#### Resources

Data the LLM can read (configuration, files, state).

- Decorated with `[McpServerResourceType]` (class) and `[McpServerResource]` (method)
- Attribute properties: `UriTemplate`, `Name`, `Title`, `MimeType`, `IconSource`
- Support subscribe/list-changed notifications

#### Additional capabilities

- **ServerInstructions** — text injected to help the LLM understand the server's purpose
- **Progress reporting** — via `IProgress<ProgressNotificationValue>`
- **Notifications** — tools can notify clients of list changes
- **OpenTelemetry** — built-in tracing (`ActivitySource: Experimental.ModelContextProtocol`) and metrics (`Meter: Experimental.ModelContextProtocol`)

---

### 4. Authentication for Remote MCP Servers

#### 4a. MCP Spec's Approach to Authorization

The MCP specification defines authorization at the transport level (OPTIONAL):

- Based on OAuth 2.1 (draft-ietf-oauth-v2-1-13)
- Uses RFC 9728 (OAuth 2.0 Protected Resource Metadata) for discovery
- MCP server acts as an OAuth 2.1 Resource Server
- MCP client acts as an OAuth 2.1 Client
- Tokens are Bearer tokens in the `Authorization` header on every HTTP request

**Discovery flow:**

1. Client makes unauthenticated MCP request
1. Server returns HTTP 401 with `WWW-Authenticate: Bearer resource_metadata="..."` header
1. Client fetches Protected Resource Metadata (`.well-known/oauth-protected-resource`)
1. Metadata includes `authorization_servers` array pointing to the OAuth/OIDC authorization server
1. Client discovers AS metadata (RFC 8414 or OIDC Discovery)
1. Standard OAuth 2.1 authorization code flow with PKCE
1. Client includes access token in subsequent MCP requests

**Client registration approaches (priority order):**

1. Pre-registered client credentials
1. Client ID Metadata Documents (HTTPS URL as client_id — RFC draft)
1. Dynamic Client Registration (RFC 7591)
1. User-entered client information

#### 4b. Anonymous/No Auth

Simply don't add authentication middleware:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();
var app = builder.Build();
app.MapMcp();  // No RequireAuthorization()
app.Run();
```

#### 4c. Bearer Token Auth (Access Code Scenario)

Use standard ASP.NET Core JWT Bearer authentication:

```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-auth-server";
        options.Audience = "mcp-server";
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp().RequireAuthorization();
```

Access the authenticated user in tools via `IHttpContextAccessor`:

```csharp
builder.Services.AddHttpContextAccessor();

[McpServerToolType]
public class AuthAwareTools(IHttpContextAccessor httpContextAccessor)
{
    [McpServerTool, Description("Returns the authenticated user's ID")]
    public string GetCurrentUser()
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.Identity?.Name ?? "anonymous";
    }
}
```

#### 4d. OAuth 2.0 / Entra ID Auth (Multi-Tenant)

The C# SDK includes MCP-specific authentication support in the `ModelContextProtocol.AspNetCore.Authentication` namespace:

**Key classes:**

- `McpAuthenticationDefaults` — default scheme names
- `McpAuthenticationHandler` — adds resource metadata to challenge responses
- `McpAuthenticationOptions` — configuration for resource metadata
- `McpAuthenticationEvents` — authentication lifecycle events
- `ResourceMetadataRequestContext` — context for resource metadata requests

**Full OAuth setup (from ProtectedMcpServer sample):**

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = "https://login.microsoftonline.com/{tenantId}/v2.0";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidAudience = "https://your-mcp-server.example.com",
        ValidIssuer = "https://login.microsoftonline.com/{tenantId}/v2.0",
    };
})
.AddMcp(options =>
{
    options.ResourceMetadata = new()
    {
        ResourceDocumentation = "https://docs.example.com/api",
        AuthorizationServers = { "https://login.microsoftonline.com/{tenantId}/v2.0" },
        ScopesSupported = ["mcp:tools"],
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp().RequireAuthorization();
```

The `.AddMcp()` extension on the authentication builder:

- Serves Protected Resource Metadata at `.well-known/oauth-protected-resource`
- Adds `resource_metadata` URL to `WWW-Authenticate` headers on 401 responses
- Enables MCP clients to discover the authorization server per RFC 9728

**For Entra ID multi-tenant:**

- Set `Authority` to `https://login.microsoftonline.com/common/v2.0` or `organizations/v2.0`
- Validate issuer against allowed tenants dynamically
- Set `ValidAudience` to the app registration's Application ID URI or client ID
- Register the MCP server as an app registration in Entra ID with `access_as_user` scope

---

### 5. NuGet Packages Required

For an ASP.NET Core MCP server with Streamable HTTP and auth:

| Package | Purpose |
|---------|---------|
| `ModelContextProtocol.AspNetCore` | HTTP transport, MapMcp, MCP auth handler (includes ModelContextProtocol and ModelContextProtocol.Core) |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT Bearer token validation |
| `Microsoft.Identity.Web` | (Optional) Entra ID integration with easier config |

That's it — `ModelContextProtocol.AspNetCore` pulls in all MCP dependencies transitively.

---

### 6. Example Code

#### Minimal HTTP Server (Program.cs)

```csharp
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new() { Name = "PromptBabbler.Mcp", Version = "1.0.0" };
    options.ServerInstructions = "Prompt Babbler MCP server for managing babbles and generating prompts.";
})
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly()
    .WithResourcesFromAssembly();

var app = builder.Build();
app.MapMcp();
app.Run();
```

#### Tool Example

```csharp
[McpServerToolType]
public sealed class BabbleTools(IBabbleService babbleService)
{
    [McpServerTool, Description("Get all babbles for the current user")]
    public async Task<string> GetBabbles(CancellationToken cancellationToken)
    {
        var babbles = await babbleService.GetBabblesAsync(cancellationToken);
        return JsonSerializer.Serialize(babbles);
    }

    [McpServerTool, Description("Generate a prompt from a specific babble")]
    public async Task<string> GeneratePrompt(
        [Description("The ID of the babble to generate a prompt from")] string babbleId,
        [Description("The template to use for generation")] string templateName,
        CancellationToken cancellationToken)
    {
        var result = await babbleService.GeneratePromptAsync(babbleId, templateName, cancellationToken);
        return result;
    }
}
```

#### Prompt Example

```csharp
[McpServerPromptType]
public static class BabblePrompts
{
    [McpServerPrompt, Description("Summarize a babble into a concise prompt")]
    public static ChatMessage SummarizeBabble(
        [Description("The babble transcription text")] string transcription) =>
        new(ChatRole.User, $"Summarize this babble into a concise, actionable prompt: {transcription}");
}
```

#### Resource Example

```csharp
[McpServerResourceType]
public static class BabbleResources
{
    [McpServerResource(UriTemplate = "babbler://templates", Name = "Available Templates",
        MimeType = "application/json"), Description("List of available prompt templates")]
    public static string GetTemplates() =>
        JsonSerializer.Serialize(TemplateRegistry.GetAll());
}
```

---

## References

- GitHub repo: <https://github.com/modelcontextprotocol/csharp-sdk> (v1.2.0, latest release March 28, 2025)
- API docs: <https://csharp.sdk.modelcontextprotocol.io/api/ModelContextProtocol.html>
- Getting started: <https://csharp.sdk.modelcontextprotocol.io/concepts/getting-started.html>
- MCP Transports spec (2025-03-26): <https://modelcontextprotocol.io/specification/2025-03-26/basic/transports#streamable-http>
- MCP Authorization spec (2025-11-25): <https://modelcontextprotocol.io/specification/2025-11-25/basic/authorization>
- Protected MCP Server sample: <https://github.com/modelcontextprotocol/csharp-sdk/blob/main/samples/ProtectedMcpServer/Program.cs>
- Microsoft quickstart: <https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server>
- Local skill reference: `mcp-csharp-create` skill (transport-config.md, api-patterns.md)

## Follow-on Questions

1. **Session migration** — The SDK has `ISessionMigrationHandler` for persisting/restoring session initialization across server instances. Relevant for non-stateless deployments behind a load balancer.
1. **CORS configuration** — Latest SDK docs emphasize AllowedHosts validation and restrictive CORS policies. The ProtectedMcpServer sample includes full CORS setup for browser clients.
1. **Aspire integration** — How would the MCP server project integrate into the existing Aspire AppHost orchestration?
1. **Multi-tenant Entra ID token validation** — Need to confirm the exact `ValidateIssuer` delegate pattern for multi-tenant scenarios where tenant ID isn't known ahead of time.

## Clarifying Questions

None — all research questions have been answered with sufficient evidence from the SDK source, API docs, spec, and skill references.
