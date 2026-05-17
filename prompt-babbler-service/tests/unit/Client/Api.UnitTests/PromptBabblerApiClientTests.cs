using System.Net;
using System.Text;
using FluentAssertions;
using PromptBabbler.ApiClient;
using PromptBabbler.ApiClient.Models;

namespace PromptBabbler.ApiClient.UnitTests;

[TestClass]
[TestCategory("Unit")]
public sealed class PromptBabblerApiClientTests
{
    [TestMethod]
    public async Task UpsertBabbleAsync_SendsAccessCodeAndJsonBody()
    {
        var handler = new RecordingHttpMessageHandler();
        using var httpClient = CreateHttpClient(handler, "seed-code");
        var client = new PromptBabblerApiClient(httpClient);

        var response = await client.UpsertBabbleAsync(new BabbleImportItem
        {
            Id = "6f6a8f9f-7b4e-4f7d-a8f1-0c0d7a8f0011",
            Title = "Test title",
            Text = "Test text",
            Tags = ["tag1"],
        }, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.Request.Should().NotBeNull();
        handler.Request!.RequestUri!.PathAndQuery.Should().Be("/api/babbles");
        handler.Request.Headers.TryGetValues("X-Access-Code", out var headerValues).Should().BeTrue();
        headerValues.Should().ContainSingle().Which.Should().Be("seed-code");

        var body = await handler.Request.Content!.ReadAsStringAsync();
        body.Should().Contain("\"id\":\"6f6a8f9f-7b4e-4f7d-a8f1-0c0d7a8f0011\"");
        body.Should().Contain("\"title\":\"Test title\"");
    }

    [TestMethod]
    public async Task StartExportAsync_ReturnsJobIdFromAcceptedPayload()
    {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent("{\"jobId\":\"job-123\"}", Encoding.UTF8, "application/json"),
        });
        using var httpClient = CreateHttpClient(handler);
        var client = new PromptBabblerApiClient(httpClient);

        var jobId = await client.StartExportAsync(new ExportRequest
        {
            IncludeBabbles = true,
            IncludeGeneratedPrompts = false,
            IncludeUserTemplates = true,
            IncludeSemanticVectors = false,
        }, CancellationToken.None);

        jobId.Should().Be("job-123");
        handler.Request.Should().NotBeNull();
        handler.Request!.RequestUri!.PathAndQuery.Should().Be("/api/exports");
    }

    [TestMethod]
    public async Task GetTemplateAsync_WhenNotFound_ReturnsNull()
    {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = CreateHttpClient(handler);
        var client = new PromptBabblerApiClient(httpClient);

        var template = await client.GetTemplateAsync("missing-id", CancellationToken.None);

        template.Should().BeNull();
        handler.Request.Should().NotBeNull();
        handler.Request!.RequestUri!.PathAndQuery.Should().Be("/api/templates/missing-id");
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler, string? accessCode = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/", UriKind.Absolute),
            Timeout = TimeSpan.FromMinutes(5),
        };

        if (!string.IsNullOrWhiteSpace(accessCode))
        {
            httpClient.DefaultRequestHeaders.Add("X-Access-Code", accessCode);
        }

        return httpClient;
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHttpMessageHandler()
            : this(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            })
        {
        }

        public RecordingHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(_response);
        }
    }
}
