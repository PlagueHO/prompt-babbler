using Azure.Core;
using FluentAssertions;
using NSubstitute;
using PromptBabbler.Api.Authentication;

namespace PromptBabbler.Api.UnitTests.Authentication;

[TestClass]
[TestCategory("Unit")]
public sealed class AiFoundryTokenCredentialTests
{
    private static string[] GetScopes(TokenRequestContext requestContext)
    {
        var scopesProperty = typeof(TokenRequestContext).GetProperty("Scopes");
        scopesProperty.Should().NotBeNull("TokenRequestContext must expose requested scopes");

        var value = scopesProperty!.GetValue(requestContext);
        return value as string[] ?? [];
    }

    [TestMethod]
    public void GetToken_UsesFoundryAudienceScope()
    {
        var innerCredential = Substitute.For<TokenCredential>();
        var expectedToken = new AccessToken("token-value", DateTimeOffset.UtcNow.AddMinutes(10));
        TokenRequestContext capturedContext = default;
        var captured = false;

        innerCredential.GetToken(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedContext = callInfo.Arg<TokenRequestContext>();
                captured = true;
                return expectedToken;
            });

        var credential = new AiFoundryTokenCredential(innerCredential);
        var token = credential.GetToken(new TokenRequestContext(["https://management.azure.com/.default"]), CancellationToken.None);

        token.Token.Should().Be(expectedToken.Token);
        captured.Should().BeTrue();
        GetScopes(capturedContext).Should().ContainSingle("https://ai.azure.com/.default");
    }

    [TestMethod]
    public async Task GetTokenAsync_UsesFoundryAudienceScope()
    {
        var innerCredential = Substitute.For<TokenCredential>();
        var expectedToken = new AccessToken("token-value", DateTimeOffset.UtcNow.AddMinutes(10));
        TokenRequestContext capturedContext = default;
        var captured = false;

        innerCredential.GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedContext = callInfo.Arg<TokenRequestContext>();
                captured = true;
                return ValueTask.FromResult(expectedToken);
            });

        var credential = new AiFoundryTokenCredential(innerCredential);
        var token = await credential.GetTokenAsync(new TokenRequestContext(["https://management.azure.com/.default"]), CancellationToken.None);

        token.Token.Should().Be(expectedToken.Token);
        captured.Should().BeTrue();
        GetScopes(capturedContext).Should().ContainSingle("https://ai.azure.com/.default");
    }
}
