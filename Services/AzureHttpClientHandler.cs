using System.Net.Http.Headers;

namespace ghCopilot.Services;

public class AzureHttpClientHandler : DelegatingHandler
{
    private readonly IAzureAuthService _azureAuthService;
    private readonly ILogger<AzureHttpClientHandler> _logger;

    public AzureHttpClientHandler(IAzureAuthService azureAuthService, ILogger<AzureHttpClientHandler> logger)
    {
        _azureAuthService = azureAuthService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_azureAuthService.IsAuthenticated)
        {
            try
            {
                var token = await _azureAuthService.GetAccessTokenAsync();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add Azure authentication token to HTTP request.");
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}