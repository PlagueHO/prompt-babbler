using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace PromptBabbler.McpServer.Client;

public sealed class ApiAuthDelegatingHandler : DelegatingHandler
{
    private readonly ApiAuthOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public ApiAuthDelegatingHandler(ApiAuthOptions options, IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_options.AzureAdClientId))
        {
            // Entra ID mode: acquire OBO token for the downstream API
            var tokenAcquisition = _serviceProvider.GetService<ITokenAcquisition>();
            if (tokenAcquisition is not null)
            {
                var token = await tokenAcquisition.GetAccessTokenForUserAsync(
                    [_options.ApiScope]);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        else if (!string.IsNullOrEmpty(_options.AccessCode))
        {
            request.Headers.Add("X-Access-Code", _options.AccessCode);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
