# MCP Authentication & Aspire Integration Research

## Research Questions

1. How does the MCP C# SDK handle authentication? (`ModelContextProtocol.AspNetCore`)
1. What is the MCP specification's authentication model?
1. How do MCP clients pass credentials to remote MCP servers?
1. Should the MCP server forward user tokens to the API or authenticate as a service principal?
1. How to integrate the MCP server into the existing Aspire AppHost?
1. What references does the MCP server need in Aspire?

---

## Topic 1: MCP Authentication Deep Dive

### MCP Specification Authentication Model (2025-06-18)

Source: <https://modelcontextprotocol.io/specification/2025-06-18/basic/authorization>

Key points:

- **Authorization is OPTIONAL** for MCP implementations
- Based on **OAuth 2.1** (draft-ietf-oauth-v2-1-13), RFC 8414 (AS Metadata), RFC 7591 (Dynamic Client Registration), RFC 9728 (Protected Resource Metadata)
- The MCP server acts as an **OAuth 2.1 Resource Server**
- The MCP client acts as an **OAuth 2.1 Client**
- MCP clients pass tokens via `Authorization: Bearer <access-token>` header on **every HTTP request**
- Authorization must be included in every HTTP request, even within the same logical session
- MCP servers MUST expose `/.well-known/oauth-protected-resource` metadata endpoint
- MCP servers MUST return `WWW-Authenticate` header on 401 responses with resource_metadata URL

### Token Passthrough is Explicitly Forbidden

Source: <https://modelcontextprotocol.io/specification/2025-06-18/basic/security_best_practices#token-passthrough>

**Critical finding:**

> "Token passthrough is an anti-pattern where an MCP server accepts tokens from an MCP client without validating that the tokens were properly issued to the MCP server and passes them through to the downstream API."
>
> "MCP servers MUST NOT accept any tokens that were not explicitly issued for the MCP server."
>
> "If the MCP server makes requests to upstream APIs, it may act as an OAuth client to them. The access token used at the upstream API is a separate token, issued by the upstream authorization server. The MCP server MUST NOT pass through the token it received from the MCP client."

**Implications for Prompt Babbler:**

- The MCP server MUST validate tokens issued specifically for itself
- It MUST NOT forward the MCP client's token to the API
- When calling the API, the MCP server acts as its own OAuth client (or uses other auth mechanisms)

### MCP C# SDK Authentication Support (`ModelContextProtocol.AspNetCore`)

Source: `samples/ProtectedMcpServer/Program.cs` in <https://github.com/modelcontextprotocol/csharp-sdk>

The C# SDK provides:

1. **`ModelContextProtocol.AspNetCore.Authentication`** namespace with:
   - `McpAuthenticationDefaults.AuthenticationScheme` — MCP-specific challenge scheme
   - `.AddMcp(options => ...)` extension on `AuthenticationBuilder` — configures Protected Resource Metadata
   - `options.ResourceMetadata` — exposes `/.well-known/oauth-protected-resource`

1. **Pattern from ProtectedMcpServer sample:**

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = "https://login.microsoftonline.com/{tenantId}";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = "api://mcp-server-client-id",
    };
})
.AddMcp(options =>
{
    options.ResourceMetadata = new()
    {
        AuthorizationServers = { "https://login.microsoftonline.com/{tenantId}/v2.0" },
        ScopesSupported = ["mcp:tools"],
    };
});

// Then protect the MCP endpoint:
app.MapMcp().RequireAuthorization();
```

1. **Stateless mode** is recommended for servers that don't need server-to-client requests:

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport(options => { options.Stateless = true; })
    .WithToolsFromAssembly();
```

### How MCP Clients Pass Credentials

#### VS Code Copilot (Agent Mode)

Source: <https://code.visualstudio.com/docs/copilot/reference/mcp-configuration>

VS Code supports remote HTTP MCP servers with static headers:

```json
{
  "servers": {
    "promptBabbler": {
      "type": "http",
      "url": "https://mcp.example.com/mcp",
      "headers": {
        "Authorization": "Bearer ${input:api-token}"
      }
    }
  }
}
```

- `headers` field supports `${input:variable}` syntax for secrets
- VS Code prompts the user once, stores securely
- No built-in OAuth flow in VS Code MCP client (as of May 2026) — relies on static bearer tokens or pre-obtained tokens

#### Claude Desktop and Other Clients

- Claude Desktop supports the full MCP OAuth 2.1 flow for remote servers:
  1. Client discovers `/.well-known/oauth-protected-resource`
  1. Client gets authorization server metadata
  1. Client performs PKCE OAuth flow via browser popup
  1. Client stores token and includes in all requests
- Other clients may support static bearer token headers only

### Authentication Pattern Recommendation for Prompt Babbler MCP Server

Given the three auth modes of the API:

| Mode | MCP Server Behavior | API Call Auth |
|------|---------------------|--------------|
| **Anonymous** | No auth required on MCP endpoint | No auth on API calls |
| **Access Code** | Accept access code as Bearer token or header | Forward access code to API via `X-Access-Code` header |
| **Entra ID** | Full MCP OAuth flow with Entra ID as authorization server | MCP server authenticates to API as itself (service-to-service) OR uses On-Behalf-Of flow |

**Recommended approach: Hybrid based on mode**

1. **Anonymous mode**: MCP server runs without auth, calls API without auth. Simplest.
1. **Access Code mode**: MCP server accepts access code (via `Authorization: Bearer <code>` or custom header), validates it, then passes it to the API as `X-Access-Code`. This is NOT token passthrough in the MCP spec sense because:
   - The access code is not an OAuth token
   - It's a simple shared secret validated at both layers
   - The MCP spec forbids passing *OAuth tokens* through; access codes are a different mechanism
