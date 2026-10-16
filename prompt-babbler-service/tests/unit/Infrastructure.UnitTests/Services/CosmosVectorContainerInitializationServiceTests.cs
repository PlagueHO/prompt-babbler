using System.Collections.ObjectModel;
using System.Net;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class CosmosVectorContainerInitializationServiceTests
{
    private readonly CosmosClient _client = Substitute.For<CosmosClient>();
    private readonly Database _database = Substitute.For<Database>();
    private readonly Container _container = Substitute.For<Container>();
    private readonly IHostEnvironment _environment = Substitute.For<IHostEnvironment>();
    private readonly CosmosVectorContainerInitializationService _service;

    public CosmosVectorContainerInitializationServiceTests()
    {
        _environment.EnvironmentName.Returns(Environments.Development);
        _client.GetDatabase(CosmosBabbleRepository.DatabaseName).Returns(_database);
        _database.GetContainer(CosmosBabbleRepository.ContainerName).Returns(_container);
        _service = new(_client, _environment, NullLogger<CosmosVectorContainerInitializationService>.Instance);
    }

    [TestMethod]
    public async Task StartAsync_DiskAnnContainerWithoutReturnedEmbeddingMetadata_PreservesContainer()
    {
        var properties = CompatibleProperties();
        properties.VectorEmbeddingPolicy = null;
        SetExistingProperties(properties);

        await _service.StartAsync(CancellationToken.None);

        await AssertNotDeletedAsync();
        await AssertNotCreatedAsync();
    }

    [TestMethod]
    [DataRow("Production")]
    [DataRow("Staging")]
    [DataRow("Testing")]
    public async Task StartAsync_NonDevelopment_DoesNotAccessCosmos(string environmentName)
    {
        _environment.EnvironmentName.Returns(environmentName);

        await _service.StartAsync(CancellationToken.None);

        _client.DidNotReceive().GetDatabase(Arg.Any<string>());
    }

    [TestMethod]
    public async Task StartAsync_MissingContainer_CreatesCompleteDiskAnnPolicy()
    {
        SetReadFailure(HttpStatusCode.NotFound);
        ContainerProperties? createdProperties = null;
        _database.CreateContainerAsync(Arg.Any<ContainerProperties>(), throughput: null, null, Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                createdProperties = call.Arg<ContainerProperties>();
                return Substitute.For<ContainerResponse>();
            });

        await _service.StartAsync(CancellationToken.None);

        createdProperties.Should().NotBeNull();
        createdProperties!.Id.Should().Be("babbles");
        createdProperties.PartitionKeyPath.Should().Be("/userId");
        var embedding = createdProperties.VectorEmbeddingPolicy.Embeddings.Should().ContainSingle().Subject;
        embedding.Path.Should().Be("/contentVector");
        embedding.Dimensions.Should().Be(1536);
        embedding.DataType.Should().Be(VectorDataType.Float32);
        embedding.DistanceFunction.Should().Be(DistanceFunction.Cosine);
        var policy = createdProperties.IndexingPolicy;
        policy.IndexingMode.Should().Be(IndexingMode.Consistent);
        policy.Automatic.Should().BeTrue();
        policy.IncludedPaths.Select(path => path.Path).Should().Equal("/*");
        policy.ExcludedPaths.Select(path => path.Path).Should().BeEquivalentTo("/contentVector/*", "/\"_etag\"/?");
        var index = policy.VectorIndexes.Should().ContainSingle().Subject;
        index.Path.Should().Be("/contentVector");
        index.Type.Should().Be(VectorIndexType.DiskANN);
        await AssertNotDeletedAsync();
    }

    [TestMethod]
    public async Task StartAsync_CompatibleContainer_DoesNotQueryOrModifyDocuments()
    {
        SetExistingProperties(CompatibleProperties());

        await _service.StartAsync(CancellationToken.None);

        _container.DidNotReceive().GetItemQueryIterator<int>(
            Arg.Any<QueryDefinition>(), Arg.Any<string>(), Arg.Any<QueryRequestOptions>());
        await AssertNotDeletedAsync();
        await AssertNotCreatedAsync();
    }

    [TestMethod]
    public async Task StartAsync_EmptyDefaultContainer_RecreatesWithDiskAnn()
    {
        SetExistingProperties(new ContainerProperties("babbles", "/userId"));
        SetDocumentPages(0, 0);

        await _service.StartAsync(CancellationToken.None);

        await _container.Received(1).DeleteContainerAsync(null, Arg.Any<CancellationToken>());
        await _database.Received(1).CreateContainerAsync(
            Arg.Is<ContainerProperties>(properties =>
                properties.IndexingPolicy.VectorIndexes.Single().Type == VectorIndexType.DiskANN),
            throughput: null, null, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [DataRow("empty-policy")]
    [DataRow("missing-index")]
    [DataRow("quantized-flat")]
    [DataRow("flat")]
    [DataRow("index-path")]
    [DataRow("embedding-path")]
    [DataRow("dimensions")]
    [DataRow("data-type")]
    [DataRow("distance-function")]
    [DataRow("partition-key")]
    public async Task StartAsync_PopulatedIncompatibleContainer_PreservesDataAndExplainsRecovery(string mismatch)
    {
        var properties = CompatibleProperties();
        switch (mismatch)
        {
            case "empty-policy": properties.VectorEmbeddingPolicy.Embeddings.Clear(); break;
            case "missing-index": properties.IndexingPolicy.VectorIndexes.Clear(); break;
            case "quantized-flat": properties.IndexingPolicy.VectorIndexes[0].Type = VectorIndexType.QuantizedFlat; break;
            case "flat": properties.IndexingPolicy.VectorIndexes[0].Type = VectorIndexType.Flat; break;
            case "index-path": properties.IndexingPolicy.VectorIndexes[0].Path = "/other"; break;
            case "embedding-path": properties.VectorEmbeddingPolicy.Embeddings[0].Path = "/other"; break;
            case "dimensions": properties.VectorEmbeddingPolicy.Embeddings[0].Dimensions = 100; break;
            case "data-type": properties.VectorEmbeddingPolicy.Embeddings[0].DataType = VectorDataType.Int8; break;
            case "distance-function": properties.VectorEmbeddingPolicy.Embeddings[0].DistanceFunction = DistanceFunction.Euclidean; break;
            case "partition-key": properties.PartitionKeyPath = "/other"; break;
        }
        SetExistingProperties(properties);
        SetDocumentPages(0, 1);

        var action = () => _service.StartAsync(CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("No documents were deleted")
            .And.Contain("back up").And.Contain("docs/quickstart-local.md#recover-an-incompatible-local-babbles-container");
        await AssertNotDeletedAsync();
        await AssertNotCreatedAsync();
    }

    [TestMethod]
    public async Task StartAsync_DocumentQueryFails_DoesNotDeleteContainer()
    {
        SetExistingProperties(new ContainerProperties("babbles", "/userId"));
        var iterator = SetDocumentPages(0);
        iterator.ReadNextAsync(Arg.Any<CancellationToken>()).ThrowsAsync(
            new CosmosException("Query failed", HttpStatusCode.BadRequest, 0, string.Empty, 0));

        var action = () => _service.StartAsync(CancellationToken.None);

        await action.Should().ThrowAsync<CosmosException>();
        await AssertNotDeletedAsync();
        await AssertNotCreatedAsync();
    }

    [TestMethod]
    public async Task StartAsync_CancelledDocumentQuery_DoesNotDeleteContainer()
    {
        SetExistingProperties(new ContainerProperties("babbles", "/userId"));
        var iterator = SetDocumentPages(0);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        iterator.ReadNextAsync(cancellation.Token).ThrowsAsync(new OperationCanceledException(cancellation.Token));

        var action = () => _service.StartAsync(cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        await AssertNotDeletedAsync();
        await AssertNotCreatedAsync();
    }

    [TestMethod]
    public async Task StartAsync_ReadFails_PropagatesFailureWithoutDeleting()
    {
        SetReadFailure(HttpStatusCode.Forbidden);

        var action = () => _service.StartAsync(CancellationToken.None);

        await action.Should().ThrowAsync<CosmosException>();
        await AssertNotDeletedAsync();
        await AssertNotCreatedAsync();
    }

    [TestMethod]
    public async Task StartAsync_CreateFails_PropagatesFailure()
    {
        SetReadFailure(HttpStatusCode.NotFound);
        _database.CreateContainerAsync(Arg.Any<ContainerProperties>(), throughput: null, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Create failed", HttpStatusCode.BadRequest, 0, string.Empty, 0));

        var action = () => _service.StartAsync(CancellationToken.None);

        await action.Should().ThrowAsync<CosmosException>();
        await AssertNotDeletedAsync();
    }

    [TestMethod]
    public async Task StartAsync_ServiceUnavailableDuringCancelledRetry_PropagatesCancellation()
    {
        SetReadFailure(HttpStatusCode.ServiceUnavailable);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => _service.StartAsync(cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        await AssertNotDeletedAsync();
        await AssertNotCreatedAsync();
    }

    private void SetExistingProperties(ContainerProperties properties)
    {
        var response = Substitute.For<ContainerResponse>();
        response.Resource.Returns(properties);
        _container.ReadContainerAsync(null, Arg.Any<CancellationToken>()).Returns(response);
    }

    private void SetReadFailure(HttpStatusCode statusCode) =>
        _container.ReadContainerAsync(null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Container read failed", statusCode, 0, string.Empty, 0));

    private FeedIterator<int> SetDocumentPages(params int[] counts)
    {
        var iterator = Substitute.For<FeedIterator<int>>();
        var responses = counts.Select(count =>
        {
            var response = Substitute.For<FeedResponse<int>>();
            response.Count.Returns(count);
            return response;
        }).ToArray();
        var page = 0;
        iterator.HasMoreResults.Returns(_ => page < counts.Length);
        iterator.ReadNextAsync(Arg.Any<CancellationToken>()).Returns(_ => responses[page++]);
        _container.GetItemQueryIterator<int>(
            Arg.Is<QueryDefinition>(query => query.QueryText == "SELECT TOP 1 VALUE 1 FROM c"),
            null, null).Returns(iterator);
        return iterator;
    }

    private async Task AssertNotDeletedAsync() =>
        await _container.DidNotReceive().DeleteContainerAsync(
            Arg.Any<ContainerRequestOptions>(), Arg.Any<CancellationToken>());

    private async Task AssertNotCreatedAsync() =>
        await _database.DidNotReceive().CreateContainerAsync(
            Arg.Any<ContainerProperties>(), Arg.Any<int?>(),
            Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>());

    private static ContainerProperties CompatibleProperties()
    {
        var properties = new ContainerProperties("babbles", "/userId")
        {
            VectorEmbeddingPolicy = new VectorEmbeddingPolicy(new Collection<Embedding>
            {
                new()
                {
                    Path = "/contentVector",
                    Dimensions = 1536,
                    DataType = VectorDataType.Float32,
                    DistanceFunction = DistanceFunction.Cosine,
                },
            }),
        };
        properties.IndexingPolicy.VectorIndexes.Add(new VectorIndexPath
        {
            Path = "/contentVector",
            Type = VectorIndexType.DiskANN,
        });
        return properties;
    }
}
