using System.Text.Json;
using FluentAssertions;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.UnitTests.Models;

[TestClass]
[TestCategory("Unit")]
public sealed class AccessControlStatusResponseTests
{
    [TestMethod]
    public void Serialization_ShouldUseCamelCase()
    {
        var response = new AccessControlStatusResponse { AccessCodeRequired = true };
        var json = JsonSerializer.Serialize(response);

        json.Should().Contain("\"accessCodeRequired\":true");
    }

    [TestMethod]
    public void Deserialization_ShouldWork()
    {
        var json = """{"accessCodeRequired":false}""";
        var response = JsonSerializer.Deserialize<AccessControlStatusResponse>(json);

        response.Should().NotBeNull();
        response!.AccessCodeRequired.Should().BeFalse();
    }

    [TestMethod]
    public void WithSemantics_ShouldCreateNewInstance()
    {
        var original = new AccessControlStatusResponse { AccessCodeRequired = true };
        var updated = original with { AccessCodeRequired = false };

        updated.AccessCodeRequired.Should().BeFalse();
        original.AccessCodeRequired.Should().BeTrue();
    }
}
