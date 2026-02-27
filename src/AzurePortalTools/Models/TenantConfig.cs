namespace AzurePortalTools.Models;

public class TenantConfig
{
    public string DisplayName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class AppAuthConfig
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AppUserConfig
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin"; // Admin | Operator
    public string? RedirectPage { get; set; }    // e.g. "/MlpsDashboard"

    // Fixed resources for Operator users
    public string? TenantDisplayName { get; set; }
    public string? ResourceGroup { get; set; }
    public string? VmName { get; set; }
    public string? NsgName { get; set; }
    public List<string> AllowedProtocols { get; set; } = new();
}
