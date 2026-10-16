using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PromptBabbler.McpServer.IntegrationTests;

[TestClass]
[TestCategory("Integration")]
public sealed class McpServerInitializeTests
{
    [TestMethod]
    public async Task Initialize_ReturnsDescriptiveServerName()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        request.Content = JsonContent.Create(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-03-26",
                capabilities = new { },
                clientInfo = new { name = "PromptBabblerTests", version = "1.0.0" },
            },
        });

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var responseJsonBody = response.Content.Headers.ContentType?.MediaType == "text/event-stream"
            ? responseBody
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Single(line => line.StartsWith("data:", StringComparison.Ordinal))["data:".Length..].Trim()
            : responseBody;
        using var responseJson = JsonDocument.Parse(responseJsonBody);

        responseJson.RootElement
            .GetProperty("result")
            .GetProperty("serverInfo")
            .GetProperty("name")
            .GetString()
            .Should()
            .Be("Prompt Babbler MCP Server");
    }
}
