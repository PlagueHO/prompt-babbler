using System.Text.Json;
using Aspire.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.IntegrationTests;

[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
public sealed class CosmosVectorContainerInitializationServiceTests
{
    private static DistributedApplication? s_app;
    private static CosmosClient s_client = null!;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PromptBabbler_AppHost>();
        // Test only Cosmos, with a session-owned container and random ports, not cloud resources.
        builder.Resources.Clear();
        builder.AddAzureCosmosDB("cosmos-vector-tests").RunAsEmulator();
        s_app = await builder.BuildAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await s_app.StartAsync(timeout.Token);
        await s_app.ResourceNotifications.WaitForResourceHealthyAsync("cosmos-vector-tests", timeout.Token);
        var connectionString = await s_app.GetConnectionStringAsync("cosmos-vector-tests", timeout.Token);
        s_client = new CosmosClient(connectionString, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web),
        });
        context.WriteLine("Isolated vNext emulator is ready for vector-container tests.");
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        s_client?.Dispose();
        if (s_app is not null)
        {
            await s_app.DisposeAsync();
        }
    }

    [TestInitialize]
    public async Task CreateDatabaseAsync()
    {
        await s_client.CreateDatabaseAsync(CosmosBabbleRepository.DatabaseName);
    }

    [TestCleanup]
    public async Task DeleteTestDatabaseAsync()
    {
        await s_client.GetDatabase(CosmosBabbleRepository.DatabaseName).DeleteAsync();
    }

    [TestMethod]
    public async Task StartAsync_FreshDatabase_CreatesDiskAnnContainer()
    {
        await CreateInitializer().StartAsync(CancellationToken.None);

        var response = await GetContainer().ReadContainerAsync();
        response.Resource.PartitionKeyPath.Should().Be("/userId");
        var index = response.Resource.IndexingPolicy.VectorIndexes.Should().ContainSingle().Subject;
        index.Path.Should().Be("/contentVector");
        index.Type.Should().Be(VectorIndexType.DiskANN);
    }

    [TestMethod]
    public async Task StartAsync_EmptyAspireDefaultContainer_RecreatesWithDiskAnn()
    {
        await s_client.GetDatabase(CosmosBabbleRepository.DatabaseName)
            .CreateContainerAsync("babbles", "/userId");

        await CreateInitializer().StartAsync(CancellationToken.None);

        var response = await GetContainer().ReadContainerAsync();
        response.Resource.IndexingPolicy.VectorIndexes.Should().ContainSingle()
            .Which.Type.Should().Be(VectorIndexType.DiskANN);
    }

    [TestMethod]
    public async Task StartAsync_PopulatedIncompatibleContainer_PreservesDocumentsAndPolicy()
    {
        await s_client.GetDatabase(CosmosBabbleRepository.DatabaseName)
            .CreateContainerAsync("babbles", "/userId");
        var repository = CreateRepository();
        var babble = CreateBabble("preserved", "user-a", Vector(1, 0));
        await repository.CreateAsync(babble);

        var action = () => CreateInitializer().StartAsync(CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No documents were deleted*");
        (await repository.GetByIdAsync(babble.UserId, babble.Id)).Should().BeEquivalentTo(babble);
        var response = await GetContainer().ReadContainerAsync();
        response.Resource.IndexingPolicy.VectorIndexes.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SearchByVectorAsync_SmallDatasetAfterRestart_ReturnsRelevantBabblesForCurrentUser()
    {
        await CreateInitializer().StartAsync(CancellationToken.None);
        var repository = CreateRepository();
        var exact = CreateBabble("exact", "user-a", Vector(1, 0));
        var similar = CreateBabble("similar", "user-a", Vector(0.8f, 0.6f));
        var unrelated = CreateBabble("unrelated", "user-a", Vector(0, 1));
        var otherUser = CreateBabble("other-user", "user-b", Vector(1, 0));
        foreach (var babble in new[] { exact, similar, unrelated, otherUser })
        {
            await repository.CreateAsync(babble);
        }

        await CreateInitializer().StartAsync(CancellationToken.None);

        (await repository.GetByIdAsync(exact.UserId, exact.Id)).Should().BeEquivalentTo(exact);
        var results = await repository.SearchByVectorAsync("user-a", Vector(1, 0), 10);
        results.Select(result => result.Babble.Id).Should().Equal("exact", "similar");
        var limitedResults = await repository.SearchByVectorAsync("user-a", Vector(1, 0), 1);
        limitedResults.Select(result => result.Babble.Id).Should().Equal("exact");
    }

    private static Container GetContainer() =>
        s_client.GetContainer(CosmosBabbleRepository.DatabaseName, CosmosBabbleRepository.ContainerName);

    private static CosmosVectorContainerInitializationService CreateInitializer()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        return new(s_client, environment, NullLogger<CosmosVectorContainerInitializationService>.Instance);
    }

    private static CosmosBabbleRepository CreateRepository() =>
        new(s_client, NullLogger<CosmosBabbleRepository>.Instance);

    private static Babble CreateBabble(string id, string userId, float[] vector) => new()
    {
        Id = id,
        UserId = userId,
        Title = $"Synthetic vector test {id}",
        Text = "Synthetic emulator integration test content.",
        CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
        UpdatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
        ContentVector = vector,
    };

    private static float[] Vector(float first, float second)
    {
        var vector = new float[1536];
        vector[0] = first;
        vector[1] = second;
        return vector;
    }
}