1. **Entra ID mode**: Two sub-options:
   - **Option A (Recommended): On-Behalf-Of (OBO) flow** — MCP server receives user's token (audience=MCP server), then uses OBO to get a new token for the API (audience=API). This preserves user identity AND complies with the MCP spec (separate tokens for each service).
   - **Option B: Service principal** — MCP server authenticates to API using its own managed identity. Simpler but loses user identity context for per-user data isolation.

**Final recommendation: Option A (OBO) for Entra ID mode** because:

- The API uses `User.GetObjectId()` for per-user data isolation
- User identity MUST flow to the API for correct data scoping
- OBO is the standard Microsoft pattern for this exact scenario
- The MCP spec explicitly allows the MCP server to "act as an OAuth client" to upstream APIs with a *separate* token

---

## Topic 2: Aspire AppHost Integration

### Current AppHost Structure

File: `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs`

Current resources:

- `foundry` / `foundryProject` — Azure AI Foundry
- `chatDeployment`, `embeddingDeployment` — AI model deployments
- `cosmos` — Azure Cosmos DB with containers
- `apiService` — The API project (`PromptBabbler.Api`) with all references
- `frontend` — Vite app with reference to `apiService`

### MCP Server Wiring Pattern

The MCP server should follow the same pattern as the `frontend` — it references the API only and does NOT need direct access to Cosmos, Foundry, or model deployments.

```csharp
// Add after the apiService definition, before the frontend
var mcpServer = builder.AddProject<Projects.PromptBabbler_McpServer>("mcp-server")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("AzureAd__ClientId", mcpClientId)  // MCP server's own app registration
    .WithEnvironment("AzureAd__TenantId", tenantId)
    .WithEnvironment("AzureAd__Instance", "https://login.microsoftonline.com/");
```

### What the MCP Server Needs from Aspire

| Concern | How Aspire Provides It |
|---------|----------------------|
| API URL | `.WithReference(apiService)` injects `services__api__https__0` or `services__api__http__0` connection string. The MCP server uses Aspire service discovery to resolve `https+http://api` to the actual URL. |
| Auth configuration | `.WithEnvironment(...)` for Entra ID settings |
| Access code | `.WithEnvironment("ACCESS_CODE", ...)` if access code mode is used |

### Service Discovery in the MCP Server

The existing `ServiceDefaults/Extensions.cs` already configures service discovery for all HTTP clients:

```csharp
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler();
    http.AddServiceDiscovery();
});
```

So in the MCP server project:

```csharp
// Program.cs of the MCP server
builder.AddServiceDefaults();

// Register HttpClient for calling the API
builder.Services.AddHttpClient("PromptBabblerApi", client =>
{
    client.BaseAddress = new Uri("https+http://api");
});
```

Aspire service discovery resolves `https+http://api` to the actual API endpoint URL at runtime.

### AppHost.csproj Changes

Need to add a project reference:

```xml
<ProjectReference Include="..\..\McpServer\PromptBabbler.McpServer.csproj" />
```

### What the MCP Server Does NOT Need

- No Cosmos DB reference (talks to API only)
- No Foundry/AI model references (talks to API only)
- No speech service references (talks to API only)
- No direct container references

---

## Key Discoveries Summary

1. **MCP spec explicitly forbids token passthrough** — the MCP server must use separate tokens for downstream API calls
1. **On-Behalf-Of (OBO) flow** is the correct pattern for Entra ID mode to preserve user identity
1. **Access code mode** works by accepting the code and forwarding it as `X-Access-Code` (not an OAuth token, so not subject to the passthrough prohibition)
1. **C# SDK provides** `ModelContextProtocol.AspNetCore.Authentication` with `.AddMcp()` for Protected Resource Metadata and `McpAuthenticationDefaults`
1. **VS Code MCP clients** use static `headers` in `mcp.json` config (e.g., `"Authorization": "Bearer ${input:token}"`)
1. **Claude Desktop** supports full OAuth 2.1 flow via browser popup
1. **Aspire wiring** follows the frontend pattern: `.WithReference(apiService)` + service discovery resolves URLs automatically
1. **Stateless mode** recommended for the MCP server (no server-to-client requests needed)

---

## References

- MCP Authorization Specification: <https://modelcontextprotocol.io/specification/2025-06-18/basic/authorization>
- MCP Security Best Practices: <https://modelcontextprotocol.io/specification/2025-06-18/basic/security_best_practices>
- MCP C# SDK: <https://github.com/modelcontextprotocol/csharp-sdk>
- ProtectedMcpServer sample: <https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples/ProtectedMcpServer>
- C# SDK Getting Started: <https://csharp.sdk.modelcontextprotocol.io/concepts/getting-started.html>
- VS Code MCP Configuration: <https://code.visualstudio.com/docs/copilot/reference/mcp-configuration>
- Existing API auth: `prompt-babbler-service/src/Api/Program.cs` (lines 170-210)
- Existing AccessCode middleware: `prompt-babbler-service/src/Api/Middleware/AccessCodeMiddleware.cs`
- Existing AppHost: `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs`
- ServiceDefaults extensions: `prompt-babbler-service/src/Orchestration/ServiceDefaults/Extensions.cs`

---

## Clarifying Questions

1. **MCP server app registration**: For Entra ID mode with OBO, the MCP server needs its own Entra ID app registration (separate from the API's). Should this be created now, or deferred to when Entra ID mode is implemented?
1. **Access code forwarding**: The API currently checks `X-Access-Code` header. Should the MCP server support both the access code header and `Authorization: Bearer <code>` (i.e., accept the access code as a bearer token from MCP clients that can only set Authorization headers)?
1. **External exposure**: Will the MCP server be exposed publicly (for Claude Desktop OAuth flow) or only locally/within the Aspire orchestration?
