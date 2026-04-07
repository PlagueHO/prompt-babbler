using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using PromptBabbler.Api.Controllers;
using PromptBabbler.Domain.Configuration;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.UnitTests.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class ConfigControllerTests
{
    private static IOptionsMonitor<AccessControlOptions> CreateOptions(string? accessCode)
    {
        var monitor = Substitute.For<IOptionsMonitor<AccessControlOptions>>();
        monitor.CurrentValue.Returns(new AccessControlOptions { AccessCode = accessCode });
        return monitor;
    }

    [TestMethod]
    public void GetAccessStatus_WhenAccessCodeConfigured_ShouldReturnRequired()
    {
        var controller = new ConfigController(CreateOptions("secret123"));

        var result = controller.GetAccessStatus();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AccessControlStatusResponse>().Subject;
        response.AccessCodeRequired.Should().BeTrue();
    }

    [TestMethod]
    public void GetAccessStatus_WhenAccessCodeEmpty_ShouldReturnNotRequired()
    {
        var controller = new ConfigController(CreateOptions(string.Empty));

        var result = controller.GetAccessStatus();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AccessControlStatusResponse>().Subject;
        response.AccessCodeRequired.Should().BeFalse();
    }

    [TestMethod]
    public void GetAccessStatus_WhenAccessCodeNull_ShouldReturnNotRequired()
    {
        var controller = new ConfigController(CreateOptions(null));

        var result = controller.GetAccessStatus();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AccessControlStatusResponse>().Subject;
        response.AccessCodeRequired.Should().BeFalse();
    }
}
