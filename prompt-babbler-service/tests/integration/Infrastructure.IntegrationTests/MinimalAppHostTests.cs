namespace PromptBabbler.Infrastructure.IntegrationTests;

using PromptBabbler.IntegrationTests.Shared;

/// <summary>
/// Minimal Aspire AppHost integration tests to verify basic functionality.
/// Uses a shared AppHost instance across all tests via AppHostFixture.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize]
public class MinimalAppHostTests
{
    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        await AppHostFixture.InitializeAsync(context);
    }

    [TestMethod]
    public void AppHost_IsCreatedAndStarted()
    {
        AppHostFixture.App.Should().NotBeNull();
        TestContext?.WriteLine("[TEST] AppHost is available and running!");
    }

    [TestMethod]
    public void AppHost_CosmosDbConnectionString_IsAvailable()
    {
        AppHostFixture.App.Should().NotBeNull("AppHost should be initialized by ClassInitialize");

        TestContext?.WriteLine("[TEST] Getting Cosmos DB connection string from fixture...");
        var connectionString = AppHostFixture.CosmosDbConnectionString;
        TestContext?.WriteLine($"[TEST] Connection string: {connectionString[..Math.Min(50, connectionString.Length)]}...");

        connectionString.Should().NotBeNullOrWhiteSpace();
        connectionString.Should().Contain("AccountEndpoint="); // Emulator uses HTTP
        connectionString.Should().Contain("AccountKey="); // Emulator uses standard key

        // Validate account endpoint is accessible
        AppHostFixture.CosmosDbAccountEndpoint.Should().NotBeNullOrWhiteSpace();

        // Validate account key is accessible
        AppHostFixture.CosmosDbAccountKey.Should().NotBeNullOrWhiteSpace();
        AppHostFixture.CosmosDbAccountKey.Length.Should().BeGreaterThan(10);

        TestContext?.WriteLine("[TEST] Connection string and connection details available!");
    }
}
