using Azure.Core;

namespace PromptBabbler.Api.Authentication;

/// <summary>
/// Forces token acquisition for Azure AI Foundry project endpoints
/// to the required audience scope.
/// </summary>
public sealed class AiFoundryTokenCredential : TokenCredential
{
    private const string DefaultScope = "https://ai.azure.com/.default";

    private readonly TokenCredential _innerCredential;
    private readonly TokenRequestContext _requestContext;

    public AiFoundryTokenCredential(TokenCredential innerCredential, string? scope = null)
    {
        _innerCredential = innerCredential ?? throw new ArgumentNullException(nameof(innerCredential));
        _requestContext = new TokenRequestContext([scope ?? DefaultScope]);
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return _innerCredential.GetToken(_requestContext, cancellationToken);
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return _innerCredential.GetTokenAsync(_requestContext, cancellationToken);
    }
}
