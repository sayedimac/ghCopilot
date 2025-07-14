namespace ghCopilot.Models;

public class AzureConfig
{
    public const string SectionName = "Azure";
    
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}