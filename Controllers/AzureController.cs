using Azure.ResourceManager.Resources;
using ghCopilot.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ghCopilot.Controllers;

public class AzureController : Controller
{
    private readonly IAzureAuthService _azureAuthService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AzureController> _logger;

    public AzureController(IAzureAuthService azureAuthService, IHttpClientFactory httpClientFactory, ILogger<AzureController> logger)
    {
        _azureAuthService = azureAuthService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var model = new AzureStatusViewModel
        {
            IsAuthenticated = _azureAuthService.IsAuthenticated,
            Subscriptions = new List<SubscriptionInfo>(),
            ErrorMessage = null
        };

        if (_azureAuthService.IsAuthenticated)
        {
            try
            {
                // Get subscriptions using the Azure Resource Manager client
                var armClient = _azureAuthService.GetArmClient();
                var subscriptions = armClient.GetSubscriptions();

                var subscriptionList = new List<SubscriptionInfo>();
                await foreach (var subscription in subscriptions)
                {
                    subscriptionList.Add(new SubscriptionInfo
                    {
                        Id = subscription.Data.SubscriptionId,
                        DisplayName = subscription.Data.DisplayName,
                        State = subscription.Data.State?.ToString() ?? "Unknown"
                    });
                    
                    // Limit to first 10 subscriptions for display
                    if (subscriptionList.Count >= 10) break;
                }

                model.Subscriptions = subscriptionList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Azure subscriptions");
                model.ErrorMessage = $"Error retrieving subscriptions: {ex.Message}";
            }
        }

        return View(model);
    }

    public async Task<IActionResult> ResourceGroups(string subscriptionId)
    {
        var model = new ResourceGroupsViewModel
        {
            SubscriptionId = subscriptionId,
            ResourceGroups = new List<ResourceGroupInfo>(),
            ErrorMessage = null
        };

        if (string.IsNullOrEmpty(subscriptionId))
        {
            model.ErrorMessage = "Subscription ID is required";
            return View(model);
        }

        if (!_azureAuthService.IsAuthenticated)
        {
            model.ErrorMessage = "Azure authentication is not configured";
            return View(model);
        }

        try
        {
            // Use the named HttpClient to call Azure REST API
            var httpClient = _httpClientFactory.CreateClient("AzureRestClient");
            var apiVersion = "2021-04-01";
            var url = $"subscriptions/{subscriptionId}/resourcegroups?api-version={apiVersion}";

            var response = await httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ResourceGroupsResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Value != null)
                {
                    model.ResourceGroups = result.Value.Select(rg => new ResourceGroupInfo
                    {
                        Name = rg.Name ?? "Unknown",
                        Location = rg.Location ?? "Unknown",
                        Id = rg.Id ?? "Unknown"
                    }).ToList();
                }
            }
            else
            {
                model.ErrorMessage = $"Failed to retrieve resource groups: {response.StatusCode} - {response.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving resource groups for subscription {SubscriptionId}", subscriptionId);
            model.ErrorMessage = $"Error retrieving resource groups: {ex.Message}";
        }

        return View(model);
    }
}

public class AzureStatusViewModel
{
    public bool IsAuthenticated { get; set; }
    public List<SubscriptionInfo> Subscriptions { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class ResourceGroupsViewModel
{
    public string? SubscriptionId { get; set; }
    public List<ResourceGroupInfo> ResourceGroups { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class SubscriptionInfo
{
    public string? Id { get; set; }
    public string? DisplayName { get; set; }
    public string? State { get; set; }
}

public class ResourceGroupInfo
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}

// Classes for JSON deserialization
public class ResourceGroupsResponse
{
    public List<ResourceGroupData>? Value { get; set; }
}

public class ResourceGroupData
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
}