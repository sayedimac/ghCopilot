using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using ghCopilot.Models;
using Microsoft.Extensions.Options;

namespace ghCopilot.Services;

public interface IAzureAuthService
{
    Task<AccessToken> GetAccessTokenAsync();
    ArmClient GetArmClient();
    bool IsAuthenticated { get; }
}

public class AzureAuthService : IAzureAuthService
{
    private readonly AzureConfig _azureConfig;
    private readonly ILogger<AzureAuthService> _logger;
    private readonly ClientSecretCredential? _credential;
    private readonly ArmClient? _armClient;
    private AccessToken? _cachedToken;

    public AzureAuthService(IOptions<AzureConfig> azureConfig, ILogger<AzureAuthService> logger)
    {
        _azureConfig = azureConfig.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_azureConfig.TenantId) ||
            string.IsNullOrEmpty(_azureConfig.ClientId) ||
            string.IsNullOrEmpty(_azureConfig.ClientSecret))
        {
            _logger.LogWarning("Azure credentials are not properly configured. Check environment variables or configuration.");
            IsAuthenticated = false;
            return;
        }

        try
        {
            _credential = new ClientSecretCredential(
                _azureConfig.TenantId,
                _azureConfig.ClientId,
                _azureConfig.ClientSecret);

            _armClient = new ArmClient(_credential);
            IsAuthenticated = true;
            _logger.LogInformation("Azure authentication initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure authentication.");
            IsAuthenticated = false;
        }
    }

    public bool IsAuthenticated { get; private set; }

    public async Task<AccessToken> GetAccessTokenAsync()
    {
        if (!IsAuthenticated || _credential == null)
        {
            throw new InvalidOperationException("Azure authentication is not properly configured.");
        }

        try
        {
            // Check if we have a cached token that's still valid (with 5 minute buffer)
            if (_cachedToken.HasValue && _cachedToken.Value.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return _cachedToken.Value;
            }

            // Get a new token
            var tokenRequestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
            _cachedToken = await _credential.GetTokenAsync(tokenRequestContext);
            
            _logger.LogDebug("Successfully obtained Azure access token.");
            return _cachedToken.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain Azure access token.");
            throw;
        }
    }

    public ArmClient GetArmClient()
    {
        if (!IsAuthenticated || _armClient == null)
        {
            throw new InvalidOperationException("Azure authentication is not properly configured.");
        }

        return _armClient;
    }
}