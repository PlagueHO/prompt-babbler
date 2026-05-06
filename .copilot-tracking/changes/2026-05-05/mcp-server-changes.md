<!-- markdownlint-disable-file -->
# Release Changes: MCP Server for Prompt Babbler

**Related Plan**: mcp-server-plan.instructions.md
**Implementation Date**: 2026-05-05

## Summary

Create a new `PromptBabbler.McpServer` ASP.NET Core project exposing Prompt Babbler data and operations to AI assistants via the MCP protocol using Streamable HTTP transport.

## Changes

### Added

* `prompt-babbler-service/src/McpServer/PromptBabbler.McpServer.csproj` - New McpServer project file
* `prompt-babbler-service/src/McpServer/Client/Models/BabbleDto.cs` - Babble response DTO
* `prompt-babbler-service/src/McpServer/Client/Models/BabbleSearchResponseDto.cs` - Search response DTO
* `prompt-babbler-service/src/McpServer/Client/Models/PromptTemplateDto.cs` - Template DTOs (response + create/update requests)
* `prompt-babbler-service/src/McpServer/Client/Models/GeneratedPromptDto.cs` - Generated prompt response DTO
* `prompt-babbler-service/src/McpServer/Client/Models/PagedResponseDto.cs` - Generic paged response DTO
* `prompt-babbler-service/src/McpServer/Client/IPromptBabblerApiClient.cs` - Typed client interface
* `prompt-babbler-service/src/McpServer/Client/ApiAuthOptions.cs` - Auth options record
* `prompt-babbler-service/src/McpServer/Client/ApiAuthDelegatingHandler.cs` - Auth header injection handler
* `prompt-babbler-service/src/McpServer/Client/PromptBabblerApiClient.cs` - HTTP client implementation
* `prompt-babbler-service/src/McpServer/Tools/BabbleTools.cs` - Babble and generate_prompt MCP tools
* `prompt-babbler-service/src/McpServer/Tools/PromptTemplateTools.cs` - Template CRUD MCP tools
* `prompt-babbler-service/src/McpServer/Tools/GeneratedPromptTools.cs` - Generated prompt MCP tools
* `prompt-babbler-service/src/McpServer/Resources/TemplateResources.cs` - Template MCP resources
* `prompt-babbler-service/src/McpServer/Prompts/TemplateReviewPrompt.cs` - review_template MCP prompt

### Modified

* `prompt-babbler-service/Directory.Packages.props` - Added `ModelContextProtocol.AspNetCore` v1.2.0 and `Microsoft.AspNetCore.Authentication.JwtBearer` v10.0.7
* `prompt-babbler-service/PromptBabbler.slnx` - Added McpServer project in /src/ folder (alphabetical order)

* `prompt-babbler-service/src/McpServer/McpAccessCodeMiddleware.cs` - Access code validation middleware
* `prompt-babbler-service/src/McpServer/Program.cs` - Application entry point with MCP server setup and conditional auth
* `prompt-babbler-service/src/McpServer/appsettings.json` - Default configuration
* `prompt-babbler-service/src/McpServer/Properties/launchSettings.json` - HTTP launch profile on port 5242

### Modified (Phase 5 & 6)

* `prompt-babbler-service/src/Orchestration/AppHost/PromptBabbler.AppHost.csproj` - Added McpServer ProjectReference
* `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs` - Added mcp-server Aspire resource registration

### Removed

<!-- No files removed -->

## Additional or Deviating Changes

* `Microsoft.AspNetCore.Authentication.JwtBearer` was absent from `Directory.Packages.props` (plan described it as already present)
  * Added at version 10.0.7 to match the `Microsoft.Extensions.*` pattern in the file
* `ApiAuthDelegatingHandler.cs`: `ITokenAcquisition.GetAccessTokenForUserAsync` has no `cancellationToken` parameter in this SDK version
  * Removed the argument; cancellation propagation deferred to future work if needed
* `PromptBabblerApiClient.cs`: `CA2024` analyzer error for `StreamReader.EndOfStream` in async context
  * Replaced `while (!EndOfStream)` loop with `while ((line = await ReadLineAsync()) is not null)` pattern
* `Resources/TemplateResources.cs`: `McpServerResourceAttribute` has no `Uri` property in v1.2.0 — only `UriTemplate`
  * Both the fixed-URI list resource and the template-URI resource use `UriTemplate`

## Release Summary

**Total files affected**: 22 files (19 added, 4 modified, 0 removed)

**Added files**:
- McpServer project: `PromptBabbler.McpServer.csproj`
- Client layer (7 files): `BabbleDto.cs`, `BabbleSearchResponseDto.cs`, `PromptTemplateDto.cs`, `GeneratedPromptDto.cs`, `PagedResponseDto.cs`, `IPromptBabblerApiClient.cs`, `ApiAuthOptions.cs`, `ApiAuthDelegatingHandler.cs`, `PromptBabblerApiClient.cs`
- MCP Tools (3 files): `BabbleTools.cs`, `PromptTemplateTools.cs`, `GeneratedPromptTools.cs`
- MCP Resources/Prompts (2 files): `TemplateResources.cs`, `TemplateReviewPrompt.cs`
- Bootstrap (4 files): `McpAccessCodeMiddleware.cs`, `Program.cs`, `appsettings.json`, `Properties/launchSettings.json`

**Modified files**:
- `Directory.Packages.props` — Added `ModelContextProtocol.AspNetCore` v1.2.0 and `Microsoft.AspNetCore.Authentication.JwtBearer` v10.0.7
- `PromptBabbler.slnx` — Added McpServer project
- `PromptBabbler.AppHost.csproj` — Added McpServer ProjectReference
- `AppHost.cs` — Added mcp-server Aspire resource registration

**Validation results**:
- `dotnet build PromptBabbler.slnx` — Build succeeded, 0 errors
- `dotnet format PromptBabbler.slnx --verify-no-changes` — No changes required
- `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit` — 235 passed, 0 failed (exit code 8 = integration assemblies had 0 unit tests, expected behavior)
