using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using PromptBabbler.McpServer.Configuration;

namespace PromptBabbler.McpServer.UnitTests.Configuration;

[TestClass]
[TestCategory("Unit")]
public sealed class AgenticFoundryClientFactoryTests
{
    private const string ConfiguredEndpoint = "https://configured.services.ai.azure.com/api/projects/configured";
    private const string EnvironmentEndpoint = "https://environment.services.ai.azure.com/api/projects/environment";
    private const string ConnectionStringEndpoint = "https://connection.services.ai.azure.com/api/projects/connection";
    private const string BareConnectionStringEndpoint = "https://bare.services.ai.azure.com/api/projects/bare";

    [TestMethod]
    public void ResolveProjectEndpoint_ShouldPreferConfiguredEndpoint()
    {
        WithEnvironmentVariable(
            "AZURE_AI_PROJECT_ENDPOINT",
            EnvironmentEndpoint,
            () =>
            {
                var factory = CreateFactory(new Dictionary<string, string?>
                {
                    ["Agentic:FoundryProjectEndpoint"] = ConfiguredEndpoint,
                    ["ConnectionStrings:ai-foundry"] = $"Endpoint={ConnectionStringEndpoint};Credential=ignored",
                });

                factory.ResolveProjectEndpoint().Should().Be(ConfiguredEndpoint);
            });
    }

    [TestMethod]
    public void ResolveProjectEndpoint_ShouldUseEnvironmentEndpointWhenConfiguredEndpointMissing()
    {
        WithEnvironmentVariable(
            "AZURE_AI_PROJECT_ENDPOINT",
            EnvironmentEndpoint,
            () =>
            {
                var factory = CreateFactory(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:ai-foundry"] = $"Endpoint={ConnectionStringEndpoint};Credential=ignored",
                });

                factory.ResolveProjectEndpoint().Should().Be(EnvironmentEndpoint);
            });
    }

    [TestMethod]
    public void ResolveProjectEndpoint_ShouldUseConnectionStringEndpointWhenHigherPrecedenceSourcesMissing()
    {
        WithEnvironmentVariable(
            "AZURE_AI_PROJECT_ENDPOINT",
            null,
            () =>
            {
                var factory = CreateFactory(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:ai-foundry"] = $"Endpoint={ConnectionStringEndpoint};Credential=ignored",
                });

                factory.ResolveProjectEndpoint().Should().Be(ConnectionStringEndpoint);
            });
    }

    [TestMethod]
    public void ResolveProjectEndpoint_ShouldUseBareConnectionStringUriWhenEndpointSegmentMissing()
    {
        WithEnvironmentVariable(
            "AZURE_AI_PROJECT_ENDPOINT",
            null,
            () =>
            {
                var factory = CreateFactory(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:ai-foundry"] = BareConnectionStringEndpoint,
                });

                factory.ResolveProjectEndpoint().Should().Be(BareConnectionStringEndpoint);
            });
    }

    [TestMethod]
    public void CreateClient_ShouldThrowClearErrorWhenEndpointMissing()
    {
        WithEnvironmentVariable(
            "AZURE_AI_PROJECT_ENDPOINT",
            null,
            () =>
            {
                var factory = CreateFactory(new Dictionary<string, string?>());

                var action = () => factory.CreateClient();

                action.Should().Throw<InvalidOperationException>()
                    .WithMessage("*Agentic Foundry project endpoint is not configured*");
            });
    }

    private static AgenticFoundryClientFactory CreateFactory(IDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values!)
            .Build();

        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns(Environments.Development);

        return new AgenticFoundryClientFactory(configuration, hostEnvironment);
    }

    private static void WithEnvironmentVariable(string name, string? value, Action action)
    {
        var originalValue = Environment.GetEnvironmentVariable(name);

        try
        {
            Environment.SetEnvironmentVariable(name, value);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, originalValue);
        }
    }
}