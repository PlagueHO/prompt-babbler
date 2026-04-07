using FluentAssertions;
using PromptBabbler.Domain.Configuration;

namespace PromptBabbler.Domain.UnitTests.Configuration;

[TestClass]
[TestCategory("Unit")]
public sealed class AccessControlOptionsTests
{
    [TestMethod]
    public void SectionName_ShouldBeAccessControl()
    {
        AccessControlOptions.SectionName.Should().Be("AccessControl");
    }

    [TestMethod]
    public void AccessCode_ShouldDefaultToNull()
    {
        var options = new AccessControlOptions();
        options.AccessCode.Should().BeNull();
    }

    [TestMethod]
    public void AccessCode_ShouldSupportWithSemantics()
    {
        var options = new AccessControlOptions { AccessCode = "secret123" };
        var updated = options with { AccessCode = "newsecret" };

        updated.AccessCode.Should().Be("newsecret");
        options.AccessCode.Should().Be("secret123");
    }
}
